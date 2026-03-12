using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    List<Enemy> enemies = new List<Enemy>();

    [Header("AI Update")]
    public int enemiesPerTick = 10;
    public float tickInterval = 0.1f;

    int currentIndex;
    float nextTick;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        enemies.Remove(enemy);
    }

    void Update()
    {
        if (Time.time < nextTick) return;

        nextTick = Time.time + tickInterval;

        if (enemies.Count == 0) return;

        int processed = 0;

        while (processed < enemiesPerTick && enemies.Count > 0)
        {
            if (currentIndex >= enemies.Count)
                currentIndex = 0;

            enemies[currentIndex].TickAI();

            currentIndex++;
            processed++;
        }
    }
}
