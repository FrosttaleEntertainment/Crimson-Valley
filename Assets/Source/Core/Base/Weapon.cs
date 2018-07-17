// (c) Copyright Cleverous 2015. All rights reserved.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Base
{
    public class Weapon : NetworkBehaviour
    {
        #region Variable Definitions

        public WeaponStats Stats;
        public GameObject ProjectileSpawnPoint;
        public List<AudioClip> FireSounds = new List<AudioClip>();

        // melee only
        public List<GameObject> AttackPrefabs;
        public List<MeleeAttack> Attacks;
        public List<string> ImpactTagNames = new List<string>();
        public List<AudioClip> ImpactSounds = new List<AudioClip>();
        public List<GameObject> ImpactEffects = new List<GameObject>();
        public LayerMask Mask;
        //

        public GameObject PickupReference;
        public Entity Owner;
        //private Animator _thisAnimator;
        private PlayerController m_player;
        private ParticleSystem _ejector;

        private int _nextSpawnPt;
        public bool IsReloading;


        public bool Attacking;
        public bool Attacking2;
        public bool DoingMeleeSwing;
        private MeleeAttack _thisAttack;

        private bool _thisIsPlayer;
        private int _currentAttack;

        private Collider _myCollider;
        private List<Collider> _hitsDuringThisSwing;
        private GameObject _victimGameObject;
        private Entity _victimSubject;
        private string _firingInputName = "Fire1";
        public bool InputPermission = true;

        [SyncVar]
        private int m_currentAmmo;

        [SyncVar]
        private Vector3 m_spawnPosition;

        [SyncVar]
        private Quaternion m_spawnRotation;

        #endregion

        void Reset()
        {
            AttackPrefabs = new List<GameObject>();
            FireSounds = new List<AudioClip>();

            ImpactSounds = new List<AudioClip>();
            ImpactEffects = new List<GameObject>();

            Stats = new WeaponStats
            {
                Title = "Derpzooka",
                WeaponType = WeaponType.Ranged,
                FireStyle = FireStyle.FullAuto,

                FireSound = null,
                FiresProjectile = null,

                PositionOffset = new Vector3(0f, 0f, 0.5f),
                NonDominantHandPos = new Vector3(0f, 0f, 0f),
                NonDominantHandRot = new Vector3(0f, 0f, 0f),
                NonDominantElbowOffset = new Vector3(0, -0.1f, 0.1f),
                DominantElbowOffset = new Vector3(0, -0.1f, 0.1f),

                NoAmmoSound = null,
                ReloadSound = null,
                AmmoCost = 1,
                StartingMagazines = 8,
                MagazineSize = 6,
                CurrentMagazines = 8,
                CurrentAmmo = 6, // TODO add editor field for Current Ammo
                TimeToCooldown = 0.2f,
                Accuracy = 1.0f,

                CanHitMultiples = true,
                CanAttackAndMove = false,
                WeaponTrail = null
            };
        }
        void Awake()
        {
            _thisAttack = null;
            Attacks = new List<MeleeAttack>();
            _hitsDuringThisSwing = new List<Collider>();
            _myCollider = GetComponent<Collider>(); // disabled for Melee in Start()

            foreach (GameObject go in AttackPrefabs)
            {
                Attacks.Add(go.GetComponent<MeleeAttack>());
            }
            m_currentAmmo = Stats.MagazineSize;


        }

        private void Update()
        {
            if(ProjectileSpawnPoint)
            {
                m_spawnPosition = ProjectileSpawnPoint.transform.position;
                m_spawnRotation = ProjectileSpawnPoint.transform.rotation;
            }
        }

        void OnEnable()
        {
            _currentAttack = 0;
            _nextSpawnPt = 0;
            Attacking = false;

            while (Owner == null) return;
            if (Owner.Stats.EntityGroup == EntityGroup.Player)
            {
                m_player = Owner.GetComponent<PlayerController>();
                if (m_player != null) _thisIsPlayer = true;
                else Debug.LogWarning(this + " could not initialize! Subject is Player, but is missing a PlayerController script for input relays.");
            }
            else { _thisIsPlayer = false; }

            //_thisAnimator = Owner.GetAnimator();
            _ejector = Stats.AmmoEjector;

            if (m_player != null && m_player.isLocalPlayer)
            {
                // Start the correct control loop for this Weapon Type.
                StartCoroutine(Stats.WeaponType == WeaponType.Ranged
                    ? WeaponLoopRanged()
                    : WeaponLoopMelee());
            }
        }
        

        void Start()
        {
            if (Stats.WeaponType == WeaponType.Melee)
            {
                Physics.IgnoreCollision(Owner.GetComponent<Collider>(), GetComponent<Collider>());
                _myCollider.enabled = false;

                if (Stats.WeaponTrail != null)
                {
                    Stats.WeaponTrail.SetActive(false);
                }
            }
        }

        // Ranged
        private IEnumerator WeaponLoopRanged()
        {
            while (true)
            {
                while (IsReloading || Owner == null || !InputPermission) yield return null;
                if (_thisIsPlayer)
                {
                    if (Owner != null && !Owner.IsDead && m_player.GetInputReload) yield return StartCoroutine(Reload());
                    Attacking = (m_player.GetInputFire1 > 0);
                }

                if (Attacking)
                {
                    yield return StartCoroutine(FireRanged());
                }
                yield return null;
            }
        }

        private IEnumerator FireRanged()
        {
            if (m_currentAmmo >= Stats.AmmoCost )
            {
                CmdFireRange();

                yield return StartCoroutine(FireCooldown());
                yield break;

            }
            else if (MagIsEmpty() && Stats.AutoReload)
            {
                CmdReload();
                yield return StartCoroutine(ReloadCooldown());
            }

            yield return StartCoroutine(NoAmmo());
        }

        [Command]
        private void CmdFireRange()
        {
            if (m_currentAmmo >= Stats.AmmoCost)
            {
                m_player.SetShooting(true, Stats.TimeToCooldown);

                //DoFireEffect(); // client call?
                RpcInstantiateBullet();
                m_currentAmmo -= Stats.AmmoCost;

                Owner.DoWeaponFire();// client call?
            }
            else if (MagIsEmpty() && Stats.AutoReload)
            {
                return;
            }

            // the IFs failed, so this code is reached and we are indeed out of ammo.
            RpcNoAmmo();
        }

        [ClientRpc]
        private void RpcInstantiateBullet()
        {

            Vector3 pos = m_spawnPosition;
            Quaternion rot = m_spawnRotation;
            GameObject thisBullet = StaticUtil.Spawn(Stats.FiresProjectile, pos, rot) as GameObject;
            if (thisBullet != null)
            {
                var bullet = thisBullet.GetComponent<Projectile>();

                bullet.Owner = Owner; // assign the bullet owner, for points, etc.
            }

            if (_ejector != null) _ejector.Emit(1); // if there is a shell casing ejector, use it.
            if (Stats.ProjectileSpawnPoints.Count > 1) // if using multiple spawn points, iterate through them.
            {
                _nextSpawnPt++;
                if (_nextSpawnPt > Stats.ProjectileSpawnPoints.Count - 1)
                {
                    _nextSpawnPt = 0;
                }
            }

        }

        [ClientRpc]
        private void RpcNoAmmo()
        {
            StartCoroutine(NoAmmo());
        }

        private IEnumerator NoAmmo()
        {
            Owner.DoMagazineIsEmpty();
            if (Stats.NoAmmoSound != null)
            {
                AudioSource.PlayClipAtPoint(Stats.NoAmmoSound, transform.position);
            }

            yield return StartCoroutine(FireCooldown());
        }

        public IEnumerator Reload()
        {
            if (HasMags() && !MagIsFull())
            {
                CmdReload();
                yield return StartCoroutine(ReloadCooldown());
            }
            else
            {
                yield return StartCoroutine(NoAmmo());
                yield return StartCoroutine(FireCooldown());

            }
        }

        [Command]
        public void CmdReload()
        {
            if (HasMags() && !MagIsFull())
            {
                Stats.CurrentMagazines--;
                m_currentAmmo = Stats.MagazineSize;
                Owner.DoWeaponReload();
                m_player.ReloadWeapon();

                if (Stats.ReloadSound != null)
                {
                    AudioSource.PlayClipAtPoint(Stats.ReloadSound, transform.position);
                }
            }
        }

        //used for grenades
        [Command]
        public void CmdForceFireOnce()
        {
            CmdFireRange();
        }


        private IEnumerator ReloadCooldown()
        {
            IsReloading = true;
            yield return new WaitForSeconds(Stats.ReloadTime);
            IsReloading = false;
        }

        // Melee
        void OnTriggerEnter(Collider theColliderWeHit)
        {
            // Add hits to a list to prevent damage stacking in the same swing. 
            // Empty the list when swing finishes.
            if (_hitsDuringThisSwing.Contains(theColliderWeHit))
            {
                return;
            }

            _victimGameObject = theColliderWeHit.gameObject;
            _victimSubject = _victimGameObject.GetComponent<Entity>();

            Vector3 impactPos = theColliderWeHit.transform.position + Vector3.up; // would be nice to get more accurate with this.

            // if what we hit is on a layer in our mask, plow into it.
            if (!StaticUtil.LayerMatchTest(Mask, _victimGameObject)) return;
            if (_victimSubject != null) DoMeleeDamage(theColliderWeHit.GetComponent<Entity>());

            DoMeleeDamage(theColliderWeHit.GetComponent<Entity>());
            DoImpactEffects(impactPos);
            _hitsDuringThisSwing.Add(theColliderWeHit);
        }
        private IEnumerator WeaponLoopMelee()
        {
            while (true)
            {
                if (Owner == null || !InputPermission) yield return null;
                if (_thisIsPlayer)
                {
                    if (Stats.FireStyle == FireStyle.SemiAuto)
                    {
                        while (m_player.GetInputFire1 > 0.1f || m_player.GetInputFire2 > 0.1f) yield return null;
                    }


                    Attacking = m_player.GetInputFire1 > 0.1f;
                    Attacking2 = m_player.GetInputFire2 > 0.1f;
                }

                if (Attacking || Attacking2)
                {
                    _firingInputName = Attacking ? "Fire1" : "Fire2";
                    if (DoingMeleeSwing) break;

                    DoingMeleeSwing = true;
                    yield return StartCoroutine(FireMelee(_firingInputName));
                    DoingMeleeSwing = false;
                }

                Attacking = false;
                Attacking2 = false;
                yield return null;
            }
        }
        private IEnumerator FireMelee(string controlInput)
        {
            var animator = Owner.GetAnimator();
            if (animator == null)
            {
                Debug.Assert(false, "No animator found");
                yield return null;
            }

            int hashCache = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            GetMeleeAttackByInput(controlInput);

            MeleeStart(); // start

            animator.SetTrigger(_thisAttack.AnimatorTriggerName);
            animator.SetFloat(_thisAttack.AnimatorAttackSpeed, _thisAttack.AnimatonSpeed);

            while (animator.GetCurrentAnimatorStateInfo(0).shortNameHash == hashCache) yield return null;
            while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < _thisAttack.TriggerStartAt) yield return null;
            MeleeTriggerToggle(true);
            while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < _thisAttack.TriggerEndAt) yield return null;
            MeleeTriggerToggle(false);

            MeleeCleanup(); // finish

            if (_thisAttack.Cooldown > 0) yield return StartCoroutine(FireCooldown());
            yield return null;
        }
        private void GetMeleeAttackByInput(string input)
        {
            ///// Find a match for the inputs
            for (int i = 0; i < Attacks.Count; i++)
            {
                if (Attacks[i].InputReference == input)
                {
                    _currentAttack = i;
                    break;
                }
            }

            _thisAttack = Attacks[_currentAttack];
        }
        private void MeleeStart()
        {
            ///// Deny Weapon Cycling and/or Movement
            //Owner.SetInputPermission(false, Stats.CanAttackAndMove, true);

            if (Stats.WeaponTrail != null)
            {
                Stats.WeaponTrail.SetActive(true);
            }

            DoFireEffect();
            Owner.DoWeaponFire();
        }
        private void MeleeTriggerToggle(bool status)
        {
            _myCollider.enabled = status;
        }
        private void MeleeCleanup()
        {
            _hitsDuringThisSwing.Clear();
            // Owner.SetInputPermission(true, !Stats.CanAttackAndMove, true);

            if (Stats.CanAttackAndMove && m_player != null)
                // m_player.InputPermission = true;
                if (Stats.WeaponTrail != null)
                    Stats.WeaponTrail.SetActive(false);
        }
        private void DoMeleeDamage(Entity toThis)
        {
            if (toThis == null) return;
            toThis.DoDamage(Attacks[_currentAttack].Damage, Owner);
        }

        // Shared
        private IEnumerator FireCooldown()
        {
            if (Stats.WeaponType == WeaponType.Melee) yield return new WaitForSeconds(Attacks[_currentAttack].Cooldown);
            else
            {
                yield return new WaitForSeconds(Stats.TimeToCooldown);
                if (Stats.FireStyle == FireStyle.SemiAuto)
                {
                    if (m_player == null) yield break;
                    while (m_player.GetInputFire1 > 0) yield return null;
                }
            }
        }
        private void DoFireEffect()
        {
            if (FireSounds.Count <= 0) return;
            int rng = FireSounds.Count > 0
                ? Random.Range(0, FireSounds.Count)
                : 0;
            if (FireSounds[rng] != null)
            {
                AudioSource.PlayClipAtPoint(FireSounds[rng], transform.position);
            }
        }
        private void DoImpactEffects(Vector3 position)
        {
            if (ImpactEffects.Count > 1)
            {
                if (_victimGameObject == null) return;

                for (int i = 0; i < ImpactTagNames.Count; i++)
                {
                    // check if the tag matches
                    if (_victimGameObject.CompareTag(ImpactTagNames[i]))
                    {
                        // if it does, we're done here.
                        PopFx(i, position);
                        break;
                    }

                    // if its the last entry and no match was found yet, default to the first entry.
                    if (i == ImpactEffects.Count) PopFx(0, position);
                }
            }
            else PopFx(0, position);
        }
        private void PopFx(int entry, Vector3 position)
        {
            if (ImpactSounds[entry] != null) AudioSource.PlayClipAtPoint(ImpactSounds[entry], position);
            else Debug.LogWarning(gameObject.name + " cannot spawn Impact sound because it is null. Check the Impact Tag List.");

            if (ImpactEffects[entry] != null) StaticUtil.Spawn(ImpactEffects[entry], position, Quaternion.identity);
            else Debug.LogWarning(gameObject.name + " cannot spawn Impact effect because it is null. Check the Impact Tag List.");
        }

        // Public
        public bool MagIsFull()
        {
            return m_currentAmmo >= Stats.MagazineSize;
        }

        public bool MagIsEmpty()
        {
            return m_currentAmmo <= 0;
        }

        public bool HasMags()
        {
            return Stats.CurrentMagazines > 0;
        }
    }
}