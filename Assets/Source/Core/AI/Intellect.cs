// (c) Copyright Cleverous 2015. All rights reserved.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
namespace Base
{
    [RequireComponent(typeof(Entity))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class Intellect : MonoBehaviour
    {
        #region ### Variable Definitions

        // DATA STUFF
        public bool DrawGizmos;
        public int IgnoreUpdates; // how many *Frames* to skip before running the **Process**.
        public int SenseFrequency; // how many **Processes** to skip between sensory updates.

        public CapsuleCollider ViewPoint;

        // PROVOKING AND ALLIES
        public enum ProvokeType { TargetIsInRange, TargetAttackedMe }
        public enum ThreatPriority { Nearest, MostDamage }
        public ProvokeType MyProvokeType;
        public ThreatPriority MyThreatPriority;
        public float ProvokeDelay;
        public bool HelpAllies;
        public int MaxAllyCount = 10;
        public bool Provoked;
        public int FleeHealthThreshold;
        public float AlertTime;

        public int RetargetDamageThreshold;

        // JUKE SETUP
        public float JukeTime;
        public float JukeFrequency;
        public float JukeFrequencyRandomness;
        private bool _juking;
        private Vector3 _jukeHeading;

        // RANGES AND MASKS
        public float FieldOfView;
        public float SightRange;
        public float PursueRange;
        public float AttackRange;
        public float WanderRange;
        public float EngageThreshold;
        public LayerMask SightMask;
        public LayerMask ThreatMask;

        // UNIMPLEMENTED
        public List<GameObject> PatrolPoints;

        // NAVIGATION
        public float MaxDeviation;
        public NavMeshAgent Agent;
        public string AnimatorDirection;
        public string AnimatorSpeed;

        // LIVE PUBLIC VARIABLES
        public Dictionary<Entity, int> ThreatList;
        public List<Entity> AllyList;
        public Entity Target;
        public enum Mood { Idle, Patrol, Attack, Flee, Alert, Chase, Dead }

        // PRIVATE VARIABLES USED/CACHED INTERNALLY
        private GameObject _go;
        private Animator _animator;
        private bool _needToReload;
        private Transform _startTransform;
        private float _distanceToTarget;
        private bool IsInRange;
        private bool _onPatrol;
        private bool _waiting;
        private int _scanClock;
        private static float _editorGizmoSpin;
        private Vector3 _victimLastPos;
        private Rigidbody _rb;
        private Vector3 _myVelocity;
        private Vector3 _myLastPos;
        private Vector3 _myCurPos;

        protected Entity ThisSubject;
        protected bool Fleeing;
        protected bool MoodSwitch;
        protected Mood MyMood;
        protected Mood MyLastMood;
        #endregion

        // Init and Editor stuff
        void Reset()
        {
            IgnoreUpdates = 2;
            SenseFrequency = 2;

            MyProvokeType = ProvokeType.TargetIsInRange;
            ProvokeDelay = 1f;
            HelpAllies = true;

            JukeTime = 2f;
            JukeFrequency = 0f;
            JukeFrequencyRandomness = 1f;

            FieldOfView = 30f;
            SightRange = 12f;
            PursueRange = 10f;
            WanderRange = 3f;
            EngageThreshold = 1f;

            FleeHealthThreshold = 0;

            SightMask = -1;
            ThreatMask = -1;

            MaxDeviation = 15f;
            AlertTime = 3f;

            AnimatorSpeed = "speed";
            AnimatorDirection = "direction";

            MaxAllyCount = 10;
        }
        void OnEnable()
        {
            ThreatList = new Dictionary<Entity, int>();
            AllyList = new List<Entity>();
        }
        void Awake()
        {
            //_rb = GetComponent<Rigidbody>();
            //_rb.angularDrag = 1000f;
            //_rb.drag = 1000f;
            //_rb.mass = 500f;
            //_rb.interpolation = RigidbodyInterpolation.Interpolate;
            //_rb.constraints = (RigidbodyConstraints) 84;

            _startTransform = transform;
            ThreatList = new Dictionary<Entity, int>();
            AllyList = new List<Entity>();
            ThisSubject = GetComponent<Entity>();
            Agent = AgentGetComponent;
        }
        void Start()
        {
            _go = gameObject;
            ViewPoint = GetComponent<CapsuleCollider>();

            _animator = GetComponent<Animator>();

            Fleeing = false;
            MyMood = Mood.Idle;

            AgentConfigure();

            ThisSubject.OnDeath += Die;


            StartCoroutine(StateMachine());
        }
        void OnDrawGizmos()
        {
            _editorGizmoSpin += 0.02f;
            if (_editorGizmoSpin > 360) _editorGizmoSpin = 0;
        }
        void OnDrawGizmosSelected()
        {
            if (!DrawGizmos) return;
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

            // ATTACK range
            // Gizmos.color = Color.red;
            // Gizmos.DrawWireSphere(transform.position, _weapon.Stats.EffectiveRange);

            // WANDER range
            Gizmos.color = Color.grey;
            Gizmos.DrawWireSphere(transform.position, WanderRange);

            // FOV
            Gizmos.color = Color.cyan;
            Vector3 dirR = Quaternion.AngleAxis(FieldOfView, Vector3.up) * transform.forward;
            Gizmos.DrawRay(transform.position, dirR * SightRange);
            Vector3 dirL = Quaternion.AngleAxis(-FieldOfView, Vector3.up) * transform.forward;
            Gizmos.DrawRay(transform.position, dirL * SightRange);

            // Allies
            if (AllyList.Count > 0)
            {
                foreach (var ally in AllyList)
                {
                    Debug.DrawLine(transform.position + Vector3.up, ally.transform.position + Vector3.up, Color.green);
                }
            }

            if (Provoked)
            {
                Debug.DrawLine(transform.position + Vector3.up, Target.transform.position + Vector3.up, Color.red);
            }

            if (ThreatList != null && ThreatList.Count > 0)
            {
                foreach (var threat in ThreatList)
                {
                    Debug.DrawLine(transform.position + Vector3.up, threat.Key.transform.position + Vector3.up, Color.yellow);
                }
            }
#if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
#endif
        }

        // Primary loop and context/condition analysis
        void DoLocomotion()
        {
            // aim either at the target or toward our path.
            if (Provoked && Target != null)
            {
                if (TargetCanBeSeen(Target.gameObject))
                {
                    LeadTarget(Target.gameObject);
                }
                else if (AgentDesiredVelocity != Vector3.zero)
                {
                    LookAt(transform.position + AgentDesiredVelocity);
                }
            }

            // adjust threshold for tiny distance movement here.
            _myVelocity = (transform.position - _myLastPos).magnitude < 0.01f
                ? Vector3.zero
                : (transform.position - _myLastPos).normalized;

            _myLastPos = transform.position;

            float speed = 0;
            float angle = 0;

            if (_myVelocity != Vector3.zero)
            {
                speed = _myVelocity.magnitude;
                angle = Vector3.Angle(transform.forward, _myVelocity);
                angle *= Mathf.Deg2Rad;
            }

            //_animator.SetFloat(AnimatorDirection, angle);
            //_animator.SetFloat(AnimatorSpeed, speed);
        }
        private IEnumerator StateMachine()
        {
            int i = IgnoreUpdates;
            while (true)
            {
                while (ThisSubject.IsDead) { yield return null; }
                DoLocomotion();
                while (i > 0)
                {
                    i--;
                    yield return null;
                }

                i = IgnoreUpdates;

                yield return StartCoroutine(ProcessConditions());

                switch (MyMood)
                {
                    case (Mood.Idle):
                        StartCoroutine(Idle());
                        break;
                    case (Mood.Patrol):
                        StartCoroutine(Patrol());
                        break;
                    case (Mood.Attack):
                        StartCoroutine(Attack());
                        break;
                    case (Mood.Alert):
                        StartCoroutine(Alert());
                        break;
                    case (Mood.Flee):
                        StartCoroutine(Flee());
                        break;
                    case (Mood.Chase):
                        StartCoroutine(Chase());
                        break;
                    case (Mood.Dead):
                        StartCoroutine(Dead());
                        break;
                }
            }
        }
        private IEnumerator ProcessConditions()
        {
            if (ThisSubject.IsDead) yield break;
            if (MyProvokeType == ProvokeType.TargetAttackedMe) Fleeing = (ThisSubject.Stats.Health.Actual <= FleeHealthThreshold);
            if (MyProvokeType == ProvokeType.TargetIsInRange) Fleeing = (ThisSubject.Stats.Health.Actual <= FleeHealthThreshold && Target);
            if (_scanClock >= SenseFrequency)
            {
                ScanForAllSubjects();
                Target = FindThreat().Key;
            }
            _scanClock++;

            // NOTE: Order of Mood processing matters here. Upper evaluations have priority over the ones below.
            // If a Mood condition is met then no other moods are processed.

            #region ### Mood Conditions
            if (ThisSubject.IsDead)
            {
                Provoked = false;
                yield return MyMood = Mood.Dead;
                yield break;
            }


            if (IsInRange)
            {
                MyLastMood = MyMood;
                yield return MyMood = Mood.Attack;
                yield break;
            }

            // TODO since we could have non-dangerous threats, this needs to look at the biggest threat value instead.
            if (MyMood == Mood.Attack && ThreatList.Count == 0)
            {
                // just dropped out of combat and there's no dangerous threats, be Alert
                Provoked = false;
                MyLastMood = MyMood;
                yield return MyMood = Mood.Alert;
                yield break;
            }
            if (_waiting) yield return null;
            if (Fleeing)
            {
                Provoked = true;
                MyLastMood = MyMood;
                yield return MyMood = Mood.Flee;
                yield break;
            }
            if (Provoked || (MyProvokeType == ProvokeType.TargetIsInRange && Target) && IsInRange == false)
            {
                if (MyLastMood != Mood.Chase)
                {
                    StartCoroutine(DelayProvoke());
                }

                Provoked = true;
                MyLastMood = MyMood;
                yield return MyMood = Mood.Chase;
                yield break;
            }
            // provoked moods above, force unprovoked if required (Alert)
            // unprovoked moods below
            Provoked = false;
            if (PatrolPoints.Count > 1)
            {
                MyLastMood = MyMood;
                yield return MyMood = Mood.Patrol;
                yield break;
            }
            if (WanderRange > 0f)
            {
                MyLastMood = MyMood;
                yield return MyMood = Mood.Chase;
                yield break;
            }
            if (!Provoked)
            {
                MyLastMood = MyMood;
                yield return MyMood = Mood.Idle;
            }

            #endregion
        }

        // Possible AI states
        private IEnumerator Idle()
        {
            if (Vector3.Distance(AgentDestination, _startTransform.position) > 0.1)
            {
                MoveTo(_startTransform.position);
            }
            else
            {
                AgentResume();
            }

            yield return null;
        }
        private IEnumerator Patrol()
        {
            /*
            if (_patrolling) yield break;

            SetStoppingDistance(0.05f);
            MoveTo(PatrolPoints[_curPatrol].transform.position);

            _patrolling = true;
            if (LogDebug) Debug.Log(_go.name + " Patrolling to " + PatrolPoints[_curPatrol]);
            while (AgentRemainingDistance >= AgentStoppingDistance) yield return null;
            _patrolling = false;

            _curPatrol++;
            if (_curPatrol > PatrolPoints.Count - 1) _curPatrol = 0;
             */
            yield return null;
        }
        private IEnumerator Chase()
        {
            if (_waiting) yield break;
            if (ThisSubject.IsDead) yield break;
            if (!Target)
            {
                yield break;
            }
            if (Target.IsDead)
            {
                AgentStoppingDistance = AttackRange + EngageThreshold;
                yield break;
            }

            // At this point, I know I *could* fire my weapon.
            // If I'm in range, look at the target, if I'm not, Move to it.

            _distanceToTarget = DistToTarget; // this is caching a variable used by other routines.
            if (TargetCanBeSeen(Target.gameObject) && TargetIsInRange() && TargetIsInFov())
            {
                if (JukeTime > 0f && !_juking) StartCoroutine(Juke());
                IsInRange = true;
                AgentStoppingDistance = AttackRange;
            }
            else // move in
            {
                IsInRange = false;
                AgentStoppingDistance = 0.3f;
                //Agent.destination = Target.transform.position;
                MoveTo(Target.transform.position);
            }
            yield return null;
        }
        private IEnumerator Flee()
        {
            if (ThisSubject.IsDead) yield break;

            AgentStoppingDistance = 0;
            if (DistToTarget >= SightRange)
            {
                yield break;
            }
            if (AgentIsPathStale | AgentRemainingDistance < 1)
            {
                // TODO better algorithm to decide flee destination
                int rng = Random.Range(0, 10);
                if (rng > 5) GetPosNearby(SightRange);
                else GetPosFleeing(SightRange);
                yield break;
            }
            if (AgentRemainingDistance < 1)
            {
                GetPosNearby(15f);
                yield break;
            }
            if (Vector3.Distance(AgentDestination, Target.transform.position) <= 5f)
            {
                GetPosNearby(SightRange);
                yield break;
            }

            yield return null;
        }
        private IEnumerator Alert()
        {

            if (_waiting) yield break;
            _waiting = true;
            //yield return new WaitForSeconds(AlertTime);
            _waiting = false;

            AgentStoppingDistance = 0.05f;
            MoveTo(_startTransform.position);
        }
        private IEnumerator Attack()
        {
            _animator.SetTrigger("Attack");

            IsInRange = false;

            yield break;
        }
        private IEnumerator Dead()
        {
            yield break;
        }

        // Callback responses
        public void Die()                                   // callback when we are dead
        {
            // method called from Subject.
            AgentEnabled(false);
        }
        public void Revive()                                // callback at ressurection
        {
            // inverse the relevant actions of Die() here.
            AgentEnabled(true);
        }

        // List Management and Queries
        void DefineAsThreat(Entity target, int threat)     // add a subject to the threat list
        {
            if (ThreatList.ContainsKey(target))
            {
                if (target.IsDead) RemoveThreat(target);
                else return;
            }

            ThreatList.Add(target, threat);

        }
        void DefineAsAlly(Entity target)                   // add a subject to the ally list
        {
            if (AllyList.Count >= MaxAllyCount)
            {
                return;
            }
            if (!AllyList.Contains(target))
            {
                AllyList.Add(target);
            }
        }
        void RemoveThreat(Entity target)                   // remove a subject from the threat list
        {
            ThreatList.Remove(target);
        }
        void AddThreatValue(Entity target, int threat)     // add threat to a specific subject
        {
            ThreatList[target] += threat;
        }
        void RemoveAlly(Entity target)                     // remove an Ally from the AllyList
        {
            AllyList.Remove(target);
        }

        void ScanForAllSubjects()                           // get all subjects in the Sight Range
        {
            _scanClock = 0;
            Collider[] scanHits = Physics.OverlapSphere(transform.position, SightRange, ThreatMask);

            foreach (Collider thisHit in scanHits)
            {
                if (thisHit.gameObject == gameObject) continue; // is it me?
                Entity otherSubject = thisHit.GetComponent<Entity>(); // TODO this is unfortunately frequent...
                if (!otherSubject) continue; // is it null?
                if (AllyList.Contains(otherSubject) || ThreatList.ContainsKey(otherSubject)) continue; // is it a duplicate?

                // none of that? then sort the new entry as Ally or Threat.
                if (ThisSubject.Stats.TeamID == otherSubject.Stats.TeamID) DefineAsAlly(otherSubject);
                else DefineAsThreat(otherSubject, (MyProvokeType == ProvokeType.TargetIsInRange) ? 1 : 0);
            }

            CleanLists();
        }
        void CleanLists()                                   // remove null entries in the lists
        {
            if (ThreatList.Count > 0)
            {
                List<Entity> removals = (from entry in ThreatList where !entry.Key || entry.Key.IsDead select entry.Key).ToList();
                foreach (Entity trash in removals) RemoveThreat(trash);
            }

            if (AllyList.Count > 0)
            {
                List<Entity> removals = (from entry in AllyList where !entry || entry.IsDead select entry).ToList();
                foreach (Entity trash in removals) RemoveAlly(trash);
            }
        }
        KeyValuePair<Entity, int> FindThreat()             // look in the threat list for something to kill
        {
            // no threats and not helping allies?
            if (!ThreatList.Any() && !HelpAllies) return new KeyValuePair<Entity, int>();

            // grab the local threatlist
            Dictionary<Entity, int> allThreats = ThreatList.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // grab the ally's threats
            if (HelpAllies)
            {
                Dictionary<Entity, int> myFriendsThreats = new Dictionary<Entity, int>();

                // look at each ally
                if (AllyList.Count > 0)
                {
                    foreach (var ally in AllyList)
                    {
                        Intellect friend = ally.GetComponent<Intellect>();

                        if (friend)
                        {
                            // look at each threat in that ally
                            foreach (var threat in friend.ThreatList)
                            {
                                // add that threat to this local list
                                if (!myFriendsThreats.ContainsKey(threat.Key))
                                {
                                    myFriendsThreats.Add(threat.Key, threat.Value);
                                }
                            }
                        }
                    }
                }
                // put any threats from allies into the full list of threats
                if (myFriendsThreats.Any())
                {
                    foreach (KeyValuePair<Entity, int> kvp in myFriendsThreats.Where(kvp => !allThreats.ContainsKey(kvp.Key)))
                    {
                        allThreats.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            // do i want the closest or the biggest threat?
            KeyValuePair<Entity, int> final = (MyThreatPriority == ThreatPriority.Nearest
                ? GetNearestThreat(allThreats, transform)
                : GetHighestThreat(allThreats));

            if (final.Value > 0 || (HelpAllies && AllyList.Count > 0 && AllyList.Where(ally => ally.GetComponent<Intellect>() != null).Any(ally => ally.GetComponent<Intellect>().Provoked))) Provoked = true;
            return final;
        }
        static KeyValuePair<Entity, int> GetNearestThreat(Dictionary<Entity, int> listOfThreats, Transform fromThis)     // find the nearest threat
        {
            if (listOfThreats.Count == 0) return new KeyValuePair<Entity, int>();
            float[] curNearestDistance = { 1000f };

            KeyValuePair<Entity, int>[] nearest = { listOfThreats.First() };
            foreach (KeyValuePair<Entity, int> kvp in listOfThreats
                .Where(kvp => Vector3.Distance(kvp.Key.transform.position, fromThis.position) < curNearestDistance[0]))
            {
                curNearestDistance[0] = Vector3.Distance(kvp.Key.transform.position, fromThis.position);
                nearest[0] = kvp;
            }
            return nearest[0];
        }
        static KeyValuePair<Entity, int> GetHighestThreat(Dictionary<Entity, int> listOfThreats)                         // find the highest threat
        {
            if (listOfThreats.Count == 0) return new KeyValuePair<Entity, int>();

            KeyValuePair<Entity, int>[] biggestThreat = { listOfThreats.First() };
            foreach (KeyValuePair<Entity, int> threat in listOfThreats.Where
                (threat => threat.Value > biggestThreat[0].Value))
            {
                biggestThreat[0] = threat;
            }

            return biggestThreat[0];
        }
        public int GetTargetThreatValue()
        {
            return Target != null ? ThreatList[Target] : 0;
        }

        void MoveTo(Vector3 position)                       // pathfind to a position
        {
            if (ThisSubject.IsDead) return;
            AgentDestination = position;
            AgentResume();
        }
        public void LookAt(Vector3 position)                // look at a specific position
        {
            if (Target.IsDead) return;

            Vector3 dir = position - _go.transform.position; dir.y = 0f;
            Quaternion fin = Quaternion.LookRotation(dir);
            _go.transform.rotation = Quaternion.Slerp(_go.transform.rotation, fin, Time.deltaTime * ThisSubject.Stats.TurnSpeed);
        }
        public void LeadTarget(GameObject victim)           // lead the target, compensating for their trajectory and projectile speed
        {
            // TODO find a way to compensate for the horizontal offset of the weapon without screwing with the character orientation

            // Get the velocity of the Subject. We need to know *direction* and *speed*.
            Vector3 victimVelocty = (victim.transform.position - _victimLastPos) * Time.deltaTime;
            Vector3 intercept = victim.transform.position + (victimVelocty * (_distanceToTarget));
            //+ victim.transform.TransformVector(new Vector3((_weapon.Stats.MountPivot == MountPivot.RightShoulder ? 1f : _weapon.Stats.PositionOffset.x),0,0)));

            _victimLastPos = victim.transform.position; // Plug in the last known position (first calc is always wrong)
            LookAt(intercept);
        }
        private IEnumerator Juke()                          // Execute a juke maneuver
        {
            // juke has to do something every frame until it reaches its point or cant reach it
            if (_juking && Agent.enabled) yield break;
            _juking = true;

            // tell the agent to stay still
            AgentStoppingDistance = AttackRange;

            _jukeHeading = GetJukeHeading();

            // setup the time before the next Juke
            float wait = Random.Range(
                    JukeFrequency - JukeFrequencyRandomness,
                    JukeFrequency + JukeFrequencyRandomness);

            bool yieldToJukeTime = true;
            float timer = 0;
            while (yieldToJukeTime && Agent.enabled)
            {
                //_rb.MovePosition(transform.position + _jukeHeading * .01f);
                Agent.SetDestination(transform.position - _jukeHeading * .1f);
                timer += Time.deltaTime;
                if (timer >= JukeTime)
                {
                    yieldToJukeTime = false;
                }
                yield return null;
            }

            //yield return new WaitForSeconds(wait);
            _juking = false;
        }
        private Vector3 GetJukeHeading()
        {
            // setup the distance to juke per frame
            bool r = (Random.value < 0.5);
            _jukeHeading = (r && !Physics.Raycast(transform.position + Vector3.up, Vector3.right, 0.2f, SightMask)
                ? transform.TransformDirection(Vector3.right * ThisSubject.Stats.MoveSpeed)
                : transform.TransformDirection(Vector3.left * ThisSubject.Stats.MoveSpeed));
            return _jukeHeading;
        }

        bool TargetCanBeSeen(GameObject interest)           // raycast to the Target and check for a hit
        {
            bool inSight = false;
            Vector3 direction = (interest.transform.position - transform.position).normalized;
            Vector3 origin = transform.position + new Vector3(0, 0f, 0) + (direction *0.5f);
            // TODO NOTE: This assumes a raycast done from 1.5 units on Y from the feet, on the outside of the Subject collider.
            // May have problems if using more than one collider.

            RaycastHit hit;
            if (Physics.Raycast(origin, direction.normalized, out hit, SightRange, SightMask))
            {
                if (hit.collider.gameObject == interest) inSight = true;
            }
            return inSight;
        }
        bool TargetIsInFov()                                // check the FOV area for the target
        {
            Vector3 direction = Target.gameObject.transform.position - transform.position;
            float angle = Vector3.Angle(direction, transform.forward);
            return angle < FieldOfView * 0.5f;
        }
        bool TargetIsInRange()                              // check if distance to target is less than vision range.
        {
            return _distanceToTarget < AttackRange;
        }

        // NavMesh queries
        private void GetPosNearby(float area)               // find a random nearby position
        {
            Vector3 waypoint = transform.position + Random.insideUnitSphere * area;
            waypoint.y = 0f;

            NavMeshHit hit;
            NavMesh.SamplePosition(waypoint, out hit, 10.0f, NavMesh.AllAreas);
            Vector3 dest = hit.position;
            MoveTo(dest);
        }
        private void GetPosFleeing(float area)              // find a random nearby position relative to the Target
        {
            AgentStoppingDistance = 0;
            Vector3 waypoint = Vector3.Scale
                (transform.position,
                (Target.transform.position - transform.position).normalized * area)
                + Random.insideUnitSphere * 2;
            waypoint.y = 0f;

            NavMeshHit hit;
            NavMesh.SamplePosition(waypoint, out hit, area, NavMesh.AllAreas);
            Vector3 dest = hit.position;
            MoveTo(dest);
        }
        public float DistToTarget                           // find the distance to the target
        {
            get
            {
                return Target.IsDead ? 0f : Vector3.Distance(Target.transform.position, transform.position);
            }
        }

        // Miscellaneous
        public string GetTargetName()
        {
            return Target != null ? Target.gameObject.name : "";
        }
        private IEnumerator DelayProvoke()
        {
            if (_waiting) yield break;
            _waiting = true;
            //yield return new WaitForSeconds(ProvokeDelay);
            _waiting = false;
        }

        // NavMeshAgent accessors 
        public NavMeshAgent AgentGetComponent
        {
            get
            {
                return GetComponent<NavMeshAgent>();
            }
        }
        public void AgentConfigure()
        {
            AgentStoppingDistance = 0;
            Agent.angularSpeed = 10f * ThisSubject.Stats.TurnSpeed;
            Agent.speed = ThisSubject.Stats.MoveSpeed;
            Agent.acceleration = 100f;
            Agent.autoBraking = false;
        }
        public void AgentResume()
        {
            Agent.Resume();
        }
        public void AgentStop()
        {
            Agent.Stop();
        }
        public void AgentEnabled(bool status)
        {
            Agent.enabled = status;
        }
        public float AgentRemainingDistance
        {
            get { return Agent.remainingDistance; }
        }
        public float AgentStoppingDistance
        {
            get { return Agent.stoppingDistance; }
            set { Agent.stoppingDistance = value; }
        }
        public Vector3 AgentDesiredVelocity
        {
            get { return Agent.desiredVelocity; }
        }
        public Vector3 AgentDestination
        {
            get { return Agent.destination; }
            set { Agent.SetDestination(value); }
        }
        public bool AgentIsPathStale
        {
            get { return Agent.isPathStale; }
        }
    }

}