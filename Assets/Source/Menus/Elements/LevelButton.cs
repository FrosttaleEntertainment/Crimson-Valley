using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LevelButton : MonoBehaviour {

    public string Scene;

    private Button mButton;

    private void Awake()
    {
        mButton = GetComponent<Button>();
        mButton.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        SceneController.Instance.StartLevel(Scene);
    }
}
