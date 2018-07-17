using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Zombie : Base.Entity
{
    private RagdollController m_ragdollControl;
    private CapsuleCollider m_capsuleCollider;

    private void Awake()
    {
        m_ragdollControl = GetComponent<RagdollController>();
        m_capsuleCollider = GetComponent<CapsuleCollider>();
        m_animator = GetComponent<Animator>();
    }

    protected override void Die()
    {
        base.Die();

        RpcActivateDeath();
    }

    [ClientRpc]
    private void RpcActivateDeath()
    {
        m_capsuleCollider.enabled = false;

        m_ragdollControl.Activate();
    }
}

