using Invector.vCharacterController.AI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class EnemyController : NetworkBehaviour
{
    public GameObject[] enemyPrefabs;
    public int waveCountPerPlayer;
    public int wavesPerNight;

    private float m_timeBetweenEmitions;
    private int m_currentEmition;
    private List<GameObject> m_enemies;

    public override void OnStartServer()
    {
        m_currentEmition = 0;
        m_enemies = new List<GameObject>();
        GameController.Instance.onPhaseChanged += OnPhaseChangedImpl;
        Invoke("EmitWave", 10);
    }

    private void OnDestroy()
    {
        if(isServer)
        {
            GameController.Instance.onPhaseChanged -= OnPhaseChangedImpl;
        }
    }

    public void EmitWave()
    {
        if (!isServer)
        {
            Debug.Assert(false, "This should not happens");
            return;
        }

        var playersCount = NetworkServer.connections.Count;
        var totalEnemies = waveCountPerPlayer * playersCount;
        
        var spawnPossitions = FindObjectsOfType<EnemySpawnPosition>();

        for (int i = 0; i < totalEnemies; i++)
        {
            var randomSpawnPosition = Random.Range(0, spawnPossitions.Length);
            var spawnPosition = spawnPossitions[randomSpawnPosition];

            var spawnRotation = Quaternion.Euler(
                0.0f,
                Random.Range(0, 180),
                0.0f);

            var type = Random.Range(0, enemyPrefabs.Length);
            var enemy = (GameObject)Instantiate(enemyPrefabs[type], spawnPosition.transform.position, spawnRotation);
            NetworkServer.Spawn(enemy);

            m_enemies.Add(enemy);
        }

        m_currentEmition++;

        if(m_currentEmition < wavesPerNight)
        {
            //schedule for another emition
            Invoke("EmitWave", m_timeBetweenEmitions);
        }
    }

    private void OnPhaseChangedImpl(GameState currentMode, float timeLeft)
    {
        if(!isServer)
        {
            Debug.Assert(false, "This should not happens");
            return;
        }

        if (currentMode == GameState.Night)
        {
            m_enemies = new List<GameObject>();

            m_timeBetweenEmitions = timeLeft / (wavesPerNight);
            EmitWave();
        }
        else
        {
            // just in case
            CancelInvoke("EmitWave");

            // TODO: kill all the enemies with burning animation(or just dying)
            foreach (var enemy in m_enemies)
            {
                var mummy = enemy.GetComponent<v_AIController>();
                if(mummy)
                {
                    mummy.ChangeHealth(0);
                }
            }
        }
    }
}