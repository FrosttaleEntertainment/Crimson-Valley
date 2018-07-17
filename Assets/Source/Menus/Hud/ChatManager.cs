using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour {

    public InputField m_chatInput;
    public GameObject m_content;

    public GameObject m_msgFieldPrefab;

	// Use this for initialization
	void Start () {
		if(m_chatInput)
        {
            m_chatInput.gameObject.SetActive(false);
        }
	}

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if(!m_chatInput.gameObject.activeSelf)
            {
                m_chatInput.gameObject.SetActive(true);
                m_chatInput.ActivateInputField();
            }
            else
            {
                var msg = m_chatInput.text;

                if (!string.IsNullOrEmpty(msg))
                {
                    ChatController.Instance.ClientSendMessage(msg);
                }

                m_chatInput.gameObject.SetActive(false);
            }
        }
    }

    public void DisplayMessage(ChatMessage msg)
    {
        var go = Instantiate(m_msgFieldPrefab, m_content.transform);
        go.GetComponent<ChatMsgField>().Init(msg.Sender, msg.Message);
    }
}
