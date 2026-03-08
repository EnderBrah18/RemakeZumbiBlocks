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

    [Header("Sensores de Borda (Context Steering)")]
    public int detectorCount = 12;
    public float detectionRadius = 1.2f;

    [Header("Social AI")]
    public float separationRadius = 1.8f;
    public float separationWeight = 2.5f; 
    public float separationSmooth = 10f;

    private void Awake()
    {
        sensors = GetComponent<EnemySensors>();
        rb = GetComponent<Rigidbody>();
        self = GetComponent<Enemy>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) targetPlayer = p.transform;
    }

    public void MoveTowards(Vector3 targetPosition, bool ignoreGroundCheck = false, bool forceDirect = false)
    {
        if (rb == null) return;

        bool isGrounded = sensors.IsGrounded();
        float heightDiff = transform.position.y - targetPosition.y;

        // 1. COMPORTAMENTO EM VOO
        if (!isGrounded)
        {
            Vector3 airVel = rb.linearVelocity;
            airVel.y = rb.linearVelocity.y;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, airVel, Time.fixedDeltaTime);
            return;
        }

        bool shouldLeap = heightDiff > 1.5f;
        Vector3 chosenDir = Vector3.zero;

        if (shouldLeap || ignoreGroundCheck)
        {
            chosenDir = (targetPosition - transform.position).normalized;
            chosenDir.y = 0;
        }
        else
        {
            Vector3 targetPos = targetPosition;
            // Se estiver em grupo, tenta ir para o slot, mas com uma "folga"
            if (self.role != SocialRole.LoneWolf && self.currentGroup != null)
            {
                Vector3 slot = self.GetCurrentSlot();
                // Só foca no slot se estiver longe dele, senão foca no player para evitar micro-ajustes
                if (Vector3.Distance(transform.position, slot) > 0.8f) targetPos = slot;
            }

            chosenDir = CalculateBestDirection(targetPos);
            chosenDir = ApplyEdgeRepulsion(chosenDir);
        }


        // Se for agressivo (forceDirect) ou estiver pulando, ele ignora o CalculateBestDirection
        if (shouldLeap || ignoreGroundCheck || forceDirect)
        {
            chosenDir = (targetPosition - transform.position).normalized;
            chosenDir.y = 0;

            // Se ele não for burro (ignoreGroundCheck), ainda aplicamos a repulsão de borda 
            // apenas para ele não cair "sem querer", mas ele não vai mais "flanquear"
            if (!ignoreGroundCheck) chosenDir = ApplyEdgeRepulsion(chosenDir);
        }
        else
        {
            // Aqui é onde os táticos calculam o melhor caminho (causando o flanqueio)
            chosenDir = CalculateBestDirection(targetPosition);
            chosenDir = ApplyEdgeRepulsion(chosenDir);
        }

        // 2. LÓGICA SOCIAL MELHORADA
        Vector3 separation = CalculateSeparation();
        Vector3 finalDir = chosenDir;

        if (separation != Vector3.zero)
        {
            // Combinamos a direção desejada com a separação ANTES de normalizar
            // Isso dá peso real à fuga de outros inimigos
            Vector3 combined = (chosenDir + separation * separationWeight).normalized;

            // Veto de segurança: Não se empurrem para o abismo
            if (sensors.HasGroundAhead(combined))
                finalDir = combined;
            else
                finalDir = chosenDir;
        }

        if (chosenDir == Vector3.zero && !shouldLeap) finalDir = Vector3.zero;

        ApplyMovement(finalDir, targetPosition, shouldLeap);
    }

    private Vector3 CalculateBestDirection(Vector3 targetPos)
    {
        Vector3 desiredDir = (targetPos - transform.position).normalized;
        desiredDir.y = 0;

        float bestScore = -1f;
        Vector3 bestDir = Vector3.zero;

        for (int i = 0; i < detectorCount; i++)
        {
            float angle = i * (360f / detectorCount);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            if (sensors.HasGroundAhead(dir))
            {
                float score = Vector3.Dot(dir, desiredDir);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = dir;
                }
            }
        }
        return bestDir;
    }

    private Vector3 ApplyEdgeRepulsion(Vector3 moveDir)
    {
        if (moveDir == Vector3.zero) return Vector3.zero;

        // 1. Definição do "espaço do corpo" (ajuste conforme a largura do seu inimigo)
        float bodyWidth = 0.4f;
        Vector3 rightEdge = transform.right * bodyWidth;
        Vector3 leftEdge = -transform.right * bodyWidth;

        // 2. CHECAGEM DE EMERGÊNCIA (Os pés estão no chão?)
        bool footLeft = sensors.IsFootGrounded(leftEdge);
        bool footRight = sensors.IsFootGrounded(rightEdge);

        // Se o pé esquerdo saiu, empurra IMEDIATAMENTE para a direita, 
        // ignorando parte do moveDir original para tirar ele da quina.
        if (!footLeft)
        {
            return (moveDir + transform.right * 2f).normalized;
        }

        if (!footRight)
        {
            return (moveDir - transform.right * 2f).normalized;
        }

        // 3. CHECAGEM PREVENTIVA (O que você já tinha, mas com vetores estáveis)
        Vector3 moveRight = Vector3.Cross(Vector3.up, moveDir).normalized;
        Vector3 leftCheck = Quaternion.Euler(0, -30f, 0) * moveDir;
        Vector3 rightCheck = Quaternion.Euler(0, 30f, 0) * moveDir;

        if (!sensors.HasGroundAhead(leftCheck)) moveDir += moveRight * 0.5f;
        if (!sensors.HasGroundAhead(rightCheck)) moveDir -= moveRight * 0.5f;

        return moveDir.normalized;
    }

    private void ApplyMovement(Vector3 moveDir, Vector3 targetPos, bool leaping)
    {
        float targetSpeed = (moveDir == Vector3.zero) ? 0 : moveSpeed * self.speedMultiplier;

        // Se estiver muito perto do player, reduz a velocidade para não "atropelar" e tremer
        float distToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
        if (distToPlayer < self.attackRange * 0.8f && !leaping) targetSpeed *= 0.5f;

        if (leaping) targetSpeed *= 1.2f;

        Vector3 targetVel = moveDir * targetSpeed;
        targetVel.y = rb.linearVelocity.y;

        // Reduzimos o Smooth se estiverem muito perto uns dos outros para evitar o efeito "mola"
        float smooth = leaping ? 20f : separationSmooth;

        // Se a velocidade for muito baixa, zeramos para evitar micro-movimentos (tremidas)
        if (targetVel.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
        else
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVel, smooth * Time.fixedDeltaTime);
        }

        // Rotação: Sempre focar no player ao atacar, mesmo parado
        Vector3 lookDir = (targetPlayer.position - transform.position);
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    private Vector3 CalculateSeparation()
    {
        Vector3 separation = Vector3.zero;
        // LayerMask para otimização
        int enemyLayer = LayerMask.GetMask("Enemy");
        Collider[] colleagues = Physics.OverlapSphere(transform.position, separationRadius, enemyLayer);

        foreach (var col in colleagues)
        {
            if (col.gameObject == gameObject) continue;

            Vector3 diff = transform.position - col.transform.position;
            float dist = diff.magnitude;

            if (dist < 0.05f) continue;

            // Curva de força: Quase zero na borda do raio, máxima no contato
            // (1 - (dist/radius))^2 cria uma curva suave que evita trepidação
            float strength = Mathf.Clamp01(1.0f - (dist / separationRadius));
            strength = strength * strength;

            separation += diff.normalized * strength;
        }

        return Vector3.ClampMagnitude(separation, 1.0f);
    }
}