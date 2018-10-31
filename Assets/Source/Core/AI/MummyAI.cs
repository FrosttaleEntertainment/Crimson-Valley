using Invector.vCharacterController;
using Invector.vCharacterController.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Networking;

namespace Base
{
    [RequireComponent(typeof(v_AIController))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class MummyAI : NetworkBehaviour
    {
        public enum Mood { Wander, Chase, Attack, Dead }

        [Header("Wander options")]
        public float WanderTime = 6.5f;
        public float WanderRadius = 5f;
        [Header("Attack options")]
        public float AttackRange = 1f;
        public float SightRange = 12f;
        public LayerMask ThreatMask;
        public float ScanFrequency = 1.0f;

        public Transform LeftHand;
        public Transform RightHand;


        private float m_lastScan;
        private float m_lastWander;
        private Mood m_mood;
        private v_AIController m_entity;
        private List<vThirdPersonController> m_threatList = new List<vThirdPersonController>();

        private vThirdPersonController m_target;
        private TreeOfLife m_treeOfLife;
        private AgentAttackRangeModifier m_attackRangeModifier;
        private Vector3 m_targetColliderLength;

        private NavMeshAgent m_agent;
        private Animator m_animator;

        private Vector3 m_startPos;

        private float m_startSpeed = 0.1f;
        private float m_stopSpeed = 0.1f;
        private float m_lastSpeed = 0.1f;

        private Vector2 m_smoothDeltaPosition = Vector2.zero;
        private Vector2 m_velocity = Vector2.zero;


        //gizmos
        private static float _editorGizmoSpin;

        [Server]
        public void HitTarget(string parameters)
        { 
            var arguments = parameters.Split('|');
            var emissionPoint = arguments[0];
            var hitRange = float.Parse(arguments[1]);
            List<vThirdPersonController> targets;

            switch (emissionPoint)
            {
                case "left":
                    targets = FindTargetsToHit(LeftHand, hitRange);
                    break;
                case "right":
                    targets = FindTargetsToHit(RightHand, hitRange);
                    break;
                default:
                    targets = new List<vThirdPersonController>();
                    break;
            }

           if(targets.Count > 0)
           {
                DoDamage(targets);
           }
        }

        [Server] 
        private List<vThirdPersonController> FindTargetsToHit(Transform emissionPoint, float hitRange)
        {
            List<vThirdPersonController> entities = new List<vThirdPersonController>();

            var playerLayerMask = 1 << 8;
            Collider[] hitColliders = Physics.OverlapSphere(emissionPoint.position, hitRange, playerLayerMask);

            foreach (var hit in hitColliders)
            {
                var entity = hit.GetComponent<vThirdPersonController>();
                if(entity)
                {
                    entities.Add(entity);
                }
            }

            return entities;
        }

        [Server]
        private void DoDamage(List<vThirdPersonController> targets)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                //targets[i].DoDamage(5, m_entity);
            }
        }




        [Server]
        private void Awake()
        {
            m_mood          = Mood.Wander;
            m_lastScan      = Time.time;
            m_lastWander    = Time.time;
            m_startPos      = this.transform.position;
            m_entity        = GetComponent<v_AIController>();
            m_agent         = GetComponent<NavMeshAgent>();
            m_animator      = GetComponent<Animator>();
            
            //TODO Fix
            //m_entity.OnAttackedAlert += AlertOnHit;
            //m_entity.Stats.MoveSpeed = Random.Range(1.3f, 3f);

            m_treeOfLife = FindObjectOfType<TreeOfLife>();

            if(m_treeOfLife)
            {
                m_target = m_treeOfLife.GetComponent<vThirdPersonController>();
            }

            AgentConfigure();
        }

        [Server]
        private void AgentConfigure()
        {
            if (m_agent && m_entity)
            {
                m_agent.stoppingDistance    = 0;
                //m_agent.autoBraking         = false;

                //TODO Fix
                //m_agent.angularSpeed        = 100f * m_entity.Stats.TurnSpeed;
                //m_agent.speed               = m_entity.Stats.MoveSpeed;
                m_agent.acceleration        = 100f;
                m_agent.updatePosition = false;
            }
        }
        
        [Server]
        void Update()
        {
            if (m_entity.isDead == true)
            {
                SetAgentState(false);
                return;
            }

            if(m_lastScan + ScanFrequency >= Time.time)
            {
                ScanForThreats();
            }


            SetupAnimatorSpeed();
            ProcessState();
        }

        [Server]
        private void SetupAnimatorSpeed()
        {
            m_lastSpeed = m_animator.GetFloat("MoveSpeed");

            if(m_lastSpeed > m_agent.velocity.magnitude)
            {

                m_animator.SetFloat("MoveSpeed", Mathf.Lerp(m_lastSpeed, m_agent.velocity.magnitude, m_stopSpeed));
            }
            else
            {

                m_animator.SetFloat("MoveSpeed", Mathf.Lerp(m_lastSpeed, m_agent.velocity.magnitude, m_startSpeed));
            }
        }

        [Server]
        private void OnAnimatorMove()
        {
            transform.position = m_agent.nextPosition;
        }

        [Server]
        private void ProcessState()
        {
            switch (m_mood)
            {
                case MummyAI.Mood.Wander:
                    Wander();
                    break;
                case MummyAI.Mood.Chase:
                    Chase();
                    break;
                case MummyAI.Mood.Attack:
                    Attack();
                    break;
                default:
                    break;
            }
        }

