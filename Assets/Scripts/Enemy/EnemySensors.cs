using UnityEngine;

public class EnemySensors : MonoBehaviour
{
    [Header("Configurações de Visão")]
    public float viewDistance = 15f;
    public float viewAngle = 90f;
    public LayerMask obstacleMask; // O que bloqueia a visão (Paredes)
    public LayerMask targetMask;   // O que ele procura (Player, NPCs)

    [Header("Configurações de Chão")]
    public float groundCheckDist = 1.5f;
    public LayerMask groundMask;

    // Retorna o objeto detectado se estiver no campo de visão e sem obstáculos
    public Transform CheckVisualDetection()
    {
        // Encontra potenciais alvos em um raio
        Collider[] targetsInRadius = Physics.OverlapSphere(transform.position, viewDistance, targetMask);

        foreach (var target in targetsInRadius)
        {
            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;

            // 1. Verifica o ângulo de visão (FOV)
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float distToTarget = Vector3.Distance(transform.position, target.transform.position);

                // 2. Dispara Raycast para garantir que não há paredes no caminho
                if (!Physics.Raycast(transform.position + Vector3.up, dirToTarget, distToTarget, obstacleMask))
                {
                    return target.transform; // Viu o alvo!
                }
            }
        }
        return null;
    }

    // Verifica se há chão à frente para decidir se deve "dropar" ou parar
    public bool HasGroundAhead(Vector3 direction)
    {
        Vector3 origin =
            transform.position +
            direction.normalized * 0.5f +
            Vector3.up * 0.5f;

        bool groundAhead = Physics.Raycast(origin, Vector3.down, groundCheckDist + 0.5f, groundMask);

        Debug.DrawRay(origin, Vector3.down * (groundCheckDist + 0.5f),
            groundAhead ? Color.green : Color.red);

        return groundAhead;
    }

    public bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;

        bool grounded = Physics.SphereCast(
    transform.position + Vector3.up * 0.3f,
    0.3f,
    Vector3.down,
    out RaycastHit hit,
    groundCheckDist,
    groundMask
);

        Debug.DrawRay(origin, Vector3.down * groundCheckDist, grounded ? Color.green : Color.red);

        return grounded;
    }
}
