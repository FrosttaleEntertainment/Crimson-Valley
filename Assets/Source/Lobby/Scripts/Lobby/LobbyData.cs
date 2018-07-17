using Prototype.NetworkLobby;
using UnityEngine;
using UnityEngine.Networking;

public struct LobbyData
{
    public LobbyData(LobbyPlayer copyFrom)
    {
        Name = copyFrom.name;
        Color = copyFrom.playerColor;
    }
    public string Name;
    public Color Color;
}