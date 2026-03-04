using UnityEngine;


[CreateAssetMenu(menuName = "Data/Score")]
public class ScoreSO : ScriptableObject
{
    public int currentScore;
    public int highScore;
    public int currentWave;
    public int highestWave;
    public int totalCoins;


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
