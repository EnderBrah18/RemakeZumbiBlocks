using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public enum EnemyFightType
{
    Melee,
    Ranged,
    Boss
}

public enum EnemySizeType
{
    small,
    medium,
    large

}

public class Enemy : MonoBehaviour, IDamageable
{
    public ScoreSO scoreData; // Referência ao ScriptableObject para atualizar a pontuação

    [Header("Stats")]
    public float maxHealth = 100;
    public float currentHealth;
    public float moveSpeed = 3.5f;
    public float chaseSpeed = 5.5f; // Mais rápido quando persegue

    [Header("Combat")]
    public float attackDamage = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("Detection")]
    public float detectionRange = 10f;
    public float stopChasingRange = 15f; // Para não seguir o player pra sempre
    private bool isChasing = false;

    [Header("Visuals & Feedback")]
    public GameObject visualModel;
    public ParticleSystem bloodEffect; // Feedback visual de tiro
    public GameObject bloodPrefab; // Arraste o ARQUIVO do prefab aqui
    private ParticleSystem bloodInstance; // Esta será a cópia na cena

    [Header("UI de Vida")]
    public bool showHealthBar = true; // Define se este inimigo terá barra de vida
    public Canvas worldCanvas;
    public UnityEngine.UI.Image healthBar;

    private NavMeshAgent agent;
    private Transform player;
    private bool isDead = false;

    [Header("Loot System")]
    [Range(0, 100)] public float generalDropChance = 50f; // Chance global de dropar ALGO (0 a 100)
    public List<LootDrop> possibleDrops; // Lista expansível de prefabs (Caixa Rifle, Caixa Pistola, etc)

    private void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        // PROTEÇÃO: Verifica se o agente existe antes de usar
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }
        else
        {
            Debug.LogError($"O inimigo {gameObject.name} está sem NavMeshAgent!");
        }

        player = GameObject.FindGameObjectWithTag("Player").transform;

        agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;

        if (worldCanvas != null)
        {
            worldCanvas.gameObject.SetActive(false);
        }

        if (bloodPrefab != null && bloodEffect == null)
        {
            GameObject go = Instantiate(bloodPrefab, transform);
            bloodInstance = go.GetComponent<ParticleSystem>();
            bloodEffect = bloodInstance;

            // Opcional: Garante que a partícula começa desligada
            bloodEffect.Stop();
        }
    }

    private void Update()
    {
        if (isDead || player == null) return;

        if (showHealthBar && worldCanvas != null && worldCanvas.gameObject.activeSelf)
        {
            worldCanvas.transform.LookAt(worldCanvas.transform.position + Camera.main.transform.forward);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (isChasing)
        {
            HandleChasing(distanceToPlayer);
        }
        else
        {
            if (distanceToPlayer <= detectionRange)
            {
                isChasing = true;
                OnDetectedPlayer(); // Trigger para rugido ou animação
            }
        }

    }

    private void HandleChasing(float dist)
    {
        agent.SetDestination(player.position);
        agent.speed = chaseSpeed;

        if (dist <= attackRange)
        {
            TryAttack();
        }

        if (dist > stopChasingRange)
        {
            isChasing = false;
            agent.speed = moveSpeed;
        }
    }

    private void TryAttack()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            float dist = Vector3.Distance(transform.position, player.position);

            if (dist <= attackRange)
            {
                lastAttackTime = Time.time;

                // "Trava" o zumbi no lugar por um breve momento após o bote
                StartCoroutine(FreezeMovement(0.5f));

                if (player.TryGetComponent(out IDamageable pDamage))
                {
                    pDamage.Damage(attackDamage);
                    // Feedback visual do "bote"
                    visualModel.transform.DOPunchPosition(transform.forward * 0.7f, 0.3f).SetLink(gameObject);
                }
            }
        }
    }

    // Pequena rotina para impedir que ele continue empurrando freneticamente
    IEnumerator FreezeMovement(float duration)
    {
        agent.isStopped = true;
        yield return new WaitForSeconds(duration);
        if (!isDead) agent.isStopped = false;
    }

    public void Damage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        isChasing = true; // Se tomou tiro, ele sabe onde você está!

        // Feedback de dano
        if (visualModel) visualModel.transform.DOShakePosition(0.1f, 0.1f);

        //if (bloodEffect) bloodEffect.Play();
        if (showHealthBar && worldCanvas != null && healthBar != null)
        {
            UpdateHealthUI();
        }

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (agent != null)
        {
            // 1. Para o movimento imediatamente
            agent.isStopped = true;

            // 2. DESATIVA a atualização de posição e rotação pelo NavMesh
            // Isso impede que o agente "puxe" o inimigo de volta para cima
            agent.updatePosition = false;
            agent.updateRotation = false;

            // 3. Opcional: Desativa o componente para garantir que não há conflitos
            agent.enabled = false;
        }

        // 4. Se tiveres um Animator, desativa-o ou ativa a trigger de morte
        // GetComponent<Animator>().enabled = false;

        // 5. Animação de queda (Agora o NavMesh não vai interferir)
        transform.DORotate(new Vector3(-90, 0, 0), 0.5f).SetEase(Ease.OutBounce);

        // 6. Remover o Collider para o player não tropeçar no cadáver
        if (TryGetComponent(out Collider col)) col.enabled = false;

        TryDropLoot();

        Destroy(gameObject, 3f);
        WaveManager.Instance.currentEnemiesAlive--; // Decrementa o contador de inimigos vivos na WaveManager
        scoreData.currentScore++; // Incrementa a pontuação do jogador

        Debug.Log($"{gameObject.name} morreu! Enemies alive: {WaveManager.Instance.currentEnemiesAlive}");
    }
    private void TryDropLoot()
    {
        // 1. Verifica se vai dropar alguma coisa nesta morte
        float randomRoll = Random.Range(0f, 100f);
        if (randomRoll > generalDropChance) return;

        if (possibleDrops == null || possibleDrops.Count == 0) return;

        // 2. Escolhe um item aleatório da lista
        // Você pode expandir isso para usar os pesos (dropChance) de cada item, 
        // mas para começar, vamos pegar um aleatório simples:
        int randomIndex = Random.Range(0, possibleDrops.Count);
        LootDrop selectedDrop = possibleDrops[randomIndex];

        // 3. Instancia o item na posição do inimigo
        // Subimos um pouco no eixo Y (0.5f) para não spawnar dentro do chão
        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
        Instantiate(selectedDrop.itemPrefab, spawnPos, Quaternion.identity);
    }

    private void UpdateHealthUI()
    {
        // Ativa o Canvas apenas no primeiro dano recebido
        if (!worldCanvas.gameObject.activeSelf)
            worldCanvas.gameObject.SetActive(true);

        float fill = currentHealth / maxHealth;
        healthBar.DOFillAmount(fill, 0.2f);
    }

    private void OnDetectedPlayer()
    {
        // Som de rugido ou efeito visual de "!"
        visualModel.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f);
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}

[System.Serializable]
public class LootDrop
{
    public GameObject itemPrefab;
    [Range(0, 100)] public float dropChance; // Chance específica deste item cair
}
