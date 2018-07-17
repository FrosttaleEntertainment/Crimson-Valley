// (c) Copyright Cleverous 2015. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Base
{
    public class Projectile : NetworkBehaviour
    {
        public ProjectileStats Stats;
        public LayerMask Mask;


        public GameObject AttackEffect;

        public List<string> ImpactTagNames = new List<string>();
        public List<AudioClip> ImpactSounds = new List<AudioClip>();
        public List<GameObject> ImpactEffects = new List<GameObject>();

        public enum ImpactType { ReflectOffHit, HitPointNormal, InLineWithShot }
        public ImpactType ImpactStyle = ImpactType.InLineWithShot;
        
        public Entity Owner;
        public GameObject DetachOnDestroy;

        private GameObject _go;
        private Vector3 _startPoint;
        private Vector3 _endPoint;
        private Vector3 _endNormal;
        private Entity _victim;
        private GameObject _victimGo;

        private List<Collider> _myColliders;
        private bool _despawning;
        private bool _firstRun = true;
        public bool LogDebug = false;

        void Awake()
        {
            if (_firstRun || Owner == null || Owner.IsDead) return;
            _despawning = true;
            // IgnoreCollision() kindly resets itself after being deactivated+reactivated... Safe for pooling. =]
            foreach (Collider c in _myColliders)
            {
                if (c != null) Physics.IgnoreCollision(c, Owner.GetComponent<Collider>());
            }

            Lifetimer.AddTimer(gameObject, Stats.Lifetime, true);
            Fire(_go.transform.position);
        }
        void Reset()
        {
            Stats = new ProjectileStats
            {
                Title = "Pewpew",
                weaponType = ProjectileStats.WeaponType.Standard,
                LineRenderer = gameObject.GetComponent<LineRenderer>(),
                Speed = 40f,
                Damage = 10,
                MaxDistance = 10f,
                Lifetime = 4f,
                Bouncer = false,
                UsePhysics = true,
                ConstantForce = true,
                CauseAoeDamage = false,
                AoeRadius = 5,
                AoeForce = 50
            };
            ImpactSounds = new List<AudioClip>();
            AttackEffect = null;
            ImpactEffects = new List<GameObject>();
        }
        void Start()
        {
            _myColliders = GetComponentsInChildren<Collider>().ToList();
            _go = gameObject;
            _firstRun = false;
            Awake();
        }

        void OnCollisionEnter(Collision col) // Handles hits for Standard Type.
        {

            if(col == null)
            {
                return;
            }

            if(Stats.weaponType == ProjectileStats.WeaponType.ThrowableExplosive)
            {
                //add grenade logic here
                return;
            }

            if (col.collider.CompareTag("Enemy") && Stats.CauseAoeDamage == false)
            {
                _victimGo = col.gameObject;
                _victim = _victimGo.GetComponent<Entity>();



                if (_victim != null)
                {
                    DoDamageToVictim();
                }
                else
                {
                    _victim = _victimGo.GetComponentInParent<Entity>();
                    if(_victim != null)
                    {
                        DoDamageToVictim();
                    }
                }

                _endPoint = col.contacts[0].point;
                SetupImpactNormal(col.contacts[0].normal);
                FinishImpact();
            }
            if (!Stats.CauseAoeDamage) // I cause damage to what I collided into.
            {
                _victimGo = col.gameObject;
                _victim = _victimGo.GetComponent<Entity>();

                if (StaticUtil.LayerMatchTest(Mask, _victimGo))
                {
                    if (_victim != null) DoDamageToVictim();

                    _endPoint = col.contacts[0].point;
                    SetupImpactNormal(col.contacts[0].normal);
                    //PopFx(GetCorrectFx(col.collider.gameObject));
                    FinishImpact();
                }
                else if(_myColliders != null)
                {
                    foreach (Collider z in _myColliders)
                    {
                        Physics.IgnoreCollision(z, col.collider);
                    }
                }
            }
            else if (Stats.CauseAoeDamage && !Stats.Bouncer) DoDamageAoe(); // I cause AoE immediately when I hit something.
        }

        private void Fire(Vector3 fromPos)
        {
            _startPoint = fromPos;
            DoMuzzleFlash();

            #region Standard Type
            if (Stats.weaponType == ProjectileStats.WeaponType.Standard)
            {
                Mover.AddMover(gameObject, Stats.UsePhysics, Stats.Speed, Stats.ConstantForce, Owner, Mask);
            }
            #endregion

            #region ThrowableExplosive Type
            if (Stats.weaponType == ProjectileStats.WeaponType.ThrowableExplosive)
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit,Mask))
                {
                    var target = hit.point;
                    //hard coded speed not used
                    ParabolaMover.AddMover(gameObject, target, 20f, Owner, Mask);
                }
            }
            #endregion

            #region Raycast Type
            // TODO build solution: How should Raycast type work? Ouput start/end for a 3rd party script? Include elaborate trail system?
            if (Stats.weaponType == ProjectileStats.WeaponType.Raycast)
            {
                Vector3 dir = _go.transform.TransformDirection(Vector3.forward);

                RaycastHit hit;
                if (Physics.Raycast(_startPoint, dir, out hit, Stats.MaxDistance, Mask))
                {
                    // This is a hit.
                    _victimGo = hit.collider.gameObject;
                    _victim = _victimGo.GetComponentInParent<Entity>();
                    Debug.Log(_victim);
                    if (_victim != null) DoDamageToVictim(); // only Subject's can be damaged.

                    _endNormal = hit.normal;
                    _endPoint = hit.point;
                    if (LogDebug)
                    {
                        Debug.Log(name + " registered a Hit on " + _victimGo.name);
                    }
                }
                else
                {
                    // This is a miss.
                    _endPoint = _startPoint + dir * Stats.MaxDistance;
                    //Owner.Stats.ShotsMissed++;
                    if (LogDebug)
                    {
                        Debug.Log(name + " registered a Miss");
                    }
                }

                DrawRayFx();
                FinishImpact();
            }
            #endregion
        }

        [Server]
        private void DoDamageToVictim()
        {
            _victim.DoDamage(Stats.Damage, Owner);
        }

        private void DoDamageAoe()
        {
            // could use foo.SendMessage, but it is sloppy... Rather pay for GetComponent instead.
            Ray ray = new Ray(transform.position, Vector3.up);
            RaycastHit[] hits = Physics.SphereCastAll(ray, Stats.AoeRadius, 0.1f, Mask);
            foreach (RaycastHit thisHit in hits)
            {
                _victimGo = thisHit.collider.gameObject;
                _victim = _victimGo.GetComponent<Entity>();

                if (Stats.AoeForce > 0)
                {
                    Rigidbody rb = _victimGo.GetComponent<Rigidbody>();
                    if (rb != null) rb.AddExplosionForce(Stats.AoeForce, transform.position, Stats.AoeRadius);
                }

                if (_victim != null)
                {
                    _victim.DoDamage(Stats.Damage, Owner);
                    //Owner.Stats.DamageDealt += Stats.Damage;
                }

                // TODO Hit FX
                // Hit FX per AoE contact not yet working.
                //
                // _endPoint = thisHit.point;
                // SetupImpactNormal(thisHit.normal);
                // PopFx(GetCorrectFx(thisHit.collider.gameObject));

                FinishImpact();
            }

            if (Stats.AoeEffect != null) StaticUtil.Spawn(Stats.AoeEffect, transform.position, Quaternion.identity);
        }

        private void DoMuzzleFlash()
        {
            // TODO should muzzle flash be on the projectile or the weapon? Poll users for suggestions.
            //StaticUtil.Spawn(AttackEffect, transform.position, transform.rotation);
        }
        private void DrawRayFx() // TODO decide how how handle the Raycast Type's behavior.
        {
            LineRenderer line = GetComponent<LineRenderer>();
            if (line == null) return;

            Stats.LineRenderer.SetPosition(0, _startPoint);
            Stats.LineRenderer.SetPosition(1, _endPoint);
        }

        private void SetupImpactNormal(Vector3 hitNormal)
        {
            switch (ImpactStyle)
            {
                case (ImpactType.InLineWithShot):
                    _endNormal = -transform.forward;                                                // standard or raycast
                    break;
                case (ImpactType.HitPointNormal):
                    _endNormal = (Stats.weaponType == ProjectileStats.WeaponType.Standard)
                        ? hitNormal                                                                 // standard
                        : _endNormal;                                                               // raycast
                    break;
                case (ImpactType.ReflectOffHit):
                    _endNormal = (Stats.weaponType == ProjectileStats.WeaponType.Standard)
                        ? Vector3.Reflect(transform.forward, hitNormal)                             // standard
                        : Vector3.Reflect(transform.forward, _endNormal);                           // raycast
                    break;
            }
        }
        private int GetCorrectFx(GameObject victim)
        {
            if (ImpactEffects.Count <= 1 || victim == null) return 0;
            for (int i = 0; i < ImpactTagNames.Count; i++)
            {
                if (victim.CompareTag(ImpactTagNames[i])) return i;
            }
            return 0;
        }
        private void PopFx(int index)
        {
            if (ImpactSounds[index] != null) AudioSource.PlayClipAtPoint(ImpactSounds[index], _endPoint);
            else Debug.LogWarning(gameObject.name + " cannot spawn Impact sound because it is null. Check the Impact Tag List.");

            if (ImpactEffects[index] != null) StaticUtil.Spawn(ImpactEffects[index], _endPoint, Quaternion.LookRotation(_endNormal));
            else Debug.LogWarning(gameObject.name + " cannot spawn Impact effect because it is null. Check the Impact Tag List.");
        }
        private void FinishImpact()
        {
            if(!isServer)
            {
                return;
            }

            // TODO how is this supposed to work with pooling? Do I need to insanely nest pool the detachable? :S
            if (DetachOnDestroy != null) DetachOnDestroy.transform.SetParent(null);

            // Only for Standard Type, relying on Lifetimer to despawn Raycast Type. (because trails, etc..)
            if (Stats.weaponType == ProjectileStats.WeaponType.Standard)
            {
                DeSpawn();
            }
        }

        public void Spawn()
        {
            // Pooling TBD
            gameObject.SetActive(true);
        }
        public void DeSpawn()
        {
            if (!isServer)
            {
                return;
            }

            // using DeSpawn() to apply aoe dmg?... not sure if that is okay...
            if (Stats.CauseAoeDamage && Stats.Bouncer && !_despawning)
            {
                _despawning = true;
                DoDamageAoe();
            }

            NetworkServer.Destroy(gameObject);
        }
    }
}