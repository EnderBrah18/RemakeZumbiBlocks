using UnityEngine;
using System.Collections;

public class WaveManager : MonoBehaviour 
{
    public ScoreSO scoreData; // ScriptableObject para armazenar dados da wave, como número da wave, inimigos por wave, etc.

    public static WaveManager Instance { get; private set; }

    public int enemiesSpawnedInThisWave;
    public int waveNumber;
    public int enemiesPerWave;
    public float spawnCooldown = 2f;
    private float timer;

    public bool waveStarted = false;

    public int currentEnemiesAlive;

    public Spawner[] allSpawners;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        Instance= this;
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();

        inputActions.Player.Accept.performed += ctx => StartFirstWave();
    }


    void StartFirstWave()
    {
        if (waveNumber == 0 && enemiesSpawnedInThisWave == 0)
        {
            scoreData.ResetCurrentRun();
            Debug.Log("Starting first wave!");
            NextWave();
            waveStarted = true;
        }
    }

    private void Update()
    {
        if(!waveStarted) return;
        if (enemiesSpawnedInThisWave < enemiesPerWave)
        {
            timer += Time.deltaTime;
            if (timer >= spawnCooldown)
            {
                CallRandomSpawner();
                timer = 0;
            }
        }

        if (enemiesSpawnedInThisWave >= enemiesPerWave && currentEnemiesAlive <= 0)
        {
            // Em vez de chamar NextWave() direto, você poderia disparar um Invoke ou um Timer
            StartCoroutine(WaitNextWave());
        }

    }

    void CallRandomSpawner()
    {
        // Escolhe um spawner aleatório da lista
        int randomIndex = Random.Range(0, allSpawners.Length);
        allSpawners[randomIndex].SpawnSingleEnemy();

        currentEnemiesAlive++; // Incrementa o contador de inimigos vivos
        enemiesSpawnedInThisWave++; // Aqui está o seu limite!
    }

    void NextWave()
    {
        waveNumber++;
        scoreData.currentWave = waveNumber; // Atualiza o ScriptableObject com o número da wave atual
        enemiesPerWave += 5;
        enemiesSpawnedInThisWave = 0; // Reseta para a nova wave
        

        Debug.Log("Wave " + waveNumber + " has started! Enemies to spawn: " + enemiesPerWave);
    }

    IEnumerator WaitNextWave()
    {
        waveStarted = false; // Pausa o spawn
        Debug.Log("Onda finalizada! Prepare-se para a próxima...");
        yield return new WaitForSeconds(5f); // Espera 5 segundos
        NextWave();
        waveStarted = true; // Retoma o spawn
    }
}
