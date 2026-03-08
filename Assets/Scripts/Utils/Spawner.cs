using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject enemyToSpawn;
    public float spawnRadius = 2f;


    public GameObject SpawnSingleEnemy() // Mudamos de void para GameObject
    {
        Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;

        Vector3 spawnPosition = new Vector3(
            transform.position.x + randomPoint.x,
            transform.position.y,
            transform.position.z + randomPoint.y
        );

        // Armazenamos a instância em uma variável
        GameObject newEnemy = Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity);

        // Retornamos o objeto para quem chamou o método (o WaveManager)
        return newEnemy;
    }

}
