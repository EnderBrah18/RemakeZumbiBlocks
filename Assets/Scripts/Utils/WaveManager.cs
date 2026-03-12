using UnityEngine;
using System.Collections;
using TMPro;
using DG.Tweening;

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

    [Header("UI References")]
    public TextMeshProUGUI startText;    // Texto "Enter to Start"
    public TextMeshProUGUI waveBanner;   // Texto "Wave 1" que aparece no centro

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

            // Esconde o texto de instrução
            if (startText != null) startText.gameObject.SetActive(false);

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
        int randomIndex = Random.Range(0, allSpawners.Length);
        // Captura o retorno do spawner
        GameObject newEnemy = allSpawners[randomIndex].SpawnSingleEnemy();

        if (newEnemy.TryGetComponent(out Enemy e))
        {
            // 70% chance de ser inteligente (1f), 30% chance de ser "burro" (0.1f)
            e.intelligenceLevel = Random.value > 0.3f ? 0.5f : 0.1f;
        }

        currentEnemiesAlive++;
        enemiesSpawnedInThisWave++;
    }

    void NextWave()
    {
        waveNumber++;
        scoreData.currentWave = waveNumber; // Atualiza o ScriptableObject com o número da wave atual
        enemiesPerWave += 5;
        enemiesSpawnedInThisWave = 0; // Reseta para a nova wave

        ShowWaveBanner();

        Debug.Log("Wave " + waveNumber + " has started! Enemies to spawn: " + enemiesPerWave);
    }

    void ShowWaveBanner()
    {
        if (waveBanner != null)
        {
            waveBanner.text = "WAVE " + waveNumber;

            // Sequência de Fade usando DOTween
            waveBanner.DOKill(); // Para fades anteriores se houver
            Sequence s = DOTween.Sequence();

            s.Append(waveBanner.DOFade(1f, 0.5f)); // Aparece em 0.5s
            s.AppendInterval(2f);                  // Fica na tela por 2s
            s.Append(waveBanner.DOFade(0f, 1f));   // Desaparece em 1s
        }
    }

    IEnumerator WaitNextWave()
    {
        waveStarted = false; // Pausa o spawn
        Debug.Log("Onda finalizada! Prepare-se para a próxima...");

        waveBanner.text = "Onda finalizada! Prepare-se para a próxima...";

        yield return new WaitForSeconds(5f); // Espera 5 segundos
        NextWave();
        waveStarted = true; // Retoma o spawn
    }

    private void OnApplicationQuit()
    {
        scoreData.SaveProgress();
    }
}
