using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    public void OpenCustomerCare()
    {
        Application.OpenURL("https://www.google.bg/search?biw=1920&bih=974&tbm=isch&sa=1&ei=GW58W5yGF8msrgSPhIbQDA&q=%D0%A1%D0%A2%D0%95%D0%A4%D0%90%D0%9D+%D0%A1%D0%A2%D0%90%D0%99%D0%9A%D0%9E%D0%92+c%2B%2B&oq=%D0%A1%D0%A2%D0%95%D0%A4%D0%90%D0%9D+%D0%A1%D0%A2%D0%90%D0%99%D0%9A%D0%9E%D0%92+c%2B%2B&gs_l=img.3...21043.21895.0.22187.4.4.0.0.0.0.104.331.3j1.4.0....0...1c.1.64.img..0.1.102...0i30k1.0.Q5Otz_ATh44#imgrc=DeprZ30zaLJyDM:");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
