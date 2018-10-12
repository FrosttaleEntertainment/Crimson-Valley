using Base;
using Invector.vCharacterController;
using Prototype.NetworkLobby;
using UnityEngine;
using UnityEngine.UI;

public struct ChatMessage
{
    public string Message;
    public LobbyData Sender;
}

public class ChatController : Singleton<ChatController> {

    public bool IsInitialized { get; set; }

    private vThirdPersonController m_controller;
    private ChatManager m_manager;

    private ChatManager ChatManager
    {
        get
        {
            if(m_manager == null)
            {
                m_manager = FindObjectOfType<ChatManager>();
            }

            return m_manager;
        }
        set
        {
            m_manager = value;
        }
    }


    // Use this for initialization
    void Start () {
        IsInitialized = false;
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Init(vThirdPersonController ctrl)
    {
        m_controller = ctrl;

        if (m_controller != null)
        {
            IsInitialized = true;
        }
    }
    
    public bool ValidateChatMessage(ChatMessage msg)
    {
        if(!ValidateText(msg.Message))
        {
            return false;
        }

        if(!ValidateSender(msg.Sender))
        {
            return false;
        }

        return true;
    }

    public void OnMessageReceived(ChatMessage msg)
    {
        if(ValidateChatMessage(msg))
        {
            if(ChatManager)
            {
                ChatManager.ReceiveChatMessage(ChatManager.ChatTabType.CHAT_TAB_TYPE_TEAM, msg.Message, msg.Sender);
            }
        }
        else
        {
            Debug.Assert(false, "Invalid message delivered from the server. Investigate");
        }
    }

    public void ClientSendMessage(string textMsg)
    {
        if(m_controller != null)
        {
            //TODO Validate text before sending
            var msg = new ChatMessage();
            msg.Sender = m_controller.LobbyData;
            msg.Message = textMsg;

            LobbyManager.s_Singleton.SendChatMsg(msg);
        }
        else
        {
            Debug.Assert(false, "Missing lobby player");
        }
    }

    private bool ValidateText(string text)
    {
        return true;
    }

    private bool ValidateSender(LobbyData sender)
    {
        return true;
    }
}
