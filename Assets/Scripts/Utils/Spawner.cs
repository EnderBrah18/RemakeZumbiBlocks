using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject enemyToSpawn;
    public float spawnRadius = 2f;


    public void SpawnSingleEnemy()
    {
        Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;
        // O 'Y' deve ser 0 (ou a altura do chão) e o 'Y' do círculo vira o 'Z' do mundo
        Vector3 spawnPosition = new Vector3(
            transform.position.x + randomPoint.x,
            transform.position.y,
            transform.position.z + randomPoint.y
        );

        Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity);
    }

}
