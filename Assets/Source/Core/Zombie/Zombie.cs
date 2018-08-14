using Base;
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

    public void BurnDie()
    {
        Die();

        if (isServer)
        {
            RpcStartBurnEffect(); // not sure if this is the right place for this
            StartCoroutine(StaticUtil.DestroyInternal(gameObject, 4f));
        }
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

    [ClientRpc]
    private void RpcStartBurnEffect()
    {
        // burn effect here
    }
}

