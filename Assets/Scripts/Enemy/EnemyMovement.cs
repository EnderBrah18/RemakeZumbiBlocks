using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private EnemySensors sensors;
    private Rigidbody rb;
    private Transform targetPlayer;
    private Enemy self;


    [Header("Configurações de Movimento")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 15f;
    public float stoppingDistance = 1.2f;

    [Header("Social AI (Matilha)")]
    public float separationRadius = 2.0f; // Raio de "espaço pessoal"
    public float separationWeight = 1.2f; // O quanto ele prioriza não bater no colega
    public float maxSeparationForce = 1.2f; // Limita a força máxima de separação
    public float separationSmooth = 8f; // Suaviza a aplicação de velocidade (Lerp)

    private void Awake()
    {
        sensors = GetComponent<EnemySensors>();
        rb = GetComponent<Rigidbody>();
        self = GetComponent<Enemy>();

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.linearDamping = 1f; // Mantive baixo para não tirar momentum
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) targetPlayer = p.transform;
    }

    public void MoveTowards(Vector3 targetPosition, bool ignoreGroundCheck = false)
    {
        Debug.Log("MoveTowards chamado");

        if (rb == null) return;

        Vector3 toTarget = targetPosition - transform.position;
        Vector3 dirToTarget = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.zero;

        bool grounded = sensors.IsGrounded();
        bool groundAhead = sensors.HasGroundAhead((targetPosition - transform.position).normalized);

        if (!groundAhead)
        {
            Vector3 left = Quaternion.Euler(0, -30f, 0) * dirToTarget;
            Vector3 right = Quaternion.Euler(0, 30f, 0) * dirToTarget;

            bool leftGround = sensors.HasGroundAhead(left);
            bool rightGround = sensors.HasGroundAhead(right);

            if (leftGround)
            {
                dirToTarget = left;
            }
            else if (rightGround)
            {
                dirToTarget = right;
            }

            Debug.Log($"GroundAhead: {groundAhead} | LeftGround: {leftGround} | RightGround: {rightGround}");
        }

        // Se está caindo, não controla movimento horizontal
        if (!grounded)
        {
            // Está no ar, deixa a física agir mas não bloqueia a IA
            return; // mantém se quiser, mas normalmente nem precisa
        }

        // Se há chão agora mas não há à frente, parar (evita cair de borda)
        bool avoidEdge = false;

        if (!ignoreGroundCheck && !groundAhead)
        {
            float playerHeight = targetPosition.y;
            float myHeight = transform.position.y;

            if (playerHeight >= myHeight - 0.5f)
            {
                avoidEdge = true;
            }
        }

        // LÓGICA 1: Lobo Solitário ou Sem Grupo
        if (self.role == SocialRole.LoneWolf || self.currentGroup == null)
        {
            Vector3 dir = dirToTarget;

            if (avoidEdge)
            {
                dir = Vector3.zero;
            }

            // --- SEPARAÇÃO ENTRE INIMIGOS ---
            Vector3 separation = CalculateSeparation(); // agora já fornece força proporcional

            // limita a força de separação para evitar empurrões extremos
            separation = Vector3.ClampMagnitude(separation, maxSeparationForce);

            // combina direção principal com separação (separationWeight controla influência)
            Vector3 combined = dir + separation * separationWeight;

            // evita zero vector
            Vector3 finalDir = combined.sqrMagnitude > 0.0001f ? combined.normalized : dir;

            ApplyVelocity(finalDir, targetPosition);
        }

        // LÓGICA 2: Membro de Grupo
        else
        {
            Vector3 slot = self.GetCurrentSlot();

            // Se o inimigo for lento, ignora slot
            if (self.speedMultiplier < 0.9f)
            {
                slot = targetPosition;
            }

            // Separação usando a mesma função (consistente)
            Vector3 separation = CalculateSeparation();
            separation = Vector3.ClampMagnitude(separation, maxSeparationForce);

            // DIREÇÃO PARA SLOT
            Vector3 dirToSlot = (slot - transform.position);
            Vector3 dir = dirToSlot.sqrMagnitude > 0.0001f ? dirToSlot.normalized : (targetPosition - transform.position).normalized;

            if (avoidEdge)
            {
                dir = Vector3.zero;
            }

            Vector3 combined = dir + separation * separationWeight;
            Vector3 finalDir = combined.sqrMagnitude > 0.0001f ? combined.normalized : dir;

            // NOVA LÓGICA ANTI-ORBITA: se estiver quase no slot, aproxima-se do player para atacar
            float distToSlot = Vector3.Distance(transform.position, slot);

            if (distToSlot < 1.0f) // reduzido de 1.2f para resposta mais rápida
            {
                Vector3 dirToPlayer = (targetPosition - transform.position).normalized;
                ApplyVelocity(dirToPlayer, targetPosition);
            }
            else
            {
                ApplyVelocity(finalDir, slot);
            }
        }

        Debug.Log($"Grounded: {grounded} | GroundAhead: {groundAhead}");
    }


    // Método auxiliar para manter o código limpo
    private void ApplyVelocity(Vector3 moveDir, Vector3 lookTarget)
    {
        // Rotação: Sempre encara o Player (mantive o comportamento original)
        Vector3 look = (lookTarget - transform.position);
        look.y = 0;

        if (look != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(look),
                rotationSpeed * Time.deltaTime
            );

        Vector3 vel = moveDir * moveSpeed * self.speedMultiplier;

        vel.y = rb.linearVelocity.y;

        // Suaviza a transição de velocidade para reduzir flicker
        rb.linearVelocity = Vector3.Lerp(
            rb.linearVelocity,
            vel,
            separationSmooth * Time.fixedDeltaTime
        );
    }

    private Vector3 CalculateSeparation()
    {
        Vector3 separation = Vector3.zero;
        int enemyLayer = LayerMask.GetMask("Enemy");

        Collider[] colleagues = Physics.OverlapSphere(transform.position, separationRadius, enemyLayer);

        foreach (var col in colleagues)
        {
            if (col.gameObject == this.gameObject) continue;

            Vector3 diff = transform.position - col.transform.position;
            float dist = diff.magnitude;

            if (dist < 0.0001f) continue;

            if (dist < separationRadius)
            {
                // força proporcional à proximidade (quanto mais perto, maior)
                float strength = (separationRadius - dist) / separationRadius; // 0..1
                separation += diff.normalized * strength;
            }
        }

        // Não normalizamos: queremos que magnitude represente "quão apertado" está o espaço
        return separation;
    }
}