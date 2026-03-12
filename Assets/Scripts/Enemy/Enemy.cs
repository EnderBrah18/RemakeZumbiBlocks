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
    Aggressive,
    Tactical
}
public enum EnemyFocus
{
    PlayerSlayer,   // Foco total no Jogador
    Hunter,         // Foca em NPCs e defesas antes do objetivo
    Guardian,       // Foca em destruir as defesas (Torres/Barricadas)
    ObjectiveRooted // Ignora tudo e corre para o ponto central
}

public enum EnemyPersonality { Disciplined, Loyal, Wild, Berserker }
public enum GroupIntent { Idle, MarchToCore, AttackPlayer, AttackObjective, DefendArea }

public enum EnemyState { Idle, Combat, Regroup, Marching }

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
    public EnemyPersonality personality;
    public float groupLeashDistance = 25f;
    public bool isDeserter = false;

    [System.NonSerialized]
    public EnemyGroup currentGroup;

    [Header("Final State AI")]
    public EnemyFocus focus; // O que este inimigo prioriza
    public float viewDistance = 15f; // Distância de busca (sincronizar com Sensors)
    public LayerMask targetMask; // Layer do Player, NPCs e Objetivos

    [Header("AI Context")]
    public Transform navigationTarget; // Onde o líder quer chegar (ex: Objetivo)
    public Transform combatTarget;     // Quem eu estou tentando bater agora (ex: Player/NPC)
    private Vector3 targetDestination;
    public EnemyState state;
    [Header("Aggro Override")]
    public float aggroOverrideDistance = 4f;
    float forcedAggroTimer = 0f;

    private float targetLostTimer;
    public float targetForgetThreshold = 5f;

    private void Awake()
    {
        sensors = GetComponent<EnemySensors>();
        movement = GetComponent<EnemyMovement>();
        navigationTarget = GameObject.FindGameObjectWithTag("Objective")?.transform;
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

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
      // if (roll < 0.1f) role = SocialRole.Leader; // 10% de chance de ser líder nato
      // else if (roll < 0.2f) role = SocialRole.LoneWolf; // 10% de lobo solitário
      // else role = SocialRole.Soldier;

        if (role == SocialRole.Leader && currentGroup == null)
        {
            currentGroup = new EnemyGroup(this);
            currentGroup.leader = this;
            currentGroup.members.Add(this);
        }

         //Exemplo: 20% de chance de ser focado no Objetivo
       //if (Random.value < 0.2f) focus = EnemyFocus.ObjectiveRooted;
       // else focus = EnemyFocus.PlayerSlayer;

        speedMultiplier = Random.Range(0.9f, 1.2f);

        EnemyManager.Instance.RegisterEnemy(this);
    }


    private void Update()
    {
        if (isDead) return;

        movement.MoveTowards(targetDestination, intelligenceLevel < 0.5f, currentGroup?.personality == GroupPersonality.Aggressive);

        UpdateUI();
        
    }

    [Header("Tick Timing")]
    float nextDetection;
    float detectionInterval = 0.3f;

    public void TickAI()
    {

        if (isDead) return;

        UpdateTargetAwareness();
        DetermineNavigationGoal();

        if (Time.time >= nextDetection)
        {
            HandleDetection();
            nextDetection = Time.time + detectionInterval;
        }

        // Se não houver um alvo prioritário de combate, EvaluateTarget decide o que fazer
        EvaluateTarget();

        UpdateDecision(); // Define o targetDestination final
        CheckGroupCohesion(); // Verifica se desertou ou precisa de Regroup
        HandleGroupLogic();
    }

    private void TryAttack()
    {
        
        if (combatTarget == null || isDead) return;

        // Em vez de centro-a-centro, calculamos a distância até o ponto mais próximo da superfície do alvo
        float dist;
        if (combatTarget.TryGetComponent(out Collider targetCol))
        {
            Vector3 closestPoint = targetCol.ClosestPoint(transform.position);
            dist = Vector3.Distance(transform.position, closestPoint);
        }
        else
        {
            dist = Vector3.Distance(transform.position, combatTarget.position);
        }

        // Agora 1.5f ou 2.0f será muito mais preciso
        if (Time.time >= lastAttackTime + attackCooldown && dist <= attackRange * 1.5f)
        {
            IDamageable damageable = combatTarget.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {

                lastAttackTime = Time.time;
                damageable.Damage(attackDamage);

                if (visualModel)
                    visualModel.transform.DOPunchPosition(transform.forward * 0.5f, 0.2f);
            }
            else
            {
                Debug.Log("Nenhum IDamageable encontrado em " + combatTarget.name);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (isDead) return;

        // Se encostar em algo que seja o player, tenta bater imediatamente
        if (combatTarget)
        {
            TryAttack();
        }
    }
    private void HandleDetection()
    {
        if (isDead) return;

        // MEMÓRIA DE PERSEGUIÇÃO
        if (combatTarget != null)
        {
            float dist = Vector3.Distance(transform.position, combatTarget.position);

            if (dist <= stopChasingRange)
            {
                // continua perseguindo MAS ainda pode trocar por alvo melhor
            }
            else
            {
                combatTarget = null;
                isChasing = false;
            }
        }

        // 1. O Sensor agora retorna uma LISTA de tudo que ele vê (Player, NPCs, Objetivos)
        List<Transform> visibleTargets = sensors.GetAllVisibleTargets();

        Transform bestTarget = null;
        float highestScore = -1000f;

        foreach (Transform t in visibleTargets)
        {
            if (ShouldIgnoreTarget(t))
                continue;

            float score = CalculatePriority(t);

            if (score > highestScore)
            {
                highestScore = score;
                bestTarget = t;
            }
        }

        if (bestTarget != null)
        {
            float newScore = CalculatePriority(bestTarget);

            if (combatTarget == null || newScore > CalculatePriority(combatTarget))
            {
                bool targetChanged = combatTarget != bestTarget;

                combatTarget = bestTarget;
                isChasing = true;

                if (targetChanged && currentGroup != null)
                {
                    currentGroup.AlertGroup(combatTarget);
                }
            }
        }
    }

    void EvaluateTarget()
    {
        if (forcedAggroTimer > 0)
        {
            forcedAggroTimer -= EnemyManager.Instance.tickInterval;
            return;
        }

        Transform potentialTarget = null;

        // 1️ & 2️ Foco Pessoal / Alvo Atual (Se ainda válido e perto)
        if (combatTarget != null)
        {
            float currentScore = CalculatePriority(combatTarget);
            Transform best = FindBestTarget();

            if (best != null)
            {
                float newScore = CalculatePriority(best);

                if (newScore > currentScore + 50f)
                {
                    combatTarget = best;
                }
            }

            return;
        }

        // 3️ Intenção do Líder (Se eu não estiver "ocupado")
        if (currentGroup != null && currentGroup.leader != null)
        {
            GroupIntent intent = currentGroup.currentIntent;

            // Se a intenção do grupo for atacar o Player e eu estiver livre
            if (intent == GroupIntent.AttackPlayer)
            {
                potentialTarget = currentGroup.leader.combatTarget;
            }
        }

        // 4️ Informação Ouvida (Sensores)
        if (potentialTarget == null)
        {
            potentialTarget = FindBestTarget();
        }

        // 5️ NavigationTarget (Core/Marching)
        if (potentialTarget == null)
        {
            // Lógica de marchar para o objetivo (Ignora player se longe do core)
            DetermineNavigationGoal();
        }

        combatTarget = potentialTarget;
    }

    private void UpdateDecision()
    {
        // Apenas decidimos o Vector3 targetDestination aqui
        if (combatTarget != null)
        {
            IDamageable attackable = combatTarget.GetComponentInParent<IDamageable>();
            if (attackable != null)
            {
                Transform atkPoint = attackable.GetAttackPoint();
                targetDestination = atkPoint != null ? atkPoint.position : combatTarget.position;
            }
            isChasing = true;
        }
        else if (role == SocialRole.Soldier && currentGroup != null)
        {
            // ObjetiveRooted ou Guardian ignoram slots de grupo
            if (focus != EnemyFocus.ObjectiveRooted && focus != EnemyFocus.Guardian)
            {
                targetDestination = GetCurrentSlot();
                isChasing = true;
            }
            else
            {
                // Segue o próprio objetivo
                targetDestination = navigationTarget != null ? navigationTarget.position : transform.position;
                isChasing = true;
            }
        }
        else
        {
            targetDestination = navigationTarget != null ? navigationTarget.position : transform.position;
        }

        // Checagem de ataque também pode ficar no Tick ou em um timer separado
        float distToTarget = Vector3.Distance(transform.position, targetDestination);
        if (combatTarget != null && distToTarget <= attackRange * 1.2f)
        {
            TryAttack();
        }
    }

    void CheckGroupCohesion()
    {
        // Inimigos ObjectiveRooted nunca abandonam o objetivo nem se tornam desertores
        if (focus == EnemyFocus.ObjectiveRooted) return;

        if (currentGroup == null || currentGroup.leader == null || isDead) return;

        float distToLeader = Vector3.Distance(transform.position, currentGroup.leader.transform.position);

        // Se ultrapassou o limite, vira Desertor
        if (distToLeader > groupLeashDistance && combatTarget == null)
        {
            BecomeDeserter();
        }

        // Lógica de Regroup baseada em Personalidade
        if (isDeserter && personality == EnemyPersonality.Disciplined)
        {
            // Disciplinados tentam voltar se estiverem longe demais
            if (distToLeader > groupLeashDistance * 1.5f)
            {
                state = EnemyState.Regroup;

                // Apenas volta para o slot se não for ObjectiveRooted
                if (focus != EnemyFocus.ObjectiveRooted)
                    MoveToFormationSlot();
            }
        }
    }

    void BecomeDeserter()
    {
        isDeserter = true;
        // Opcional: Feedback visual ou log
    }

    void DetermineNavigationGoal()
    {
        float distToCore = Vector3.Distance(transform.position, navigationTarget.position);

        // Se estiver muito longe do Core (ex: acabou de dar spawn)
        if (distToCore > 50f)
        {
            // Força a marcha, ignorando distrações menores
            targetDestination = navigationTarget.position;
            sensors.sensorInterval = 1.0f; // Diminui a frequência de busca por players
        }
        else
        {
            sensors.sensorInterval = 0.2f; // Perto do core, fica alerta
        }
    }

    void MoveToFormationSlot()
    {
        if (currentGroup != null)
        {
            targetDestination = GetCurrentSlot();
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

        if (player != null && focus != EnemyFocus.ObjectiveRooted)
        {
            combatTarget = player;
            isChasing = true;
            forcedAggroTimer = 3f; // 3 segundos de foco forçado
            OnDetectedPlayer();
        }

        if (currentGroup != null && combatTarget != null)
        {
            currentGroup.AlertGroup(combatTarget);
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

        EnemyManager.Instance.UnregisterEnemy(this);
        Destroy(gameObject, 3f);
        WaveManager.Instance.currentEnemiesAlive--; // Decrementa o contador de inimigos vivos na WaveManager
        scoreData.currentScore++; // Incrementa a pontuação do jogador


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
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.UnregisterEnemy(this);
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

    }

    public void JoinGroup(EnemyGroup group)
    {
        if (role == SocialRole.LoneWolf) return;

        if (socialBehavior == SocialBehavior.Rebellious && group.members.Count > 3)
        {
            return;
        }

        if (!group.allowNewMembers)
            return;

        currentGroup = group;

        if (!currentGroup.members.Contains(this))
            currentGroup.members.Add(this);

    }

    public void LeaveGroup()
    {
        currentGroup?.members.Remove(this);
        currentGroup = null;

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
        }
    }

    private Transform FindBestTarget()
    {
        List<Transform> potentialTargets = sensors.GetAllVisibleTargets();

        // Se o array vier vazio, o problema é a TargetMask ou falta de Collider no Core
        if (potentialTargets.Count == 0)
        {
            return null;
        }

        Transform bestTarget = null;
        float highestPriority = -1000f;

        foreach (var col in potentialTargets)
        {
            // Ignora a si mesmo para não tentar atacar o próprio corpo
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;

            float priority = CalculatePriority(col.transform);


            if (priority > highestPriority)
            {
                highestPriority = priority;
                bestTarget = col.transform;
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
            if (focus == EnemyFocus.PlayerSlayer) score += 100;

            // ⭐ REGRA NOVA: Player muito perto força aggro
            if (focus == EnemyFocus.PlayerSlayer && dist < aggroOverrideDistance)
                score += 1000;
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

    public void ReceiveGroupAlert(Transform target)
    {

        if (isDead) return;

        // ObjectiveRooted ignora qualquer alerta que não seja o Objective
        if (focus == EnemyFocus.ObjectiveRooted && !target.CompareTag("Objective"))
            return;
        if (focus == EnemyFocus.Guardian && !target.CompareTag("Defense"))
            return;



        // Se já estou em combate, não abandono
        if (combatTarget != null)
        {
            float current = CalculatePriority(combatTarget);
            float incoming = CalculatePriority(target);

            if (incoming <= current)
                return;
        }

        // Se meu foco ignora esse alvo, não faço nada
        if (ShouldIgnoreTarget(target)) return;

        float score = CalculatePriority(target);

        // Só aceita se for algo relevante
        if (score > 50f)
        {
            combatTarget = target;
            isChasing = true;
        }
    }

    bool ShouldIgnoreTarget(Transform target)
    {
        if (focus == EnemyFocus.ObjectiveRooted && !target.CompareTag("Objective"))
            return true;

        if (focus == EnemyFocus.Guardian && !target.CompareTag("Defense"))
            return true;

        if (focus == EnemyFocus.Hunter && !target.CompareTag("NPC"))
            return true;

        return false;
    }

    private void UpdateTargetAwareness()
    {
        if (combatTarget == null) return;

        // Verifica se o alvo atual está na lista de alvos visíveis dos sensores
        bool targetIsVisible = sensors.GetAllVisibleTargets().Contains(combatTarget);

        if (!targetIsVisible)
        {
            // O alvo sumiu! Começa a contar o tempo
            targetLostTimer += EnemyManager.Instance.tickInterval; // Usa o intervalo do Manager

            if (targetLostTimer >= targetForgetThreshold)
            {
                ForgetTarget();
            }
        }
        else
        {
            // Alvo visível, reseta o timer
            targetLostTimer = 0;
        }
    }

    private void ForgetTarget()
    {
        combatTarget = null;
        isChasing = false;
        targetLostTimer = 0;
        state = EnemyState.Idle;

        // Atualiza o destino para o navigationTarget
        if (navigationTarget != null)
            targetDestination = navigationTarget.position;
        else
            targetDestination = transform.position;

    }


    public Transform attackPoint;

    public Transform GetAttackPoint()
    {
        return attackPoint != null ? attackPoint : transform;
    }

}

[System.Serializable]
public class LootDrop
{
    public GameObject itemPrefab;
    [Range(0, 100)] public float dropChance; // Chance específica deste item cair
}
