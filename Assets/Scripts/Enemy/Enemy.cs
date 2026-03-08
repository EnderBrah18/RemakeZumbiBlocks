using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum EnemyFightType
{
    Melee,
    Ranged,
    Boss
}

public enum SocialRole
{
    Leader,     // O cérebro do grupo
    Soldier,    // Segue as ordens do líder e preenche slots
    LoneWolf    // Ignora o grupo, usa a lógica de avoidance antiga
}

public enum SocialBehavior
{
    Proactive,
    Passive,
    Rebellious
}

public enum GroupPersonality
{
    Aggressive,  // Vai direto para o player, não se importa com slots
    Defensive,   // Tenta manter distância e usar o ambiente a seu favor
    Supportive   // Prioriza ficar perto do líder e ajudar os outros membros
}
public enum EnemyFocus
{
    PlayerSlayer,   // Foco total no Jogador
    Bystander,      // Neutro, só ataca se for provocado (Player ou NPCs)
    Hunter,         // Foca em NPCs e defesas antes do objetivo
    Guardian,       // Foca em destruir as defesas (Torres/Barricadas)
    ObjectiveRooted // Ignora tudo e corre para o ponto central
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

    [HideInInspector]
    public float speedMultiplier = 1f;

    [Header("Combat")]
    public float attackDamage = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("Detection")]
    public float stopChasingRange = 15f; // Para não seguir o player pra sempre
    public bool isChasing = false;
    public Transform targetPlayer;

    private float lastDamageTime;
    public float memoryAfterDamage = 5f;

    [Header("Visuals & Feedback")]
    public GameObject visualModel;
    public ParticleSystem bloodEffect; // Feedback visual de tiro
    public GameObject bloodPrefab; // Arraste o ARQUIVO do prefab aqui
    private ParticleSystem bloodInstance; // Esta será a cópia na cena

    [Header("UI de Vida")]
    public bool showHealthBar = true; // Define se este inimigo terá barra de vida
    public Canvas worldCanvas;
    public UnityEngine.UI.Image healthBar;
    private Transform player;
    private bool isDead = false;

    [Header("Loot System")]
    [Range(0, 100)] public float generalDropChance = 50f; // Chance global de dropar ALGO (0 a 100)
    public List<LootDrop> possibleDrops; // Lista expansível de prefabs (Caixa Rifle, Caixa Pistola, etc)

    [Header("AI Intelligence")]
    [Range(0, 1)] public float intelligenceLevel = 0.5f;

    private EnemySensors sensors;
    private EnemyMovement movement;

    [Header("Group Logic")]
    public float groupDetectionRadius = 5f;
    private Vector3 currentSlotPosition;

    [Header("Social Settings")]
    public SocialRole role;
    public SocialBehavior socialBehavior;

    [System.NonSerialized]
    public EnemyGroup currentGroup;

    [Header("Final State AI")]
    public EnemyFocus focus; // O que este inimigo prioriza
    public float viewDistance = 15f; // Distância de busca (sincronizar com Sensors)
    public LayerMask targetMask; // Layer do Player, NPCs e Objetivos

    [Header("AI Context")]
    public Transform navigationTarget; // Onde o líder quer chegar (ex: Objetivo)
    public Transform combatTarget;     // Quem eu estou tentando bater agora (ex: Player/NPC)

    private void Awake()
    {
        sensors = GetComponent<EnemySensors>();
        movement = GetComponent<EnemyMovement>();
    }

    private void Start()
    {
        Debug.Log($"{name} iniciou com role = {role} e group = {currentGroup}");

        currentHealth = maxHealth;
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

       float roll = Random.value;
       if (roll < 0.1f) role = SocialRole.Leader; // 10% de chance de ser líder nato
       else if (roll < 0.2f) role = SocialRole.LoneWolf; // 10% de lobo solitário
       else role = SocialRole.Soldier;

        if (role == SocialRole.Leader && currentGroup == null)
        {
            currentGroup = new EnemyGroup(this);
            currentGroup.leader = this;
            currentGroup.members.Add(this);
            Debug.Log($"<color=orange>{name} nasceu como Líder e inicializou seu próprio grupo.</color>");
        }

        // Exemplo: 20% de chance de ser focado no Objetivo
        if (Random.value < 0.2f) focus = EnemyFocus.ObjectiveRooted;
        else focus = EnemyFocus.PlayerSlayer;

        speedMultiplier = Random.Range(0.9f, 1.2f);
    }

    private void Update()
    {
        if (isDead) return;

        HandleGroupLogic();

        HandleAI();
        HandleDetection();
        UpdateUI();

        if (isChasing && targetPlayer != null)
        {
            float dist = Vector3.Distance(transform.position, targetPlayer.position);
            if (dist < attackRange * 0.8f) // Se está "esfregando" no player
            {
                TryAttack();
            }
        }

    }
    private void TryAttack()
    {
        if (targetPlayer == null || isDead) return;

        float currentDist = Vector3.Distance(transform.position, targetPlayer.position);

        // 1. Aumentamos a tolerância da distância para 1.5f para compensar colisores largos
        if (Time.time >= lastAttackTime + attackCooldown && currentDist <= attackRange * 1.5f)
        {
            // 2. BUSCA INTELIGENTE: Procura o IDamageable no objeto, nos pais ou nos filhos
            // GetComponentInParent é o mais seguro se o colisor for a cabeça e o script estiver no corpo
            IDamageable pDamage = targetPlayer.GetComponentInParent<IDamageable>();

            if (pDamage != null)
            {
                lastAttackTime = Time.time;
                pDamage.Damage(attackDamage);

                if (visualModel)
                    visualModel.transform.DOPunchPosition(transform.forward * 0.5f, 0.2f);

                Debug.Log($"<color=red>Dano causado em:</color> {targetPlayer.name} (ou seu pai)");
            }
            else
            {
                // Debug para você saber se ainda está errando o alvo
                Debug.LogWarning($"Inimigo tentou bater em {targetPlayer.name}, mas não achou IDamageable!");
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Colidiu com: " + collision.gameObject.name);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (isDead) return;

        // Se encostar em algo que seja o player, tenta bater imediatamente
        if (collision.gameObject.CompareTag("Player"))
        {
            TryAttack();
            Debug.Log("Ainda colidindo com: " + collision.gameObject.name);
        }
    }
    private void HandleDetection()
    {
        if (isDead) return;

        // 1. O Sensor agora retorna uma LISTA de tudo que ele vê (Player, NPCs, Objetivos)
        List<Transform> visibleTargets = sensors.GetAllVisibleTargets();

        Transform bestTarget = null;
        float highestScore = -1000f;

        foreach (Transform t in visibleTargets)
        {
            float score = CalculatePriority(t);
            if (score > highestScore)
            {
                highestScore = score;
                bestTarget = t;
            }
        }

        // 2. Se achou um alvo melhor do que o atual (ou se não tinha nenhum)
        if (bestTarget != null)
        {
            if (targetPlayer == null || targetPlayer != bestTarget)
            {
                targetPlayer = bestTarget;
                isChasing = true;
                // Se for líder, avisa o grupo do novo foco estratégico
                currentGroup?.AlertGroup(targetPlayer);
            }
        }
    }


    private void HandleAI()
    {
        if (isDead) return;

        // 1. ATUALIZA QUEM EU DEVO ATACAR (Baseado na minha personalidade)
        combatTarget = FindBestTarget(); // Usa aquele sistema de score/prioridade

        // 2. DEFINE O DESTINO DE MOVIMENTO
        Vector3 destination;

        if (combatTarget != null)
        {
            // Se tenho alguém para bater, meu destino é o meu alvo de combate
            destination = combatTarget.position;
            isChasing = true;
        }
        else if (role == SocialRole.Soldier && currentGroup != null)
        {
            // Se não tenho ninguém para bater, sigo o Slot do grupo (que vai para o objetivo do Líder)
            destination = GetCurrentSlot();
            isChasing = true;
        }
        else
        {
            // Se sou líder ou LoneWolf sem alvo, vou para o objetivo global
            destination = navigationTarget != null ? navigationTarget.position : transform.position;
        }

        // 3. EXECUTA O MOVIMENTO
        float distToCombat = combatTarget != null ? Vector3.Distance(transform.position, combatTarget.position) : 999f;

        // Lei da Oportunidade: Ataca se houver alvo de combate perto
        if (distToCombat <= attackRange * 1.2f)
        {
            TryAttack();
        }

        movement.MoveTowards(destination, intelligenceLevel < 0.5f, currentGroup?.personality == GroupPersonality.Aggressive);
    }

    private void UpdateUI()
    {
        if (showHealthBar && worldCanvas != null && worldCanvas.gameObject.activeSelf)
        {
            worldCanvas.transform.LookAt(worldCanvas.transform.position + Camera.main.transform.forward);
        }
    }


    public void Damage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        lastDamageTime = Time.time;

        // Ao tomar dano, ignoramos o sensor de visão e focamos no player imediatamente
        if (!isChasing || targetPlayer == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                targetPlayer = playerObj.transform;
                isChasing = true;
                OnDetectedPlayer();
            }
        }

        if (currentGroup != null)
        {
            currentGroup.AlertGroup(targetPlayer);
        }

        if (visualModel)
        {
            visualModel.transform.DOKill(); // Para o shake anterior antes de começar um novo
            visualModel.transform.DOShakePosition(0.1f, 0.1f);
        }

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


        // 4. Se tiveres um Animator, desativa-o ou ativa a trigger de morte
        // GetComponent<Animator>().enabled = false;

        if (role == SocialRole.Leader && currentGroup != null)
        {
            // Remove o líder da lista imediatamente para o próximo da fila assumir
            currentGroup.members.Remove(this);
            currentGroup.TransferLeadership();
        }

        // Desativa movimento e colisões físicas
        if (TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
        if (TryGetComponent(out Collider col)) col.enabled = false;

        // 5. Animação de queda (Agora o NavMesh não vai interferir)
        transform.DORotate(new Vector3(-90, 0, 0), 0.5f).SetEase(Ease.OutBounce);


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

    private void HandleGroupLogic()
    {
        if (role == SocialRole.LoneWolf || isDead) return;

        int enemyLayer = LayerMask.GetMask("Enemy");

        // Lógica para o Líder
        if (role == SocialRole.Leader && currentGroup != null)
        {
            currentGroup.UpdateGroup();

            Collider[] neighbors = Physics.OverlapSphere(transform.position, groupDetectionRadius, enemyLayer);
            foreach (var col in neighbors)
            {
                Enemy other = col.GetComponentInParent<Enemy>();
                if (other == null || other == this || other.isDead) continue;

                // 1. RECRUTAMENTO: Se o outro não tem grupo, entra no meu
                if (other.currentGroup == null && other.role != SocialRole.LoneWolf)
                {
                    other.JoinGroup(currentGroup);
                }

                // 2. FUSÃO (MergeInto): Se o outro TAMBÉM é líder de um grupo
                else if (other.role == SocialRole.Leader && other.currentGroup != null && other.currentGroup != this.currentGroup)
                {
                    // Lei do mais forte: O grupo com menos membros é absorvido
                    if (this.currentGroup.members.Count >= other.currentGroup.members.Count)
                    {
                        Debug.Log($"<color=orange>{this.name} absorveu o grupo de {other.name}</color>");
                        other.currentGroup.MergeInto(this.currentGroup);
                    }
                }
            }
        }
    }
    private bool hasSlot;


    public Vector3 GetCurrentSlot()
    {
        return currentSlotPosition;
    }

    public void SetGroupSlot(Vector3 pos)
    {
        currentSlotPosition = pos;
        hasSlot = true;
    }

    public void ClearGroupSlot()
    {
        hasSlot = false;
        currentSlotPosition = Vector3.zero;
    }

    private void CreateGroup(Enemy other)
    {
        currentGroup = new EnemyGroup(this);
        currentGroup.leader = this; // Define quem manda
        this.role = SocialRole.Leader; // Garante o cargo

        currentGroup.members.Add(this);
        other.JoinGroup(currentGroup);

        Debug.Log($"<color=cyan>{name} criou um novo grupo e recrutou {other.name}!</color>");
    }

    public void JoinGroup(EnemyGroup group)
    {
        // Lógica de recusa
        if (socialBehavior == SocialBehavior.Rebellious && group.members.Count > 3)
        {
            Debug.Log($"{name}: 'Não sigo grupos grandes!'");
            return;
        }

        currentGroup = group;
        if (!currentGroup.members.Contains(this)) currentGroup.members.Add(this);
    }

    public void LeaveGroup()
    {
        currentGroup?.members.Remove(this);
        currentGroup = null;

        Debug.Log($"{gameObject.name} <color=redn>entrou</color> saiu do grupo");
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || isDead) return;

        // 1. Desenha uma linha até o líder se estiver em grupo
        if (currentGroup != null && currentGroup.leader != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentGroup.leader.transform.position);

            // 2. Desenha uma esfera onde o "Slot" (vaga) do inimigo está
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentSlotPosition, 0.3f);
            Gizmos.DrawLine(transform.position, currentSlotPosition);
        }

        // 3. Diferencia o Líder visualmente no Editor
        if (role == SocialRole.Leader)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
        }
        // 4. Diferencia o Lobo Solitário
        else if (role == SocialRole.LoneWolf)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
        }
    }

    public void ForceCreateGroup()
    {
        if (currentGroup == null)
        {
            role = SocialRole.Leader;
            currentGroup = new EnemyGroup(this);
            // O construtor do EnemyGroup já adiciona 'this' à lista de membros
            Debug.Log($"<color=green>{name} forçou a criação de um grupo estratégico.</color>");
        }
    }

    private Transform FindBestTarget()
    {
        // Certifique-se de que 'viewDistance' e 'targetMask' existam (veja o erro 3 abaixo)
        Collider[] potentialTargets = Physics.OverlapSphere(transform.position, viewDistance, targetMask);
        Transform bestTarget = null;
        float highestPriority = -1f;

        foreach (var col in potentialTargets)
        {
            // Passamos o transform do collider para o cálculo
            float priority = CalculatePriority(col.transform);
            if (priority > highestPriority)
            {
                highestPriority = priority;
                bestTarget = col.transform; // Aqui pegamos o .transform do collider
            }
        }
        return bestTarget;
    }

    private float CalculatePriority(Transform target) // Mude de Collider para Transform
    {
        float score = 0;
        float dist = Vector3.Distance(transform.position, target.position);

        score += (viewDistance - dist);

        if (target.CompareTag("Player"))
        {
            // Use 'focus' em vez de 'personality' se você renomeou a variável para os alvos
            if (focus == EnemyFocus.PlayerSlayer) score += 100;
            if (focus == EnemyFocus.Bystander && currentHealth < maxHealth) score += 200;
        }
        else if (target.CompareTag("NPC"))
        {
            if (focus == EnemyFocus.Hunter) score += 150;
        }
        else if (target.CompareTag("Objective"))
        {
            if (focus == EnemyFocus.ObjectiveRooted) score += 500;
        }

        return score;
    }

}

[System.Serializable]
public class LootDrop
{
    public GameObject itemPrefab;
    [Range(0, 100)] public float dropChance; // Chance específica deste item cair
}
