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
        for (int i = 0; i < rootObjects.Length; i++)
        {
            LoadingScreen = rootObjects[i].GetComponent<LoadingScreen>();

            if(LoadingScreen)
            {
                break;
            }
        }

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
