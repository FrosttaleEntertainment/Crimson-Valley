using Base;
using Invector.vCharacterController;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TreeOfLife : vCharacter
{
    [Server]
    private void Start()
    {

    }

    [Server]

    private void OnDestroy()
    {

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

