﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Networking;

namespace Base
{
    [RequireComponent(typeof(Entity))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class ZombieAI : NetworkBehaviour
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


        private float m_lastScan;
        private float m_lastWander;
        private Mood m_mood;
        private Entity m_entity;
        private List<Entity> m_threatList = new List<Entity>();

        private Entity m_target;

        private NavMeshAgent m_agent;
        private Animator m_animator;

        private Vector3 m_startPos;

        private float m_startSpeed = 0.1f;
        private float m_stopSpeed = 0.01f;
        private float m_lastSpeed = -1f;


        //gizmos
        private static float _editorGizmoSpin;


        [Server]
        private void Awake()
        {
            m_mood          = Mood.Wander;
            m_lastScan      = Time.time;
            m_lastWander    = Time.time;
            m_startPos      = this.transform.position;
            m_entity        = GetComponent<Entity>();
            m_agent         = GetComponent<NavMeshAgent>();
            m_animator      = GetComponent<Animator>();

            m_entity.OnAttackedAlert += AlertOnHit;
            m_entity.Stats.MoveSpeed = Random.Range(1f, 2.8f);

            AgentConfigure();
        }

        [Server]
        private void AgentConfigure()
        {
            if (m_agent && m_entity)
            {
                m_agent.stoppingDistance    = 0.2f;
                //m_agent.autoBraking         = false;
                m_agent.angularSpeed        = 100f * m_entity.Stats.TurnSpeed;
                m_agent.speed               = m_entity.Stats.MoveSpeed;
                m_agent.acceleration        = 100f;
                m_agent.updatePosition = false;
            }
        }
        
        [Server]
        void Update()
        {
            if (m_entity.IsDead == true)
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
                case ZombieAI.Mood.Wander:
                    Wander();
                    break;
                case ZombieAI.Mood.Chase:
                    Chase();
                    break;
                case ZombieAI.Mood.Attack:
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
            if (m_threatList.Count > 0)
            {
                StartChase();

            }
        }

        [Server]
        private void Chase()
        {
            GetClosestThreat();

            if (m_target)
            {
                m_agent.SetDestination(m_target.transform.position);
                m_animator.SetBool("IsMoving", true);
                if (StaticUtil.FastDistance(m_target.transform.position, this.transform.position) <= AttackRange)
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
            if (StaticUtil.FastDistance(m_target.transform.position, this.transform.position) <= AttackRange)
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

            m_mood = Mood.Chase;
        }

        [Server]
        private void RotateTowardsTarget()
        {
            Vector3 direction = (m_target.transform.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * m_agent.angularSpeed);
        }

        [Server]
        private void StartChase(Entity target)
        {
            m_target = target;

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
        private void AlertOnHit(Entity attacker)
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
            for (int i = 0; i < m_threatList.Count; i++)
            {
                float currentDist = StaticUtil.FastDistance(this.transform.position, m_threatList[i].transform.position);
                if (currentDist > closest)
                {
                    closest = currentDist;
                    if(m_target != m_threatList[i])
                    {
                        m_target = m_threatList[i];
                    }
                }
            }
        }

        [Server]
        private void ScanForThreats()
        {
            for (int i = 0; i < EntityRepository.Players.Count; i++)
            {
                if (StaticUtil.FastDistance(this.transform.position, EntityRepository.Players[i].transform.position) < SightRange )
                {
                    if(m_threatList.Contains(EntityRepository.Players[i]) == false)
                    {
                        m_threatList.Add(EntityRepository.Players[i]);
                    }
                }
            }

            m_lastScan = Time.time;
        }

        [Server]
        private void DefineAsThreat(Entity target)     // add a subject to the threat list
        {
            if (m_threatList.Contains(target))
            {
                if (target.IsDead)
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