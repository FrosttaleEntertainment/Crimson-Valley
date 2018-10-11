using UnityEngine;
using Prototype.NetworkLobby;
using System.Collections;
using UnityEngine.Networking;
using Base;

public class NetworkLobbyHook : LobbyHook 
{
    public override void OnLobbyServerSceneLoadedForPlayer(NetworkManager manager, GameObject lobbyPlayer, GameObject gamePlayer)
    {
        LobbyPlayer lobby = lobbyPlayer.GetComponent<LobbyPlayer>();

        LobbyData data;
        data.Name = lobby.playerName;
        data.Color = lobby.playerColor;

        //Entity entity = gamePlayer.GetComponent<Entity>();
        //entity.LobbyData = data;
    }
}