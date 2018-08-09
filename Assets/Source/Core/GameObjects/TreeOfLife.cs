using Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Entity))]
public class TreeOfLife : NetworkBehaviour
{
    private Entity m_entity;

    [Server]
    private void Awake()
    {
        m_entity = GetComponent<Entity>();

        if(m_entity)
        {
            m_entity.OnDeath += Death;
        }
    }

    [Server]

    private void OnDestroy()
    {
        if (m_entity)
        {
            m_entity.OnDeath -= Death;
        }
    }


    [Server]
    private void Death()
    {

    }
}

