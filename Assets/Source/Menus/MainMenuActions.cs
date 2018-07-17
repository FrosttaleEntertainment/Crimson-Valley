using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuActions : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void StartSingleplayer()
    {
        GameController.Instance.SetSinglePlayer();

        SceneController.Instance.LoadLevelSelectionMenu();

    }

    public void StartMultyplayer()
    {
        GameController.Instance.SetMultyPlayer();

        SceneController.Instance.LoadLevelSelectionMenu();
    }
}
