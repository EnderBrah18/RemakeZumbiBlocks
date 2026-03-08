using System.Collections.Generic;
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

    public List<Transform> GetAllVisibleTargets()
    {
        List<Transform> found = new List<Transform>();
        Collider[] targetsInRadius = Physics.OverlapSphere(transform.position, viewDistance, targetMask);

        foreach (var col in targetsInRadius)
        {
            Vector3 dirToTarget = (col.transform.position - transform.position).normalized;
            float distToTarget = Vector3.Distance(transform.position, col.transform.position);

            // Verifica FOV e se há paredes
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                if (!Physics.Raycast(transform.position + Vector3.up, dirToTarget, distToTarget, obstacleMask))
                {
                    found.Add(col.transform);
                }
            }
        }
        return found;
    }

    // Verifica se há chão à frente para decidir se deve "dropar" ou parar
    public bool HasGroundAhead(Vector3 direction)
    {
        // Aumente o 0.6f para 1.2f. 
        // Isso faz ele detectar o abismo bem antes, dando margem para ele desviar para o centro.
        Vector3 origin = transform.position + direction.normalized * 1.2f + Vector3.up * 0.5f;

        bool hasGround = Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hit,
            2.5f, // Distância do raio
            groundMask
        );

        Debug.DrawRay(origin, Vector3.down * 2.5f, hasGround ? Color.green : Color.red);
        return hasGround;
    }

    public bool IsGrounded()
    {
        // Check de pé no chão central para evitar flutuação
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f, groundMask);
    }

    public bool IsFootGrounded(Vector3 sideOffset)
    {
        // sideOffset será transform.right * 0.4f ou -transform.right * 0.4f
        Vector3 origin = transform.position + Vector3.up * 0.5f + sideOffset;
        return Physics.Raycast(origin, Vector3.down, 1.2f, groundMask);
    }
}
