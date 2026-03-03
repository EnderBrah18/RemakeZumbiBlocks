using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MenuScore : MonoBehaviour
{
    public ScoreSO scoreData;

    public TextMeshProUGUI highestWaveText;
    public TextMeshProUGUI highScoreText;

    public TextMeshProUGUI moneyText;

    [Header("Cena e Spawn")]
    public string sceneToLoad;
    public string spawnPointID = "Default";

    private void Start()
    {
        UpdateScoreUI();

        // Procura o botão na cena
        Button startButton = GameObject.Find("Start").GetComponent<Button>();

        // Adiciona a função de clique via código
        // O SceneLoader.Instance deve ser o seu Singleton que persistiu
        startButton.onClick.AddListener(() => SceneLoader.Instance.LoadScene(sceneToLoad, spawnPointID));
    }

    void UpdateScoreUI()
    {
        if (scoreData != null)
        {
            highestWaveText.text = $"Highest Wave: {scoreData.highestWave}";
            highScoreText.text = $"High Score: {scoreData.highScore}";
            moneyText.text = $"Money: {scoreData.totalCoins}";
        }
        else
        {
            Debug.LogError("MenuScore: ScoreSO não atribuído.");
        }

    }
}
