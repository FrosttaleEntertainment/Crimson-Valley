using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatMsgField : MonoBehaviour {
    public Text m_senderName;
    public Text m_message;

	public void Init(LobbyData data, string message)
    {
        m_senderName.text = data.Name;
        m_senderName.color = data.Color;

        m_message.text = message;
    }
}
