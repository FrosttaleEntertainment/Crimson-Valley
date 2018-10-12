using UnityEngine;
using System.Collections;

namespace Invector.vMelee
{
    using UnityEngine.Networking;
    using vCharacterController.vActions;
    using vShooter;
    public class vCollectShooterMeleeControl : vCollectMeleeControl
    {
        protected vShooterManager shooterManager;

        protected override void Start()
        {
            base.Start();
            shooterManager = GetComponent<vShooterManager>();
        }

        [Command]
        void CmdSetAuthority(NetworkIdentity grabID, NetworkIdentity playerID)
        {
            grabID.AssignClientAuthority(playerID.connectionToClient);
        }

        [Command]
        void CmdRemoveAuthority(NetworkIdentity grabID, NetworkIdentity playerID)
        {
            grabID.RemoveClientAuthority(playerID.connectionToClient);
        }

        public override void HandleCollectableInput(vCollectableStandalone collectableStandAlone)
        {
            if (shooterManager && collectableStandAlone != null && collectableStandAlone.weapon != null)
            {
                var weapon = collectableStandAlone.weapon.GetComponent<vShooterWeapon>();
                if (weapon)
                {
                    Transform p = null;
                    if (weapon.isLeftWeapon)
                    {
                        p = GetEquipPoint(leftHandler, collectableStandAlone.targetEquipPoint);
                        if (p)
                        {
                            collectableStandAlone.weapon.transform.SetParent(p);
                            collectableStandAlone.weapon.transform.localPosition = Vector3.zero;
                            collectableStandAlone.weapon.transform.localEulerAngles = Vector3.zero;

                            if (leftWeapon && leftWeapon != weapon.gameObject)
                            {
                                if (isLocalPlayer)
                                {
                                    CmdRemoveAuthority(leftWeapon.GetComponent<NetworkIdentity>(), gameObject.GetComponent<NetworkIdentity>());
                                }

                                RemoveLeftWeapon();
                            }

                            shooterManager.SetLeftWeapon(weapon.gameObject);
                            collectableStandAlone.OnEquip.Invoke();

                            if (isLocalPlayer)
                            {
                                CmdSetAuthority(collectableStandAlone.weapon.GetComponent<NetworkIdentity>(), gameObject.GetComponent<NetworkIdentity>());
                            }

                            leftWeapon = weapon.gameObject;
                            UpdateLeftDisplay(collectableStandAlone);

                            if (rightWeapon)
                            {
                                if (isLocalPlayer)
                                {
                                    CmdRemoveAuthority(rightWeapon.GetComponent<NetworkIdentity>(), gameObject.GetComponent<NetworkIdentity>());
                                }

                                RemoveRightWeapon();
                            }
                        }
                    }
                    else
                    {
                        p = GetEquipPoint(rightHandler, collectableStandAlone.targetEquipPoint);
                        if (p)
                        {
                            collectableStandAlone.weapon.transform.SetParent(p);
                            collectableStandAlone.weapon.transform.localPosition = Vector3.zero;
                            collectableStandAlone.weapon.transform.localEulerAngles = Vector3.zero;

                            if (rightWeapon && rightWeapon != weapon.gameObject)
                            {
                                if (isLocalPlayer)
                                {
                                    CmdRemoveAuthority(rightWeapon.GetComponent<NetworkIdentity>(), gameObject.GetComponent<NetworkIdentity>());
                                }

                                RemoveRightWeapon();
                            }

                            shooterManager.SetRightWeapon(weapon.gameObject);
                            collectableStandAlone.OnEquip.Invoke();

                            if (isLocalPlayer)
                            {
                                CmdSetAuthority(collectableStandAlone.weapon.GetComponent<NetworkIdentity>(), gameObject.GetComponent<NetworkIdentity>());
                            }

                            rightWeapon = weapon.gameObject;
                            UpdateRightDisplay(collectableStandAlone);

                            if (leftWeapon)
                            {
                                if (isLocalPlayer)
                                {
                                    CmdRemoveAuthority(leftWeapon.GetComponent<NetworkIdentity>(), gameObject.GetComponent<NetworkIdentity>());
                                }

                                RemoveLeftWeapon();
                            }
                        }
                    }
                }
            }
            base.HandleCollectableInput(collectableStandAlone);
        }

        protected override void RemoveRightWeapon()
        {
            base.RemoveRightWeapon();
            if (shooterManager)            
                shooterManager.rWeapon = null;            
        }

        protected override void RemoveLeftWeapon()
        {
            base.RemoveLeftWeapon();
            if (shooterManager)
                shooterManager.lWeapon = null;
        }
    }
}

