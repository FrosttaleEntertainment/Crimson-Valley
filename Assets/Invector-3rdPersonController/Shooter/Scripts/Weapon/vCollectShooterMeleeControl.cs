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

        public override void HandleCollectableInput(vCollectableStandalone collectableStandAlone)
        {
            if (isLocalPlayer)
            {
                CmdHandleCollectableInput(collectableStandAlone.weapon);
            }
        }

        [Command]
        public void CmdHandleCollectableInput(GameObject collectableObj)
        {
            try
            {                
                InternalHandleCollectableInput(collectableObj.GetComponentInChildren<vCollectableStandalone>());

                //base.CmdHandleCollectableInput(collectableId);
            }
            catch (System.Exception)
            {
                Debug.Log("Error equiping weapon on server");
                return;                
            }

            RpcHandleCollectableInput(collectableObj);            
        }

        [ClientRpc]
        public void RpcHandleCollectableInput(GameObject collectableObj)
        {
            try
            {
                InternalHandleCollectableInput(collectableObj.GetComponentInChildren<vCollectableStandalone>());

                //base.RpcHandleCollectableInput(collectableId);
            }
            catch (System.Exception)
            {
                Debug.Log("Error equiping weapon on client");
                return;
            }
        }

        protected override void InternalHandleCollectableInput(vCollectableStandalone collectableStandAlone)
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
                                RemoveLeftWeapon();
                            }

                            shooterManager.SetLeftWeapon(weapon.gameObject);
                            collectableStandAlone.OnEquip.Invoke();

                            if (isServer)
                            {
                                SetAuthority(collectableStandAlone.weapon.GetComponent<NetworkIdentity>(), gameObject.GetComponent<NetworkIdentity>());
                            }

                            leftWeapon = weapon.gameObject;
                            UpdateLeftDisplay(collectableStandAlone);

                            if (rightWeapon)
                            {

                                RemoveRightWeapon();
                            }
                        }
                        else
                        {
                            throw new System.Exception("Invalid equipment point");
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
                                RemoveRightWeapon();
                            }

                            shooterManager.SetRightWeapon(weapon.gameObject);
                            collectableStandAlone.OnEquip.Invoke();

                            if (isServer)
                            {
                                SetAuthority(collectableStandAlone.weapon.GetComponent<NetworkIdentity>(), gameObject.GetComponent<NetworkIdentity>());
                            }

                            rightWeapon = weapon.gameObject;
                            UpdateRightDisplay(collectableStandAlone);

                            if (leftWeapon)
                            {
                                RemoveLeftWeapon();
                            }
                        }
                        else
                        {
                            throw new System.Exception("Invalid equipment point");
                        }
                    }
                }
                else
                {
                    throw new System.Exception("Invalid collectable");
                }
            }
            else
            {
                throw new System.Exception("Invalid collectable");
            }

            base.InternalHandleCollectableInput(collectableStandAlone);
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

