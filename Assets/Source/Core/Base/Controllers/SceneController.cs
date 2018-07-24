using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class SceneController : Singleton<SceneController>
{
    public const string LEVEL_LOADING_SCENE_ID = "LevelLoadinScreen";
    public const string LOBBY_SCENE_ID = "NetworkLobby";
    public const string LEVEL_SELECTION_SCENE_ID = "LevelSelectionMenu";
    public const string CHAT_LAYOUT_SCENE_ID = "ChatLayout";
    public const string HUD_SCENE_ID = "HudOverlay";

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        Debug.Log("OnDisable");
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public bool StartLevel(string level)
    {
        GameController.Instance.Scene = level;

        bool isSinglePlayer = GameController.Instance.IsSinglePlayer();

        if (isSinglePlayer)
        {
            MenuController.Instance.PrepareLoading(level, true);

            LoadScene(level, LoadSceneMode.Single, true);
        }
        else
        {
            return LoadLobby();
        }

        return true;
    }

    public bool LoadLobby()
    {
        LoadScene(LOBBY_SCENE_ID, LoadSceneMode.Single, false);
        return true;
    }

    public bool StartGame()
    {
        LoadScene(1, LoadSceneMode.Single, false); //sync
        return true;
    }

    public bool LoadLevelSelectionMenu()
    {
        LoadScene(LEVEL_SELECTION_SCENE_ID, LoadSceneMode.Single, true); //async
        return true;
    }

    private bool AddLoadingScene()
    {
        LoadScene(LEVEL_LOADING_SCENE_ID, LoadSceneMode.Additive, true); //async
        return true;
    }

    public void OnLevelLoaded()
    {
        AddHudLayout();

        if (GameController.Instance.IsSinglePlayer())
        {
            //Spawn the network comp
            var networkController = GameController.Instance.SinglePlayerNetworkController;
            var obj = Instantiate(networkController);
            var manager = obj.GetComponent<NetworkManager>();

            if (manager != null)
            {
                manager.StartHost();

                GameController.Instance.StartPlaying(true);
            }
        }
        else
        {
            AddChatLayout();
        }
    }

    public UnityEngine.AsyncOperation AddHudLayout()
    {
        return LoadScene(HUD_SCENE_ID, LoadSceneMode.Additive, true);
    }

    public Scene GetCurrentHudScene()
    {
        return SceneManager.GetSceneByName(HUD_SCENE_ID);
    }

    public Scene GetCurrentScene()
    {
        return SceneManager.GetActiveScene();
    }

    private bool AddChatLayout()
    {
        LoadScene(CHAT_LAYOUT_SCENE_ID, LoadSceneMode.Additive, true); //async
        return true;
    }

    private UnityEngine.AsyncOperation LoadScene(string id, LoadSceneMode mode = LoadSceneMode.Single, bool async = false)
    {
        if (!async)
        {
            SceneManager.LoadScene(id, mode);
            return null;
        }

        return SceneManager.LoadSceneAsync(id, mode);
    }

    private UnityEngine.AsyncOperation LoadScene(int index, LoadSceneMode mode = LoadSceneMode.Single, bool async = false)
    {
        if (!async)
        {
            SceneManager.LoadScene(index, mode);
            return null;
        }

        return SceneManager.LoadSceneAsync(index, mode);
    }

    private UnityEngine.AsyncOperation UnloadScene(string id)
    {
        return SceneManager.UnloadSceneAsync(id);
    }

    /////////////////
    /// CALLBACKS ///
    /////////////////  

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.IsValid() == false)
        {
            Debug.Assert(false, "OnSceneLoaded Invalid scene");
            return;
        }

        if(IsSceneLevel(scene))
        {
            OnLevelLoaded();

            return;
        }

        if(scene.name == LEVEL_SELECTION_SCENE_ID)
        {
            if(GameController.Instance.IsSinglePlayer())
            {
                AddLoadingScene();
            }
        }
        else if(scene.name == LOBBY_SCENE_ID)
        {
            AddLoadingScene();
        }
        else if(scene.name == LEVEL_LOADING_SCENE_ID)
        {
            MenuController.Instance.OnLoadingScreenLoaded(scene.GetRootGameObjects());
        }
    }

    private bool IsSceneLevel(Scene scene)
    {
        switch (scene.name)
        {
            case "Desert":
            case "Forest":
            case "Test_Scene_01":
                return true;
#if UNITY_EDITOR
            case "TestZombieKillMultyplayer":
                return true;
#endif
            default:
                return false;
        }
    }
}
