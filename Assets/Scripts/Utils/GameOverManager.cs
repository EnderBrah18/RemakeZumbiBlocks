using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public ScoreSO scoreData; // Arraste seu ScriptableObject de Score aqui

    public static GameOverManager Instance;

    [Header("Referências de UI")]
    public CanvasGroup gameOverCanvasGroup; // Arraste o Canvas Group do Painel aqui
    public Button mainMenuButton;          // Arraste o botão do Menu aqui
    public GameObject hudCanvas;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Garante que a UI comece invisível e bloqueada
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0;
            gameOverCanvasGroup.interactable = false;
            gameOverCanvasGroup.blocksRaycasts = false;
        }
    }

    private void Start()
    {
        // Configura o botão para chamar a função de carregar o menu
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    public void TriggerGameOver()
    {
        // 1. Dados e HUD do Player
        scoreData.EndRun();
        if (WaveManager.Instance != null) WaveManager.Instance.waveStarted = false;

        if (hudCanvas != null) hudCanvas.SetActive(false);

        // 2. Mostrar Game Over
        if (gameOverCanvasGroup != null)
        {
            // Forçamos o objeto a estar ativo
            gameOverCanvasGroup.gameObject.SetActive(true);

            // Resetamos a escala (as vezes o Time.Scale 0 buga a escala se houver animação)
            gameOverCanvasGroup.transform.localScale = Vector3.one;

            // Interrompemos qualquer animação anterior no CanvasGroup
            gameOverCanvasGroup.DOKill();

            // Fazemos o Fade In ignorando o pause (SetUpdate(true))
            gameOverCanvasGroup.DOFade(1f, 0.5f)
                .SetUpdate(true)
                .OnComplete(() => {
                    gameOverCanvasGroup.interactable = true;
                    gameOverCanvasGroup.blocksRaycasts = true;
                });

            // Caso o Fade falhe por algum motivo de conflito, garantimos o Alpha em 0.1s
            DOVirtual.DelayedCall(0.1f, () => {
                if (gameOverCanvasGroup.alpha < 0.1f) gameOverCanvasGroup.alpha = 1f;
            }).SetUpdate(true);
        }

        // 3. Pausa e Mouse
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void GoToMainMenu()
    {
        // 5. IMPORTANTE: Resetar o tempo antes de trocar de cena!
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
        DOTween.KillAll();
    }
}
