using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private EnemySensors sensors;
    private Rigidbody rb;
    private Enemy self;

    [Header("Configurações de Movimento")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 15f;

    [Header("Sensores de Borda (Context Steering)")]
    public int detectorCount = 16;
    public float detectionRadius = 1.2f;

    [Header("Social AI")]
    public float separationRadius = 1.8f;
    public float separationWeight = 2.5f;
    public float separationSmooth = 10f;
    Vector3 separationVelocity;
    Vector3 lastSeparation;


    private void Awake()
    {
        sensors = GetComponent<EnemySensors>();
        rb = GetComponent<Rigidbody>();
        self = GetComponent<Enemy>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

    }

    public void MoveTowards(Vector3 targetPosition, bool ignoreGroundCheck = false, bool forceDirect = false)
    {
        if (rb == null) return;

        bool isGrounded = sensors.IsGrounded();
        float heightDiff = transform.position.y - targetPosition.y;

        if (!isGrounded)
        {
            Vector3 airVel = rb.linearVelocity;
            airVel.y = rb.linearVelocity.y;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, airVel, Time.fixedDeltaTime);
            return;
        }

        bool shouldLeap = heightDiff > 1.5f;
        Vector3 chosenDir = Vector3.zero;

        if (shouldLeap || ignoreGroundCheck || forceDirect)
        {
            chosenDir = (targetPosition - transform.position).normalized;
            chosenDir.y = 0;
            if (!ignoreGroundCheck) chosenDir = ApplyEdgeRepulsion(chosenDir);
        }
        else
        {
            Vector3 targetPos = targetPosition;
            if (self.role != SocialRole.LoneWolf && self.currentGroup != null)
            {
                Vector3 slot = self.GetCurrentSlot();
                if (Vector3.Distance(transform.position, slot) > 0.8f) targetPos = slot;
            }

            chosenDir = CalculateBestDirection(targetPos);
            chosenDir = ApplyEdgeRepulsion(chosenDir);
        }

        Vector3 separation = Vector3.SmoothDamp(
    lastSeparation,
    CalculateSeparation(),
    ref separationVelocity,
    0.15f
);

        lastSeparation = separation;
        if (separation.magnitude < 0.1f)
            separation = Vector3.zero;


        Vector3 finalDir = chosenDir;

        if (separation != Vector3.zero)
        {
            Vector3 combined = (chosenDir + separation * separationWeight).normalized;
            if (sensors.HasGroundAhead(combined))
                finalDir = combined;
            else
                finalDir = chosenDir;
        }

        if (chosenDir == Vector3.zero && !shouldLeap) finalDir = Vector3.zero;

        // Chamas o ApplyMovement passando a posição do alvo atual
        ApplyMovement(finalDir, targetPosition, shouldLeap);
    }

    private void ApplyMovement(Vector3 moveDir, Vector3 targetPos, bool leaping)
    {
        float targetSpeed = (moveDir == Vector3.zero) ? 0 : moveSpeed * self.speedMultiplier;

        // 1. DISTÂNCIA DO ALVO ATUAL (Usa targetPos em vez de targetPlayer)
        float distToTarget = Vector3.Distance(transform.position, targetPos);



        // Se estiver muito perto do alvo (Core ou Player), reduz a velocidade
        if (distToTarget < self.attackRange * 0.8f && !leaping) targetSpeed *= 0.5f;

        if (leaping) targetSpeed *= 1.2f;

        Vector3 targetVel = moveDir * targetSpeed;
        targetVel.y = rb.linearVelocity.y;

        float smooth = leaping ? 20f : separationSmooth;



        if (targetVel.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
        else
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVel, smooth * Time.fixedDeltaTime);
        }

        // 2. ROTAÇÃO: Foca na posição do alvo (targetPos)
        Vector3 lookDir = (targetPos - transform.position);
        lookDir.y = 0;


        if (lookDir.sqrMagnitude > 0.01f && distToTarget > 0.5f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }

    // --- Mantenha os métodos CalculateBestDirection, ApplyEdgeRepulsion e CalculateSeparation como estão ---
    // (Apenas certifique-se de que eles não usem a variável targetPlayer)

    private Vector3 CalculateBestDirection(Vector3 targetPos)
    {
        Vector3 desiredDir = (targetPos - transform.position).normalized;
        desiredDir.y = 0;

        float bestScore = -1f;
        Vector3 bestDir = Vector3.zero;

        for (int i = 0; i < detectorCount; i++)
        {
            float angle = i * (360f / detectorCount);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;

            // Checa chão e obstáculos
            if (sensors.HasGroundAhead(dir) && !sensors.HasObstacleAhead(dir, detectionRadius))
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
        float bodyWidth = 0.4f;
        Vector3 rightEdge = transform.right * bodyWidth;
        Vector3 leftEdge = -transform.right * bodyWidth;
        if (!sensors.IsFootGrounded(leftEdge)) return (moveDir + transform.right * 2f).normalized;
        if (!sensors.IsFootGrounded(rightEdge)) return (moveDir - transform.right * 2f).normalized;
        Vector3 moveRight = Vector3.Cross(Vector3.up, moveDir).normalized;
        Vector3 leftCheck = Quaternion.Euler(0, -30f, 0) * moveDir;
        Vector3 rightCheck = Quaternion.Euler(0, 30f, 0) * moveDir;
        if (!sensors.HasGroundAhead(leftCheck)) moveDir += moveRight * 0.5f;
        if (!sensors.HasGroundAhead(rightCheck)) moveDir -= moveRight * 0.5f;
        return moveDir.normalized;
    }

    private Vector3 CalculateSeparation()
    {
        Vector3 separation = Vector3.zero;
        int enemyLayer = LayerMask.GetMask("Enemy");
        Collider[] colleagues = Physics.OverlapSphere(transform.position, separationRadius, enemyLayer);
        foreach (var col in colleagues)
        {
            if (col.gameObject == gameObject) continue;
            Vector3 diff = transform.position - col.transform.position;
            float dist = diff.magnitude;
            if (dist < 0.05f) continue;
            float strength = Mathf.Clamp01(1.0f - (dist / separationRadius));
            strength = strength * strength;
            separation += diff.normalized * strength;
        }
        return Vector3.ClampMagnitude(separation, 1.0f);
    }
}