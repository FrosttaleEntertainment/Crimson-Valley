using Base;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Base
{
    enum PlayerAnimatorLayers
    {
        RunMain,
        ReloadOverride,
        ShootingIdleOverride,
    }

    [RequireComponent(typeof(Entity))]
    public class PlayerController : NetworkBehaviour
    {
        public Vector3 ChestOffset;

        private Entity m_entity;
        private GameObject m_arbiter;
        private Rigidbody m_rigidBody;
        private Camera m_camera;
        private Animator m_animator;
        private NetworkAnimator m_networkAnimator;

        private Vector3 m_target;
        private Transform m_chest;

        private const string ChangeWeapon = "Mouse ScrollWheel";
        private const string Horizontal = "Horizontal";
        private const string Vertical = "Vertical";
        private const string Fire1 = "Fire1";
        private const string Fire2 = "Fire2";
        private const string Throw = "ThrowGrenade";
        private const string Reload = "Reload";
        private const string Interact = "Interact";
        private const string DropWeapon = "DropWeapon";

        public void SetShooting(bool state, float cooldown = 0)
        {
            if(state == true && Input.GetAxis("Horizontal") == 0 && Input.GetAxis("Vertical") == 0)
            {
                m_animator.SetLayerWeight((int)PlayerAnimatorLayers.ShootingIdleOverride, 1);
                IsShooting = true;
            } else
            {
                EndShooting();
            }
        }

        public void ReloadWeapon()
        {
            m_animator.SetTrigger("isReloading");
            EndShooting();
        }

        private void EndShooting()
        {
            m_animator.SetLayerWeight((int)PlayerAnimatorLayers.ShootingIdleOverride, 0);
            IsShooting = false;
        }


        private void Start()
        {
            if (GameController.Instance.IsMultyPlayer() &&  !isLocalPlayer)
            {
                Destroy(this);
                return;
            }

            m_entity = GetComponent<Entity>();
            m_camera = Camera.main;
            m_arbiter = m_camera.GetComponent<CameraControl>().GetArbiter();
            m_rigidBody = GetComponent<Rigidbody>();
            m_animator = m_entity.GetAnimator();

            m_camera.GetComponent<CameraControl>().Target = this.gameObject;

            if (m_entity)
            {
                EntityRepository.Players.Add(m_entity);
            }

            if(m_animator)
            {
                m_chest = m_animator.GetBoneTransform(HumanBodyBones.Chest);
            }
        }

        private void OnDestroy()
        {
            if (m_entity)
            {
                EntityRepository.Players.Remove(m_entity);
            }
        }

        private void FixedUpdate()
        {
            if (m_entity.IsDead == true)
            {
                return;
            }

            Move();
            Aim();
        }


        private void LateUpdate()
        {
            if(IsShooting && GetInputFire1 == 0)
            {
                EndShooting();
            }
        }

        private void Move()
        {
            var horizontalAxis = Input.GetAxis("Horizontal");
            var verticalAxis   = Input.GetAxis("Vertical");

            Vector3 direction = new Vector3(horizontalAxis * m_entity.Stats.MoveSpeed, 0, verticalAxis * m_entity.Stats.MoveSpeed);

            Vector3 movement = m_arbiter.transform.TransformDirection(direction);

            //TODO: hardcoded stuff fix this
            float multiplier = Vector3.Dot(movement,transform.forward) > -1.3f ? .01f : 0.005f;

            m_rigidBody.MovePosition(m_rigidBody.transform.position + movement * multiplier);

            var angle = SignedAngle(new Vector3(horizontalAxis, 0, verticalAxis), this.transform.forward);
            var normalizedAngle = NormalizeAngle(angle);
            normalizedAngle += Camera.main.transform.eulerAngles.y;

            var radianAngles = Mathf.Deg2Rad * normalizedAngle;
            Vector2 moveValues = Vector3.zero;

            if(horizontalAxis !=0 || verticalAxis !=0)
            {
                moveValues.x = Mathf.Sin(radianAngles);
                moveValues.y = Mathf.Cos(radianAngles);
            }

            float xVelocity = 0;
            float yVelocity = 0;
            float smoothTime = 0.05f;

            moveValues = new Vector2(Mathf.SmoothDamp(0, moveValues.x, ref xVelocity, smoothTime), Mathf.SmoothDamp(0, moveValues.y, ref yVelocity, smoothTime));
            
            if (xVelocity != 0)
            {
                m_animator.SetFloat("horizontal", xVelocity, 2f, Time.deltaTime);
            }
            else
            {
                m_animator.SetFloat("horizontal", xVelocity,0.01f,Time.deltaTime);
            }

            if(yVelocity != 0)
            {
                m_animator.SetFloat("vertical", yVelocity, 2f, Time.deltaTime);
            }
            else
            {

                m_animator.SetFloat("vertical", yVelocity, 0.01f, Time.deltaTime);
            }
          //
          //m_animator.SetBool("Running", movement.sqrMagnitude != 0);
          //m_animator.SetBool("Moving", movement.sqrMagnitude != 0);
        }

        private void Aim()
        {
            // Generate a plane that intersects the transform's position with an upwards normal.
            Plane playerPlane = new Plane(Vector3.up, transform.position);

            // Generate a ray from the cursor position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Determine the point where the cursor ray intersects the plane.
            // This will be the point that the object must look towards to be looking at the mouse.
            // Raycasting to a Plane object only gives us a distance, so we'll have to take the distance,
            //   then find the point along that ray that meets that distance.  This will be the point
            //   to look at.
            float hitdist = 0.0f;
            // If the ray is parallel to the plane, Raycast will return false.
            if (playerPlane.Raycast(ray, out hitdist))
            {
                // Get the point along the ray that hits the calculated distance.
                m_target = ray.GetPoint(hitdist);

                // Determine the target rotation.  This is the rotation if the transform looks at the target point.
                Quaternion targetRotation = Quaternion.LookRotation(m_target - transform.position);

                // Smoothly rotate towards the target point.
                m_rigidBody.rotation = Quaternion.Slerp(transform.rotation, targetRotation, m_entity.Stats.TurnSpeed * Time.deltaTime);
            }
        }

        private float SignedAngle(Vector3 a, Vector3 b)
        {
            return Vector3.Angle(a, b) * Mathf.Sign(Vector3.Cross(a, b).y);
        }

        private float NormalizeAngle(float angle)
        {
            return angle < 0 ? (angle * -1) : (360 - angle);
        }


        public float GetInputFire1 { get { return Input.GetAxis(Fire1); } }
        public float GetInputFire2 { get { return Input.GetAxis(Fire2); } }
        public bool GetThrowGrenade { get { return Input.GetButton(Throw); } }
        public bool GetInputInteract { get { return Input.GetButton(Interact); } }
        public bool GetInputDropWeapon { get { return Input.GetButton(DropWeapon); } }
        public bool GetInputReload { get { return Input.GetButton(Reload); } }
        public float GetInputChangeWeapon { get { return Input.GetAxisRaw(ChangeWeapon); } }

        public bool IsShooting { get; private set; }
        public bool IsReloading { get; private set; }

    }
}
