using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public enum EnemyPersonality
{
    Hostile,
    Neutral,
    Friendly
}

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
    public bool respawnableEnemy = false;
    [SerializeField] private GameObject visualModel;

    private Vector3 originalPosition;   

    [Header("Basic Settings")]
    public string enemyName;
    public string enemyType;
    public EnemyPersonality personality;
    public float maxHealth = 100;
    public float currentHealth;
    public int attackDamage = 10;
    public float detectionRange = 5f;
    public float attackRange = 1.5f;

    [Header("Attack Settings")]
    public float attackCooldown = 1f;
    private float lastAttackTime = 0f;

    [Header("Movement Settings")]
    public bool patrol = false;
    public Transform[] patrolPoints;
    public float moveSpeed = 3f;
    private int currentPatrolIndex = 0;
    private NavMeshAgent agent;

    [Header("Tipo do Inimigo")]
    public EnemySizeType enemySizeType;
    public EnemyFightType enemyFightType;

    [Header("Quest Settings")]
    public bool isQuestTarget = false; // Ex: precisa ser morto para completar quest
    public string questName;

    [Header("UI de Vida")]
    public Canvas worldCanvas;
    public Image healthBar;

    private Tween healthTween;

    private Transform player;

    private void Start()
    {
        currentHealth = maxHealth;

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.speed = moveSpeed;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        
    }

    private void Update()
    {
        if (agent != null)
            agent.speed = moveSpeed;

        switch (personality)
        {
            case EnemyPersonality.Hostile:
                HandleHostileBehavior();
                break;
            case EnemyPersonality.Neutral:
                HandleNeutralBehavior();
                break;
            case EnemyPersonality.Friendly:
                HandleFriendlyBehavior();
                break;
        }

        if (patrol && personality != EnemyPersonality.Hostile)
            HandlePatrol();
    }

    #region Behavior
    private void HandleHostileBehavior()
    {
        if (player == null || agent == null) return;

        if (agent == null || !agent.isOnNavMesh || !agent.enabled)
            return;

        float dist = Vector3.Distance(transform.position, player.position);

        // Jogador dentro da área de detecção
        if (dist < detectionRange)
        {
            agent.SetDestination(player.position);

            // Atacar
            if (dist <= attackRange)
            {
                AttackPlayer();
            }
        }
        else
        {
            // Jogador fugiu -> voltar para posição original
            agent.SetDestination(originalPosition);

            // Opcional: rotacionar lentamente enquanto volta
            Vector3 dir = originalPosition - transform.position;
            dir.y = 0;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5);
        }
    }

    private void HandleNeutralBehavior()
    {
        // Por padrão, neutros não atacam e só patrulham
        HandlePatrol();
    }

    private void HandleFriendlyBehavior()
    {
        // Amigáveis podem seguir o jogador ou dar buffs, etc.
    }

    private void HandlePatrol()
    {
        if (patrol && patrolPoints.Length > 0 && agent != null)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);

            if (Vector3.Distance(transform.position, patrolPoints[currentPatrolIndex].position) < 0.2f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
        }
    }
    #endregion

    #region Combat
    private void AttackPlayer()
    {
        // Cooldown
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        //Player playerComponent = player.GetComponent<Player>();
        //if (playerComponent == null) return;

        // Virar para o jogador
        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        transform.rotation = Quaternion.LookRotation(dir);

        // Atacar
        //playerComponent.TakeDamage(Mathf.RoundToInt(attackDamage));

        Debug.Log($"{enemyName} atacou o jogador causando {attackDamage} de dano!");
    }

    public bool isDead = false;

    public void Damage(float amount)
    {
        Debug.Log("TOMOU DANO");

        if (isDead) return; // <- evita lógica de morte repetida

        currentHealth -= amount;

        if (worldCanvas != null)
            worldCanvas.gameObject.SetActive(true);

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;


        Debug.Log($"{enemyName} morreu!");

        HideHealthUI();
        if (respawnableEnemy)
        {
            visualModel.SetActive(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    public void InitializeAfterNavmesh()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        // tenta encontrar a posição válida mais próxima no navmesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            originalPosition = hit.position;

            Debug.Log($"{enemyName}: Posicionamento corrigido após navmesh: {originalPosition}");
        }
        else
        {
            originalPosition = transform.position;
            Debug.LogWarning($"{enemyName}: NÃO encontrou posição no navmesh após build!");
        }
    }

    public void ApplyKnockback(Vector3 direction, float force)
    {
        StartCoroutine(KnockbackCoroutine(direction, force));
    }

    private IEnumerator KnockbackCoroutine(Vector3 dir, float force)
    {
        if (agent == null) yield break;

        // desativa o navmesh agent temporariamente
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        agent.updatePosition = false;
        agent.updateRotation = false;

        float t = 0f;
        float duration = 0.15f; // tempo do empurrão

        Vector3 start = transform.position;
        Vector3 end = start + dir.normalized * force;

        // anima o deslocamento manualmente
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        // reativa o navmesh agent
        agent.Warp(transform.position); // atualiza a posição no navmesh
        agent.updatePosition = true;
        agent.updateRotation = true;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    public void UpdateHealthUI(bool instant = false)
    {
        if (healthBar == null) return;

        float fill = (float)currentHealth / maxHealth;

        if (healthTween != null && healthTween.IsActive())
            healthTween.Kill();

        // Cria a nova tween da barra de vida
        healthTween = healthBar
            .DOFillAmount(fill, 0.25f)
            .SetEase(Ease.OutQuad);

    }

    private void HideHealthUI()
    {
        if (worldCanvas != null)
            worldCanvas.gameObject.SetActive(false);

        healthTween?.Kill();

    }
}
