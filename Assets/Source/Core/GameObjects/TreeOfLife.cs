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
    private void Start()
    {
        m_entity = GetComponent<Entity>();

        if (m_entity)
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
        CmdSetPlayerLookAtTree();
    }

    [Command]
    private void CmdSetPlayerLookAtTree()
    {
        var camera = Camera.main;
        var camControl = camera.GetComponent<CameraControl>();
        if(camControl)
        {
            camControl.Target = this.gameObject;
        }
    }
}

