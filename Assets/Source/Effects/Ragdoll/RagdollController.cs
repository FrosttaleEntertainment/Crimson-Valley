using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RagdollController : MonoBehaviour {

    public Rigidbody[] Rigidbodies;
    private Collider m_collider;
    private Animator m_animator;

    public void ActivateRagdoll()
    {
        Enable(true);
    }

    private void Awake()
    {
        m_animator = GetComponent<Animator>();
        m_collider = GetComponent<Collider>();

        if(!m_animator)
        {
            Debug.LogError("NO ANIAMTOR");
        }

        ModifyRigidbodiesState(true);
    }


    private void Enable(bool gravityState = false) {
        ModifyRigidbodiesState(false, gravityState);

        m_animator.enabled = false;

        if(m_collider)
        {
            m_collider.enabled = false;
        }
    }

    /// <summary>
    /// True disables them, while false enables them
    /// </summary>
    /// <param name="state"></param>
    private void ModifyRigidbodiesState(bool state, bool gravityState = false)
    {
        for (int i = 0; i < Rigidbodies.Length; i++)
        {
            Rigidbodies[i].isKinematic = state;
            Rigidbodies[i].detectCollisions = !state;
            Rigidbodies[i].useGravity = gravityState;

            Rigidbodies[i].GetComponent<Collider>().enabled = !state;
        }
    }
}
