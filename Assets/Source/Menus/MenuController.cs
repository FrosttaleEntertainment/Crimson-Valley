using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : Singleton<MenuController>
{
    public LoadingScreen LoadingScreen { get; set; }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnLoadingScreenLoaded(GameObject[] rootObjects)
    {
        if (rootObjects.Length != 1)
        {
            Debug.Assert(false, "More than one root object (canvas) in loading scene!");
            return;
        }

        LoadingScreen = rootObjects[0].GetComponent<LoadingScreen>();

        Debug.Assert(LoadingScreen, "LoadingScreen not found on this root object");
    }

    public void PrepareLoading(bool autoShow = false)
    {
        if (LoadingScreen)
        {
            if(autoShow)
            {
                ShowLoading();
            }
        }
    }

    public void ShowLoading()
    {
        if (LoadingScreen)
        {
            LoadingScreen.Show();
        }
    }

    public void HideLoading()
    {
        Debug.Assert(GameController.Instance.IsMultyPlayer(), "This should only be used for multiplayer mode");

        if (LoadingScreen)
        {
            Destroy(LoadingScreen.gameObject);
            LoadingScreen = null;
        }
    }

    public void OnLocalLoaded()
    {
        if (LoadingScreen)
        {
            LoadingScreen.Done();
        }
    }
}
