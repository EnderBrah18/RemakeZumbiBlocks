using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject enemyToSpawn;
    public GameObject playerSlayerPrefab;
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

    public void SpawnPlayerSlayerSquad(Vector3 center)
    {
        List<Enemy> squad = new List<Enemy>();

        for (int i = 0; i < 5; i++)
        {
            Vector3 spawnPos = center + Random.insideUnitSphere * 3f;
            spawnPos.y = center.y;

            GameObject obj = Instantiate(playerSlayerPrefab, spawnPos, Quaternion.identity);
            Enemy enemy = obj.GetComponent<Enemy>();

            squad.Add(enemy);
        }

        Enemy leader = squad[0];
        leader.role = SocialRole.Leader;

        EnemyGroup group = new EnemyGroup(leader, GroupPersonality.Tactical);

        foreach (var member in squad)
        {
            member.JoinGroup(group);
        }
        group.allowNewMembers = false;

        Debug.Log($"Squad criado com {group.members.Count} membros");
    }

}
