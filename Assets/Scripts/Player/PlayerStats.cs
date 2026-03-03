using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerStats : MonoBehaviour, IDamageable
{
    public ScoreSO scoreData; // Referência ao ScriptableObject para atualizar a pontuação

    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI")]
    public Image healthBarFill; // Arraste a imagem da barra de vida do HUD aqui
    public Image damageOverlay;

    [Header("Damage Shake")]
    public Transform shakePivot; // Arraste o Camera_Shake_Pivot aqui
    public float shakeDuration = 0.2f;
    public float shakeStrength = 0.3f;


    void Awake()
    {
        currentHealth = maxHealth;

        if (damageOverlay != null)
        {
            damageOverlay.color = new Color(damageOverlay.color.r, damageOverlay.color.g, damageOverlay.color.b, 0);
        }

        UpdateUI();
    }

    public void Damage(float amount)
    {
        currentHealth -= amount;
        UpdateUI();

        // 1. Corrigindo o Damage Overlay (Sangue)
        if (damageOverlay != null)
        {
            damageOverlay.DOKill();
            // Garante que o objeto está visível antes do fade
            damageOverlay.gameObject.SetActive(true);
            damageOverlay.DOFade(0.5f, 0.1f).OnComplete(() => damageOverlay.DOFade(0, 0.5f));
        }

        // 2. Shake de Dano via variável de Offset
        PlayerLook look = GetComponentInChildren<PlayerLook>();
        if (look != null)
        {
            // Anima a variável shakeOffset do PlayerLook
            DOTween.Shake(() => look.shakeOffset, x => look.shakeOffset = x, 0.3f, 5f, 20);
        }

        if (currentHealth <= 0) Die();
    }

    void UpdateUI()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = currentHealth / maxHealth;
        }
    }

    void Die()
    {
        Debug.Log("O Jogador Morreu!");

        scoreData.EndRun(); // Atualiza os high scores e total de moedas
        WaveManager.Instance.waveStarted = false; // Reseta o estado das waves para o início


        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        DOTween.KillAll();
        SceneManager.LoadScene("MainMenu");

        // Aqui podes recarregar a cena ou mostrar tela de Game Over
        // UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
