using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour {

    public Text m_phaseText;
    public Text m_phaseCountdown;

	// Use this for initialization
	void Start () {
        GameController.Instance.onPhaseProgress += OnPhaseProgressImpl;
    }

    private void OnDestroy()
    {
        if(GameController.Instance)
        {
            GameController.Instance.onPhaseProgress -= OnPhaseProgressImpl;
        }
    }

    private void OnPhaseProgressImpl(GameState currentMode, float timeLeft)
    {
        if(currentMode == GameState.Day)
        {
            m_phaseText.color = Color.black;
            m_phaseCountdown.color = Color.black;
        }
        else
        {
            m_phaseText.color = Color.white;
            m_phaseCountdown.color = Color.white;
        }

        m_phaseText.text = currentMode.ToString();
        m_phaseCountdown.text = timeLeft.ToString();
    }

    // Update is called once per frame
    void Update () {
		
	}
}
