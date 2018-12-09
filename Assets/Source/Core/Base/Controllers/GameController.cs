using Prototype.NetworkLobby;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public enum GameMode
{
    None,
    SinglePlayer,
    MultyPlayer
}

public enum GameState
{
    Day,
    Night,
    Count
}

public class GamePhaseChangedMsg : MessageBase
{
    public GameState m_phase;
    public float m_duration;
}

public class GameController : Singleton<GameController>
{
    public string Scene { get; set; }
    public GameObject SinglePlayerNetworkController { get; set; }

    public event UnityAction<GameState, float> onPhaseProgress;
    public event UnityAction<GameState, float> onPhaseChanged;

    private GameMode GameMode { get; set; }

    private bool m_isPlaying;
    private bool m_isServer;

    private float m_phaseStartTime;
    private float m_phaseEndTime;
    private GameState m_currentPhase;

    // Use this for initialization
    void Start()
    {
        ResetState();
    }

    // Update is called once per frame
    void Update()
    {
        //if (m_isPlaying)
        //{
        //    if (m_isServer)
        //    {
        //        if (Time.time >= m_phaseEndTime)
        //        {
        //            var nextDuration = GetNextStateDuration();
        //
        //            if(IsMultyPlayer())
        //            {
        //                //notify all clients
        //                var msg = new GamePhaseChangedMsg();
        //                msg.m_duration = nextDuration;
        //                msg.m_phase = GetNextState();
        //
        //                LobbyManager.s_Singleton.SendGamePhaseChangedMsg(msg);
        //            }
        //            else
        //            {
        //                ChangeState(GetNextState(), nextDuration);
        //            }
        //        }
        //    }           
        //
        //    // update hud timers
        //    if(onPhaseProgress != null)
        //    {
        //        onPhaseProgress.Invoke(m_currentPhase, Time.time - m_phaseStartTime);
        //    }
        //}
    }

    public void StartPlaying(bool isServer = false)
    {
        if(!m_isPlaying)
        {
            m_isPlaying = true;
            m_isServer = isServer;
        }
    }

    public void ChangeState(GameState newState, float duration)
    {
        m_currentPhase = newState;
        m_phaseStartTime = Time.time;
        m_phaseEndTime = m_phaseStartTime + duration;

        switch (m_currentPhase)
        {
            case GameState.Day:
                {                    
                }
                break;
            case GameState.Night:
                {
                }
                break;
        }

        if(onPhaseChanged != null)
        {
            onPhaseChanged.Invoke(m_currentPhase, duration);
        }
    }

    public void StopPlaying()
    {
        m_isPlaying = false;
    }

    public bool Reload()
    {
        if (GameMode == GameMode.None)
        {
            Debug.Assert(false, "Invalid gamemod");
            return false;
        }

        return true;
    }

    public void ResetState()
    {
        GameMode = GameMode.None;
        Scene = string.Empty;

        m_currentPhase = GameState.Count;
    }

    public bool IsMultyPlayer()
    {
        return GameMode == GameMode.MultyPlayer;
    }

    public bool IsSinglePlayer()
    {
        return GameMode == GameMode.SinglePlayer;
    }

    public void SetMultyPlayer()
    {
        GameMode = GameMode.MultyPlayer;
        Reload();
    }

    public void SetSinglePlayer()
    {
        GameMode = GameMode.SinglePlayer;
        Reload();
    }

    private GameState GetNextState()
    {
        if (m_currentPhase + 1 >= GameState.Count)
        {
            return 0;
        }

        return m_currentPhase + 1;
    }

    private float GetNextStateDuration()
    {
        if (m_currentPhase + 1 >= GameState.Count)
        {
            return GetStateDuration(0);
        }

        return GetStateDuration(m_currentPhase + 1);
    }

    private float GetStateDuration(GameState state)
    {
        switch (state)
        {
            case GameState.Day:
                return 40;
            case GameState.Night:
                return 10;
        }

        Debug.Assert(false, "Why is this reached?");
        return 0;
    }
}