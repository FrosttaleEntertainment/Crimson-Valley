using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    public Image Background;
    public Text DoneText;

    private void Awake()
    {
        this.gameObject.SetActive(false);
    }
	
    public bool SetLoadingScreenBackground(string level)
    {
        if(Background == null)
        {
            return false;
        }

        //TODO Set desired image from config

        return true;
    }

    public bool Show()
    {
        if (Background == null)
        {
            return false;
        }

        this.gameObject.SetActive(true);

        if(GameController.Instance.IsMultyPlayer())
        {
            // Loading screen will wait for all players, then will manually be unloaded
            DontDestroyOnLoad(this.gameObject);
        }

        return true;
    }

    public void Done()
    {
        if(DoneText)
        {
            DoneText.gameObject.SetActive(true);
        }
    }
}