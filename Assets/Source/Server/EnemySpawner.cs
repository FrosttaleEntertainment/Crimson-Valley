using UnityEngine;
using UnityEngine.Networking;

public class EnemySpawner : NetworkBehaviour
{
    public GameObject[] enemyPrefabs;
    public int numberOfEnemies;

    public override void OnStartServer()
    {
        //TODO add specific spawn possitions for the enemies

        for (int i = 0; i < numberOfEnemies; i++)
        {
            var spawnPosition = new Vector3(
                Random.Range(this.transform.position.x - 30 , this.transform.position.x + 30),
                this.transform.position.y,
                Random.Range(this.transform.position.z - 30, this.transform.position.z + 30));

            var spawnRotation = Quaternion.Euler(
                0.0f,
                Random.Range(0, 180),
                0.0f);

            var type = Random.Range(0, enemyPrefabs.Length);
            var enemy = (GameObject)Instantiate(enemyPrefabs[type], spawnPosition, spawnRotation);
            NetworkServer.Spawn(enemy);
        }
    }
}