using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Net;
using System.Net.Sockets;

namespace Prototype.NetworkLobby
{
    //List of players in the lobby
    public class LobbyPlayerList : MonoBehaviour
    {
        public static LobbyPlayerList _instance = null;

        public RectTransform playerListContentTransform;
        public GameObject warningDirectPlayServer;
        public Transform addButtonRow;

        protected VerticalLayoutGroup _layout;
        protected List<LobbyPlayer> _players = new List<LobbyPlayer>();

        public void OnEnable()
        {
            _instance = this;
            _layout = playerListContentTransform.GetComponent<VerticalLayoutGroup>();
        }

        public void DisplayDirectServerWarning(bool enabled)
        {
            if(warningDirectPlayServer != null)
            {
                var ipAdress = LocalIPAddress();
                warningDirectPlayServer.GetComponentInChildren<Text>().text = string.Format(warningDirectPlayServer.GetComponentInChildren<Text>().text, ipAdress);
                warningDirectPlayServer.SetActive(enabled);
            }
        }

        public string LocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        void Update()
        {
            //this dirty the layout to force it to recompute evryframe (a sync problem between client/server
            //sometime to child being assigned before layout was enabled/init, leading to broken layouting)
            
            if(_layout)
                _layout.childAlignment = Time.frameCount%2 == 0 ? TextAnchor.UpperCenter : TextAnchor.UpperLeft;
        }

        public void AddPlayer(LobbyPlayer player)
        {
            if (_players.Contains(player))
                return;

            _players.Add(player);

            player.transform.SetParent(playerListContentTransform, false);
            addButtonRow.transform.SetAsLastSibling();

            PlayerListModified();
        }

        public void RemovePlayer(LobbyPlayer player)
        {
            _players.Remove(player);
            PlayerListModified();
        }

        public void PlayerListModified()
        {
            int i = 0;
            foreach (LobbyPlayer p in _players)
            {
                p.OnPlayerListChanged(i);
                ++i;
            }
        }
    }
}
