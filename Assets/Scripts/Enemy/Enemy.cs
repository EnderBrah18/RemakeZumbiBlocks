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

    [System.NonSerialized]
    public EnemyGroup currentGroup;

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

       // float roll = Random.value;
        //if (roll < 0.1f) role = SocialRole.Leader; // 10% de chance de ser líder nato
       // else if (roll < 0.2f) role = SocialRole.LoneWolf; // 10% de lobo solitário
       // else role = SocialRole.Soldier;

        if (role == SocialRole.Leader && currentGroup == null)
        {
            currentGroup = new EnemyGroup();
            currentGroup.leader = this;
            currentGroup.members.Add(this);
            Debug.Log($"<color=orange>{name} nasceu como Líder e inicializou seu próprio grupo.</color>");
        }

        speedMultiplier = Random.Range(0.9f, 1.2f);
    }

    private void Update()
    {
        if (isDead) return;

        HandleGroupLogic();

        HandleAI();
        HandleDetection();
        UpdateUI();

    }
    private void TryAttack()
    {
        if (targetPlayer == null) return;

        // Verifica a distância real novamente antes de aplicar o dano
        float currentDist = Vector3.Distance(transform.position, targetPlayer.position);

        if (Time.time >= lastAttackTime + attackCooldown && currentDist <= attackRange * 1.2f)
        {
            lastAttackTime = Time.time;

            // Garante que estamos pegando o componente IDamageable do Player
            // e não de qualquer outra coisa que possa ter entrado no targetPlayer
            if (targetPlayer.CompareTag("Player") && targetPlayer.TryGetComponent(out IDamageable pDamage))
            {
                pDamage.Damage(attackDamage);

                if (visualModel)
                    visualModel.transform.DOPunchPosition(transform.forward * 0.7f, 0.3f).SetLink(gameObject);

                Debug.Log($"Inimigo atacou o player. Distância: {currentDist}");
            }
        }
    }

    private void HandleDetection()
    {
        // 1. Tenta detectar visualmente
        Transform detected = sensors != null ? sensors.CheckVisualDetection() : null;

        // 2. Se o sensor viu alguém, atualiza o alvo (visão tem prioridade)
        if (detected != null)
        {
            if (!isChasing) OnDetectedPlayer();
            targetPlayer = detected;
            isChasing = true;
        }
        // 3. Se o sensor NÃO viu ninguém, mas já estamos perseguindo (ex: por causa do Dano)
        else if (isChasing && targetPlayer != null)
        {
            // Mantemos a perseguição ativa, a menos que o player fuja para muito longe
            float dist = Vector3.Distance(transform.position, targetPlayer.position);

            // SÓ para de perseguir se estiver longe E se já passou o tempo de memória do dano
            bool lostDamageMemory = Time.time > lastDamageTime + memoryAfterDamage;

            if (dist > stopChasingRange && lostDamageMemory)
            {
                isChasing = false;
                targetPlayer = null;
            }
            // NOTA: Não limpamos o targetPlayer aqui se ele estiver dentro do alcance, 
            // permitindo que o inimigo continue indo atrás de quem o deu dano.
        }
    }

    private bool hasSlot;

    private void HandleAI()
    {
        if (!isChasing || targetPlayer == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, targetPlayer.position);

        // Ajusta a velocidade do movement quando estiver perseguindo
        if (movement != null) movement.moveSpeed = chaseSpeed;

        // Se estiver fora do alcance de ataque, move-se em direção ao alvo
        if (distanceToTarget > attackRange * 0.8f)
        {
            bool ignoreGroundCheck = intelligenceLevel < 0.5f;

            // NOVIDADE: Verifica se o slot não é zero antes de ir para lá
            // Se o slot estiver muito próximo do player, abandonamos o slot e vamos direto ao player para atacar.
            bool hasValidSlot = currentGroup != null && hasSlot && Vector3.Distance(GetCurrentSlot(), targetPlayer.position) > attackRange * 0.6f;
            Vector3 destination = hasValidSlot ? GetCurrentSlot() : targetPlayer.position;

            movement.MoveTowards(destination, ignoreGroundCheck);
        }
        else
        {
            // Se estiver perto, tenta atacar
            TryAttack();
        }
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

        // Se eu sou líder mas ainda não tenho grupo -> crio
        if (role == SocialRole.Leader && currentGroup == null)
        {
            currentGroup = new EnemyGroup();
            currentGroup.leader = this;
            currentGroup.members.Add(this);
        }

        if (role == SocialRole.Leader && currentGroup != null)
        {
            currentGroup.UpdateGroup();

            Collider[] neighbors = Physics.OverlapSphere(transform.position, groupDetectionRadius, enemyLayer);

            foreach (var col in neighbors)
            {
                Enemy other = col.GetComponentInParent<Enemy>();

                if (other != null && other != this && other.currentGroup == null && other.role != SocialRole.LoneWolf)
                {
                    other.JoinGroup(currentGroup);
                }
            }

            return;
        }
    }

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
        currentGroup = new EnemyGroup();
        currentGroup.leader = this; // Define quem manda
        this.role = SocialRole.Leader; // Garante o cargo

        currentGroup.members.Add(this);
        other.JoinGroup(currentGroup);

        Debug.Log($"<color=cyan>{name} criou um novo grupo e recrutou {other.name}!</color>");
    }

    public void JoinGroup(EnemyGroup group)
    {
        currentGroup = group;
        if (!currentGroup.members.Contains(this)) currentGroup.members.Add(this);

        Debug.Log($"{gameObject.name} <color=green>entrou</color> no grupo do líder {group.leader.name}");
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

    [ContextMenu("Forçar Criação de Grupo")]
    public void ForceCreateGroup()
    {
        if (isDead) return;

        // 1. Reset total deste líder
        if (currentGroup != null) LeaveGroup();

        this.role = SocialRole.Leader;
        currentGroup = new EnemyGroup();
        currentGroup.leader = this;
        currentGroup.members.Add(this);

        // 2. Busca vizinhos em um raio maior (ex: 15) para garantir que pegue alguém
        int enemyLayer = LayerMask.GetMask("Enemy");
        Collider[] neighbors = Physics.OverlapSphere(transform.position, 15f, enemyLayer);

        int recrutasCount = 0;
        foreach (var col in neighbors)
        {
            // O segredo está aqui: busca o script no objeto ou em qualquer pai dele
            Enemy other = col.GetComponentInParent<Enemy>();

            if (other != null && other != this)
            {
                other.LeaveGroup();
                other.JoinGroup(this.currentGroup);
                recrutasCount++;
            }
        }

        Debug.Log($"<color=cyan><b>{name}</b></color> sequestrou {recrutasCount} recrutas para seu novo grupo!");
    }



}

[System.Serializable]
public class LootDrop
{
    public GameObject itemPrefab;
    [Range(0, 100)] public float dropChance; // Chance específica deste item cair
}
