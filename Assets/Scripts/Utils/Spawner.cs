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

    public void SpawnSquad(Vector3 centerPos, int size)
    {
        // 1. Spawna o Líder
        GameObject leaderObj = Instantiate(enemyToSpawn, centerPos, Quaternion.identity);
        Enemy leader = leaderObj.GetComponent<Enemy>();
        leader.role = SocialRole.Leader;

        // Força a criação do grupo antes dos outros nascerem
        leader.ForceCreateGroup();

        // 2. Spawna os soldados ao redor
        for (int i = 0; i < size - 1; i++)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
            GameObject soldierObj = Instantiate(enemyToSpawn, centerPos + randomOffset, Quaternion.identity);
            Enemy soldier = soldierObj.GetComponent<Enemy>();

            soldier.role = SocialRole.Soldier;
            soldier.JoinGroup(leader.currentGroup);
        }
    }

}
