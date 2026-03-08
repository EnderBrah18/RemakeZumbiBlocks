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
    public float separationRadius = 2.0f;
    public float separationWeight = 1.5f;
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

    public void MoveTowards(Vector3 targetPosition, bool ignoreGroundCheck = false)
    {
        if (rb == null) return;

        bool isGrounded = sensors.IsGrounded();
        float heightDiff = transform.position.y - targetPosition.y;

        // --- NOVIDADE: SE JOGAR DA BORDA ---
        // Se ele não tem chão mas o player está abaixo, ou se ele já está no ar,
        // ele NÃO deve congelar, deve manter a velocidade para frente.
        if (!isGrounded)
        {
            // Mantém a velocidade horizontal atual (inércia) para ele cair descrevendo um arco
            Vector3 airVel = rb.linearVelocity;
            airVel.y = rb.linearVelocity.y; // Mantém gravidade
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, airVel, Time.fixedDeltaTime);
            return;
        }

        // Se o player está abaixo, ele ignora a cautela com a borda
        bool shouldLeap = heightDiff > 1.5f;

        Vector3 chosenDir;
        if (shouldLeap || ignoreGroundCheck)
        {
            chosenDir = (targetPosition - transform.position).normalized;
            chosenDir.y = 0;
        }
        else
        {
            Vector3 targetPos = targetPosition;
            if (self.role != SocialRole.LoneWolf && self.currentGroup != null)
            {
                Vector3 slot = self.GetCurrentSlot();
                if (Vector3.Distance(transform.position, slot) > 1.2f) targetPos = slot;
            }

            chosenDir = CalculateBestDirection(targetPos);

            // Só aplica a repulsão se não estiver tentando pular
            chosenDir = ApplyEdgeRepulsion(chosenDir);
        }

        // Se ele está tentando ir para o player mas o sensor diz que é abismo
        // e ele DEVE pular, forçamos a direção mesmo sem chão.
        if (chosenDir == Vector3.zero && shouldLeap)
        {
            chosenDir = (targetPosition - transform.position).normalized;
            chosenDir.y = 0;
        }

        Vector3 separation = CalculateSeparation();
        Vector3 finalDir = (chosenDir + separation * separationWeight).normalized;

        // Se mesmo após tudo ele não tem direção e não deve pular, aí sim ele para
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

        // Se for pular, dá um pequeno bônus de velocidade para garantir que saia da quina
        if (leaping) targetSpeed *= 1.2f;

        Vector3 targetVel = moveDir * targetSpeed;
        targetVel.y = rb.linearVelocity.y;

        // Se estiver pulando, a resposta é imediata para não "escorregar" e parar
        float smooth = leaping ? 20f : separationSmooth;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVel, smooth * Time.fixedDeltaTime);

        // Rotação estável
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
        Collider[] colleagues = Physics.OverlapSphere(transform.position, separationRadius, LayerMask.GetMask("Enemy"));
        foreach (var col in colleagues)
        {
            if (col.gameObject == gameObject) continue;
            Vector3 diff = transform.position - col.transform.position;
            float dist = diff.magnitude;
            if (dist < 0.1f) continue;
            separation += diff.normalized * ((separationRadius - dist) / separationRadius);
        }
        return separation;
    }
}