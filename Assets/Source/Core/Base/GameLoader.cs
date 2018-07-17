using UnityEngine;
using System.Collections;
using System;

public class GameLoader : MonoBehaviour
{
    public GameObject m_SinglePlayerNetworkController;

    void Awake()
    {
        //GameController
        if (GameController.Instance != null)
        {
            var gameCtrl = GameController.Instance;
            Debug.Assert(m_SinglePlayerNetworkController != null, "Missing player prefab on the game loader");

            gameCtrl.SinglePlayerNetworkController = m_SinglePlayerNetworkController;
        }

        //SceneController
        if (SceneController.Instance != null)
        {
            //TODO init the scene controller
        }

        //MenuController
        if (MenuController.Instance != null)
        {
            //TODO init the menu controller
        }

        //ChatController
        if (ChatController.Instance != null)
        {
            //TODO init the chat controller
        }

        Destroy(gameObject);
    }
}