        #region STATES

        [Server]
        private void Wander()
        {
            StartChase();
        }

        [Server]
        private void Chase()
        {
            GetClosestThreat();

            if (m_target)
            {
                m_agent.SetDestination(m_target.transform.position);
                m_animator.SetBool("IsMoving", true);
                var distance = StaticUtil.FastDistance(m_target.transform.position, this.transform.position);
                if ((m_attackRangeModifier && distance <= m_attackRangeModifier.AgentAttackRange) || distance < AttackRange)
                {
                    m_mood = Mood.Attack;
                    RotateTowardsTarget();
                    Attack();
                }
            }
        }


        [Server]
        private void Attack()
        {
            var distance = StaticUtil.FastDistance(m_target.transform.position, this.transform.position);
            if ((m_attackRangeModifier && distance <= m_attackRangeModifier.AgentAttackRange) || distance < AttackRange)
            {
                m_animator.SetTrigger("IsAttacking");
                RotateTowardsTarget();
                m_mood = Mood.Attack;
            } else
            {
                m_mood = Mood.Chase;
            }
        }
        #endregion


        [Server]
        private void StartChase()
        {
            GetClosestThreat();

            if(m_target)
            {
                m_mood = Mood.Chase;

                m_attackRangeModifier = m_target.GetComponent<AgentAttackRangeModifier>();
            }
        }

        [Server]
        private void RotateTowardsTarget()
        {
            var vec1 = m_target.transform.position;
            vec1.y = 0;
            var vec2 = transform.position;
            vec2.y = 0;
            Vector3 direction = (vec1 - vec2).normalized;

            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * m_agent.angularSpeed);
        }

        [Server]
        private void StartChase(vThirdPersonController target)
        {
            m_target = target;

            var col = target.GetComponent<Collider>();
            var colBounds = col.bounds;
            m_targetColliderLength = colBounds.extents;

            m_mood = Mood.Chase;
        }

        [Server]
        private void SetAgentState(bool state)
        {
            if(state == true)
            {
                m_agent.enabled = true;
            } else
            {
                m_agent.enabled = false;
            }
        }

        [Server]
        private void AlertOnHit(vThirdPersonController attacker)
        {
            if (attacker)
            {
                m_target = attacker;
                StartChase();
            }
        }

        [Server]
        private void GetClosestThreat()
        {
            float closest = -1;
            var previousTarget = m_target;
            for (int i = 0; i < m_threatList.Count; i++)
            {
                float currentDist = StaticUtil.FastDistance(this.transform.position, m_threatList[i].transform.position);
                if (currentDist > closest)
                {
                    closest = currentDist;
                    if(m_target != m_threatList[i])
                    {
                        m_target = m_threatList[i];

                        if(m_target != previousTarget)
                        {
                            m_attackRangeModifier = m_target.GetComponent<AgentAttackRangeModifier>();
                        }
                        break;
                    }
                }
            }
        }

        [Server]
        private void ScanForThreats()
        {
            for (int i = 0; i < vThirdPersonControllerRepository.Players.Count; i++)
            {
                if (StaticUtil.FastDistance(this.transform.position, vThirdPersonControllerRepository.Players[i].transform.position) < SightRange )
                {
                    if(m_threatList.Contains(vThirdPersonControllerRepository.Players[i]) == false)
                    {
                        m_threatList.Add(vThirdPersonControllerRepository.Players[i]);
                    }
                }
            }

            m_lastScan = Time.time;
        }

        [Server]
        private void DefineAsThreat(vThirdPersonController target)     // add a subject to the threat list
        {
            if (m_threatList.Contains(target))
            {
                if (target.isDead)
                {
                    m_threatList.Remove(target);
                }
                else
                {
                    return;
                }
            }

            m_threatList.Add(target);

        }


        void OnDrawGizmos()
        {
            _editorGizmoSpin += 0.02f;
            if (_editorGizmoSpin > 360) _editorGizmoSpin = 0;
        }

        void OnDrawGizmosSelected()
        {
            // SIGHT range
            Gizmos.color = Color.grey;
            Gizmos.DrawRay(transform.position, Quaternion.Euler(0, _editorGizmoSpin, 0) * new Vector3(0, 0, SightRange));
            Gizmos.DrawRay(transform.position, Quaternion.Euler(0, _editorGizmoSpin, 0) * new Vector3(0, 0, -SightRange));
            Gizmos.DrawRay(transform.position, Quaternion.Euler(0, _editorGizmoSpin, 0) * new Vector3(SightRange, 0, 0));
            Gizmos.DrawRay(transform.position, Quaternion.Euler(0, _editorGizmoSpin, 0) * new Vector3(-SightRange, 0, 0));
            Gizmos.DrawRay(transform.position, Quaternion.Euler(0, _editorGizmoSpin + 45, 0) * new Vector3(0, 0, SightRange));
            Gizmos.DrawRay(transform.position, Quaternion.Euler(0, _editorGizmoSpin + 45, 0) * new Vector3(0, 0, -SightRange));
            Gizmos.DrawRay(transform.position, Quaternion.Euler(0, _editorGizmoSpin + 45, 0) * new Vector3(SightRange, 0, 0));
            Gizmos.DrawRay(transform.position, Quaternion.Euler(0, _editorGizmoSpin + 45, 0) * new Vector3(-SightRange, 0, 0));
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, SightRange);



#if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
#endif
        }

    }
}