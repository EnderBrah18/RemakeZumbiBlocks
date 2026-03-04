using UnityEngine;


[CreateAssetMenu(menuName = "Data/Score")]
public class ScoreSO : ScriptableObject
{
    public int currentScore;
    public int highScore;
    public int currentWave;
    public int highestWave;
    public int totalCoins;

    public void SaveProgress()
    {
        PlayerPrefs.SetInt("HighScore", highScore);
        PlayerPrefs.SetInt("HighestWave", highestWave);
        PlayerPrefs.SetInt("TotalCoins", totalCoins);

        PlayerPrefs.Save(); // Força a gravação no disco
        Debug.Log("Dados salvos no PlayerPrefs!");
    }

    // Chame isso no início do jogo (ex: no Awake do WaveManager ou Player)
    public void LoadProgress()
    {
        // O segundo valor (0) é o padrão caso não exista nada salvo
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        highestWave = PlayerPrefs.GetInt("HighestWave", 0);
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);

        Debug.Log("Dados carregados do PlayerPrefs!");
    }


    public void EndRun()
    {
        UpdateHighScores();

        int coinsGained = currentScore * currentWave;
        totalCoins += coinsGained;

        Debug.Log($"Run ended! Score: {currentScore}, Coins gained: {coinsGained}, Total coins: {totalCoins}");

        ResetCurrentRun();
    }

    public void UpdateHighScores()
    {
        // Se a pontuação atual for maior que o recorde, atualiza o recorde
        if (currentScore > highScore)
        {
            highScore = currentScore;
        }

        // Se a wave atual for maior que a maior wave já alcançada
        if (currentWave > highestWave)
        {
            highestWave = currentWave;
        }
    }

    public void ResetCurrentRun()
    {
        currentScore = 0;
        currentWave = 1; // Ou 0, dependendo de como seu jogo começa
    }
}
