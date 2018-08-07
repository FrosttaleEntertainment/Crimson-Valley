using Prototype.NetworkLobby;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Base
{
    public enum AnimationState
    {
        AS_IDLE,
        AS_RELOADING,
        AS_SHOOTING
    }

    public class Entity : NetworkBehaviour
    {
        public EntityStats Stats = new EntityStats();
        public PlayerControllerStats ControlStats = new PlayerControllerStats();

        [SyncVar]
        public LobbyData LobbyData;

        [SyncVar(hook = "OnAnimationState")]
        public int AnimationState;

        public bool IsDead = false;

        // subscribe to the events below for callbacks.
        public delegate void SwitchedWeapon(GameObject currentWeapon);
        public event SwitchedWeapon OnSwitchedWeapon;
        public delegate void Reload();
        public event Reload OnReload;
        public delegate void ReloadFinished();
        public event Reload OnReloadFinished;
        public delegate void Fire(Entity whoFired); // potentially way too expensive. untested. use with caution.
        public event Fire OnFire;
        public delegate void Death();
        public event Death OnDeath;
        public delegate void Attacked();
        public event Attacked OnAttacked;
        public delegate void AttackedAlert(Entity dealer);
        public event AttackedAlert OnAttackedAlert;
        public delegate void MagazineEmpty();
        public event MagazineEmpty OnMagazineEmpty;
        public delegate void HealthChanged();
        public event HealthChanged OnHealthChanged;
        public delegate void GetPowerup();
        public event GetPowerup OnGetPowerup;
        public delegate void LevelChange();
        public event LevelChange OnLevelChange;

        //Weapons
        public List<GameObject> WeaponListEditor;
        public List<GameObject> WeaponListRuntime; // the list of physical weapons

        private int m_currentWeapon;
        private static Entity s_localPlayer;
        private Transform m_hand;

        [SyncVar]
        private bool m_switchingWeapons;

        //IK stuff
        public bool ShowGunDebug;
        public bool InvertHandForward;
        public Vector3 ThumbDirection = new Vector3(0, 0, -1);
        public Vector3 PalmDirection = new Vector3(0, 1, 0);
        public Vector3 DominantHandPosCorrection = new Vector3(0, 0, 0);
        public Vector3 DominantHandRotCorrection = new Vector3(90, -90, 0);
        public Vector3 NonDominantHandPosCorrection = new Vector3(0, 0, 0);
        public Vector3 NonDominantHandRotCorrection = new Vector3(0, 0, 0);
        public Vector3 WeaponPositionInHandCorrection = new Vector3(0, 0, 0);
        public int RightHandIkLayer = 0;
        public int LeftHandIkLayer = 1;

        private bool m_initialized;
        protected Animator m_animator;

        private static int m_createdPlayers = 0;

        private void Awake()
        {
            m_animator = GetComponent<Animator>();
            EntityRepository.Entities.Add(this);
        }

        private void OnDestroy()
        {
            EntityRepository.Entities.Remove(this);
        }

        private void Start()
        {
            if (Stats.EntityGroup == EntityGroup.Player && isLocalPlayer)
            {
                StartCoroutine(InputLoop());
            }

            m_currentWeapon = -1;
            bool weaponSetupIsOkay = true;

            if (Stats.UseMecanim)
            {
                if (Stats.AnimatorHostObj)
                {
                    m_hand = GetAnimator().GetBoneTransform(HumanBodyBones.RightHand);
                }
                else
                {
                    weaponSetupIsOkay = false;
                    Debug.LogWarning("No Animator Component was found! Assign the Animator Host Obj on the Subject.");
                }
            }
            else
            {
                if (Stats.WeaponMountPoint != null) m_hand = Stats.WeaponMountPoint.transform;
                else
                {
                    weaponSetupIsOkay = false;
                    if (WeaponListEditor.Count != 0)
                    {
                        Debug.LogWarning("No Weapon Mount Point is specified! Assign it on the Subject.");
                    }
                }
            }
            // Initialize if there were no problems.
            if (!weaponSetupIsOkay) return;

            // Create the predefined weapons, if any
            if (WeaponListEditor.Count == 0) return;

            var ins = this.netId;
            gameObject.name += ins;

            if (Stats.EntityGroup == EntityGroup.Player)
            {
                if (isLocalPlayer || GameController.Instance.IsSinglePlayer())
                {
                    s_localPlayer = this;
                }

                if (GameController.Instance.IsSinglePlayer())
                {
                    CmdLoadInitialWeapons();
                }
                else if (GameController.Instance.IsMultyPlayer())
                {
                    PlayersCreated++;

                    if (isServer)
                    {
                        LobbyManager.s_Singleton.AddEntity(this);
                    }

                    if (GameController.Instance.IsSinglePlayer() || isServer)
                    {
                        EntityRepository.Players.Add(this);
                    }

                    if (isLocalPlayer)
                    {
                        // It is safe to init the chat controller now
                        ChatController.Instance.Init(this);
                    }
                }
            }

            m_initialized = true;
        }

        [Command]
        void CmdOnClientPlayersReady()
        {
            LobbyManager.s_Singleton.ReadyPlayers++;
        }


        [Command]
        public void CmdLoadInitialWeapons()
        {
            foreach (GameObject boomboom in WeaponListEditor)
            {
                CmdCreateNewWeapon(boomboom);
            }
        }

        [ClientRpc]
        private void RpcOnWeaponAdded(GameObject toy, int changeToSlot)
        {
            if (!WeaponListRuntime.Contains(toy))
            {
                var ins = netId;
                Weapon wx = toy.GetComponent<Weapon>();
                wx.Owner = this;

                WeaponListRuntime.Add(toy);
                SetupWeapon(toy);
                toy.SetActive(false);
            }

            if (m_currentWeapon == -1)
            {
                m_currentWeapon = changeToSlot;
            }

            if (m_currentWeapon != -1)
            {
                ChangeWeaponToSlotImpl(m_currentWeapon);
            }
        }

        [Server]
        public void SwitchAnimation(AnimationState state)
        {
            AnimationState = (int)state;
        }

        public void OnAnimationState(int state)
        {
            AnimationState oldState = (AnimationState)AnimationState;
            AnimationState newState = (AnimationState)state;

            if (oldState != newState)
            {
                switch (newState)
                {
                    case Base.AnimationState.AS_SHOOTING:
                        {
                            m_animator.SetLayerWeight((int)PlayerAnimatorLayers.ShootingIdleOverride, 1);
                        }
                        break;
                    case Base.AnimationState.AS_IDLE:
                        {
                            m_animator.SetLayerWeight((int)PlayerAnimatorLayers.ShootingIdleOverride, 0);
                        }
                        break;
                    case Base.AnimationState.AS_RELOADING:
                        {
                            m_animator.SetLayerWeight((int)PlayerAnimatorLayers.ShootingIdleOverride, 0);
                            m_animator.SetTrigger("isReloading");
                        }
                        break;
                    default:
                        break;
                }
                // finally, switch the animations
                AnimationState = state;
            }
        }

        public static void ResetCharacterStatValues(EntityStats group)
        {
            // Base    Min     Max     PerLvl  Actual
            group.Level = new Stat(1, 0, 100, 1, 0);
            group.Experience = new Stat(0, 0, 50, 50, 0);
            group.XpReward = new Stat(25, 25, 100, 25, 25);

            group.Health = new Stat(100, 15, 100, 8, 15);
            group.Armor = new Stat(100, 0, 100, 1, 0);
            group.Agility = new Stat(0, 1, 100, 1.25f, 1);
            group.Dexterity = new Stat(0, 1, 100, 1.5f, 1);
            group.Endurance = new Stat(0, 1, 100, 2, 1);
            group.Strength = new Stat(0, 1, 100, 2.5f, 1);
        }

        public bool HasWeapon()
        {
            return m_currentWeapon != -1;
        }

        private IEnumerator InputLoop()
        {
            if (GetComponent<Intellect>() != null) yield break; // this is only relevant for Players

            var controls = GetComponent<PlayerController>();

            if (controls == null) yield break;

            while (true)
            {
                //while (!InputPermission) yield return null;
                if (!IsDead)
                {
                    bool shouldThrowGrenade = controls.GetThrowGrenade;
                    if (shouldThrowGrenade)
                    {
                        for (int i = 0; i < WeaponListRuntime.Count; i++)
                        {
                            Weapon wpn = WeaponListRuntime[i].GetComponent<Weapon>();
                            wpn.CmdForceFireOnce();
                        }
                    }
                }
                if (!IsDead && WeaponListRuntime.Count > 0)
                {
                    float changeWeapon = controls.GetInputChangeWeapon;
                    if (changeWeapon > 0 && !m_switchingWeapons) yield return StartCoroutine(ChangeWeaponToSlot(m_currentWeapon + 1));
                    if (changeWeapon < 0 && !m_switchingWeapons) yield return StartCoroutine(ChangeWeaponToSlot(m_currentWeapon - 1));
                    while ((int)controls.GetInputChangeWeapon != 0) yield return null;
                    if (controls.GetInputDropWeapon && !m_switchingWeapons)
                    {
                        StartCoroutine(DropCurrentWeapon());
                    }

                }
                yield return null;
            }

        }

        public int Armor
        {
            get { return (int)Stats.Armor.Actual; }
            set
            {
                Stats.Armor.Actual = value;
                if (Armor < Stats.Armor.Min) Stats.Armor.Actual = Stats.Armor.Min;
                if (Armor > Stats.Armor.Max) Stats.Armor.Actual = Stats.Armor.Max;
                if (OnHealthChanged != null) OnHealthChanged();
            }
        }
        public int Health
        {
            get { return (int)Stats.Health.Actual; }
            set
            {
                Stats.Health.Actual = value;
                if (Health <= Stats.Health.Min) Die();
                if (Health > Stats.Health.Max) Stats.Health.Actual = Stats.Health.Max;
                if (OnHealthChanged != null) OnHealthChanged();
            }
        }

        /// <summary> 
        /// Inflict damage to this subject 
        /// </summary>
        public void DoDamage(int damage, Entity dealer)
        {
            if (IsDead || !isServer)
            {
                return;
            }

            int damageProcessed = damage;

            #region #### Armor Calculation Block
            //if (Armor > 0)
            //{
            //    if (Stats.ArmorType == ArmorType.Fat)
            //    {
            //        Armor = Armor - damage; // Absorb damage
            //        if (Armor < 0) // If any armor is left, count as excess
            //        {
            //            damageProcessed = -Armor;
            //            Armor = 0;
            //        }
            //        else damageProcessed = 0;
            //    }
            //    else
            //    {
            //        damageProcessed = damage - Armor; // Nullify damage
            //        if (Armor < 0) // if any is left, count as excess
            //        {
            //            damageProcessed = -Armor;
            //            Armor = 0;
            //        }
            //    }
            //}
            #endregion

            if (damageProcessed < 0) damageProcessed = 0;
            else if (damageProcessed > Stats.Health.Actual)
            {
                damageProcessed = (int)Stats.Health.Actual;
            }

            Health -= damageProcessed;

            if (OnAttacked != null) OnAttacked();
            if (OnAttackedAlert != null) OnAttackedAlert(dealer);
        }


        /// <summary> 
        /// Begins the Subject's death. Subjects can be 'down-but-not-out' for a period of time, then completely die. 
        /// </summary>
        protected virtual void Die()
        {
            // Deny controls, reset mecanim inputs, reset velocity, turn off navmesh, turn off collider
            //if (_isControllable)
            //{
            //    SetInputPermission(false, false, false);
            //    _myControls.ResetMecanimParameters();
            //}
            //if (GetComponent<Intellect>() != null) GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;


            // if (_animator != null && Stats.UseMecanim && Stats.UseWeaponIk) _ikProxy.DisableHandIk();
            // if (Stats.UseMecanim && !IsDead && _animator != null) _animator.SetTrigger(ControlStats.AnimatorDie);
            if (Stats.DeathFx) StaticUtil.Spawn(Stats.DeathFx, transform.position, Quaternion.identity);
            if (OnDeath != null) OnDeath();

            // Update stats
            //Stats.Deaths++;
            //LastAttacker.Stats.Kills++;
            // StaticUtil.GiveXp((int)Stats.XpReward.Actual, LastAttacker);
            //

            IsDead = true;

            if (isServer)
            {
                StartCoroutine(StaticUtil.DestroyInternal(gameObject, 2f));
                //NetworkServer.Destroy(gameObject); //StartCoroutine(CrippleAndDie());
            }
        }


        /// <summary> 
        /// Broadcast Weapon has fired. 
        /// </summary>
        public void DoWeaponFire()
        {
            if (OnFire != null) OnFire(this);
        }

        /// <summary>
        /// Broadcast Weapon has reloaded
        /// </summary>
        public void DoWeaponReload()
        {
            if (OnReload != null) OnReload();
        }

        /// <summary>
        /// Broadcast Weapon magazine is empty.
        /// </summary>
        public void DoMagazineIsEmpty()
        {
            if (OnMagazineEmpty != null) OnMagazineEmpty();
        }

        /// <summary>
        /// Returns the [Animator] component from the Animator Host Obj
        /// </summary>
        public Animator GetAnimator()
        {
            if (!Stats.UseMecanim) return null;
            Animator foo = Stats.AnimatorHostObj.GetComponent<Animator>();
            return foo;
        }

        public NetworkAnimator GetNetworkAnimator()
        {
            if (!Stats.UseMecanim) return null;
            NetworkAnimator foo = Stats.AnimatorHostObj.GetComponent<NetworkAnimator>();
            return foo;
        }

        /// <summary> 
        /// Starts the pickup sequence 
        /// </summary>
        public void PickupWeapon(GameObject obj)
        {
            if (isLocalPlayer)
            {
                CmdCreateNewWeapon(obj);
            }

            if (WeaponListRuntime.Count == 1 && m_initialized) StartCoroutine(ChangeWeaponToSlot(WeaponListRuntime.Count - 1));
        }

        /// <summary> 
        /// Starts the pickup sequence on server 
        /// </summary>
        void CmdCreateNewWeapon(GameObject weaponPrefab)
        {
            if (weaponPrefab == null) return;
            if (m_hand == null)
            {
                Debug.LogError("Incorrect Rig Type on " + gameObject.name + "! Must be Humanoid. Check the Import Settings for this model and correct the type. Also confirm the Avatar Configuration has no errors.");
                Stats.UseMecanim = false;
                return;
            }

            GameObject newToy = (GameObject)StaticUtil.Spawn(weaponPrefab, m_hand.position, m_hand.rotation);
            Weapon wx = newToy.GetComponent<Weapon>();
            wx.Owner = this;

            WeaponListRuntime.Add(newToy);
            SetupWeapon(newToy);
            newToy.SetActive(false);

            if (m_currentWeapon == -1)
            {
                // This will dispatch msg to all clients
                m_currentWeapon = 0;
            }

            NetworkServer.Spawn(newToy);

            newToy.GetComponent<NetworkIdentity>().AssignClientAuthority(connectionToClient);
            var ins = netId;
            RpcOnWeaponAdded(newToy, m_currentWeapon);

            if (true) Debug.Log("Adding Weapon to Subject " + newToy.name + "...");
        }

        private void SetupWeapon(GameObject weapon)
        {
            weapon.transform.SetParent(m_animator.GetBoneTransform(HumanBodyBones.RightHand));

            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.rotation = FindWeaponOrientation();
        }

        private Quaternion FindWeaponOrientation()
        {
            Transform bone = m_animator.GetBoneTransform(HumanBodyBones.RightHand);
            return Quaternion.LookRotation(bone.right, bone.TransformDirection(-ThumbDirection));
        }

        /// <summary> 
        /// Drops the current Weapon into the scene 
        /// </summary>
        public IEnumerator DropCurrentWeapon()
        {
            m_switchingWeapons = true;
            //if (LogDebug) Debug.Log("Dropping current weapon in slot " + _currentWeapon);
            if (GetCurrentWeaponGo() == null || GetCurrentWeaponComponent().PickupReference == null)
            {
                Debug.Log("Either there is no weapon equippped or the weapon doesn't have a Pickup Reference defined.");
                m_switchingWeapons = false;
                yield break;
            }
            //StaticUtil.SpawnLoot(WeaponListRuntime[m_currentWeapon].GetComponent<Weapon>().PickupReference, transform.position, true);

            GameObject droppingThis = WeaponListRuntime[m_currentWeapon];
            WeaponListRuntime.RemoveAt(m_currentWeapon);
            if (WeaponListRuntime.Count == m_currentWeapon) m_currentWeapon -= 1;

            yield return StartCoroutine(ChangeWeaponToSlot(m_currentWeapon));
            m_switchingWeapons = true;

            StaticUtil.DeSpawn(droppingThis);
            var controls = GetComponent<PlayerController>();
            if (controls != null)
            {
                while (controls.GetInputDropWeapon) yield return null;
            }
            m_switchingWeapons = false;
        }

        /// <summary> 
        /// Cycles directly to a specific weapon slot 
        /// </summary>
        public IEnumerator ChangeWeaponToSlot(int index)
        {
            if (!isLocalPlayer)
                yield return null;

            CmdChangeWeaponToSlot(index);
        }

        /// <summary> 
        /// Cycles directly to a specific weapon slot 
        /// </summary>
        [Command]
        public void CmdChangeWeaponToSlot(int index)
        {
            m_switchingWeapons = true;

            if (WeaponListRuntime.Count == 0)
            {
                //if (LogDebug) Debug.Log("No weapons left to switch to. Disabling IK, sending null switch.");
                m_currentWeapon = 0;
                if (OnSwitchedWeapon != null) OnSwitchedWeapon(null);
                m_switchingWeapons = false;
                return;
            }

            ChangeWeaponToSlotImpl(index);

            RpcPlayerChangedWeapon(m_currentWeapon);

            return;
        }

        private IEnumerator SwitchWeapons(GameObject oldWep, Weapon newWep)
        {
            if (!isServer)
            {
                yield return null;
            }

            // halfway through the swap, flip them on/off
            yield return new WaitForSeconds(newWep.Stats.SwapTime / 2);
            oldWep.SetActive(false);
            newWep.gameObject.SetActive(true);
            yield return new WaitForSeconds(newWep.Stats.SwapTime / 2);

            m_switchingWeapons = false;
        }

        private static int PlayersCreated
        {
            get
            {
                return m_createdPlayers;
            }
            set
            {
                m_createdPlayers = value;

                int playersCount = 0;
                var lobbySlots = LobbyManager.s_Singleton.lobbySlots;
                for (int i = 0; i < lobbySlots.Length; ++i)
                {
                    if (lobbySlots[i] != null)
                    {
                        playersCount++;
                    }
                }

                if (m_createdPlayers == playersCount)
                {
                    //this client is ready to for weapon delivery
                    s_localPlayer.CmdOnClientPlayersReady();

                    // Show "Waiting for other players" text
                    MenuController.Instance.OnLocalLoaded();
                }
            }
        }

        /// <summary>
        /// Returns the current weapon as a GameObject
        /// </summary>
        public GameObject GetCurrentWeaponGo()
        {
            if (WeaponListRuntime.Count == 0) return null;
            GameObject foo = WeaponListRuntime[m_currentWeapon];
            return foo;
        }

        /// <summary>
        /// Returns the current weapon as a [Weapon] Component
        /// </summary>
        public Weapon GetCurrentWeaponComponent()
        {
            if (WeaponListRuntime.Count == 0) return null;
            Weapon foo = WeaponListRuntime[m_currentWeapon].GetComponent<Weapon>();
            return foo;
        }

        /// <summary>
        /// Returns the weapon's projectile spawn point
        /// </summary>
        public GameObject GetCurrentWeaponSpawnPt()
        {
            if (WeaponListRuntime.Count == 0) return null;
            GameObject foo = WeaponListRuntime[m_currentWeapon].GetComponent<Weapon>().Stats.ProjectileSpawnPoints[0];
            return foo;
        }

        [ClientRpc]
        public void RpcPlayerChangedWeapon(int index)
        {
            if (m_currentWeapon != index)
            {
                ChangeWeaponToSlotImpl(index);
            }
        }

        private void ChangeWeaponToSlotImpl(int index)
        {
            // turn off current weapon (old)
            GameObject oldWeapon = WeaponListRuntime[m_currentWeapon];

            // 1. target is under the first weapon, go to last weapon.
            // 2. target is over the last weapon, go to first weapon.
            // 3. target is somewhere in between, go to the desired index.
            if (index < 0) m_currentWeapon = WeaponListRuntime.Count - 1;
            else if (index >= WeaponListRuntime.Count) m_currentWeapon = 0;
            else m_currentWeapon = index;

            // tell everybody what just happened.
            if (OnSwitchedWeapon != null)
            {
                OnSwitchedWeapon(WeaponListRuntime[m_currentWeapon]);
            }

            Weapon newWeapon = WeaponListRuntime[m_currentWeapon].GetComponent<Weapon>();

            StartCoroutine(SwitchWeapons(oldWeapon, newWeapon));
        }
    }
}