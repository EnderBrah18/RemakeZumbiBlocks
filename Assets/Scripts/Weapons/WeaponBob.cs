using UnityEngine;

public class WeaponBob : MonoBehaviour
{
    [Header("Configurações de Bobbing")]
    public float walkingBobSpeed = 14f;
    public float bobAmount = 0.05f;
    public float leanAmount = 0.02f; // Balanço lateral

    [Header("Referências")]
    public CharacterController playerController;

    private float timer = 0;
    private Vector3 initialPosition;

    void Start()
    {
        initialPosition = transform.localPosition;
    }

    void Update()
    {
        // 1. Verificar a velocidade do jogador no chão
        float speed = new Vector3(playerController.velocity.x, 0, playerController.velocity.z).magnitude;

        if (speed > 0.1f && playerController.isGrounded)
        {
            // 2. O timer avança baseado na velocidade
            timer += Time.deltaTime * walkingBobSpeed;

            // 3. Cálculo do movimento usando Seno e Cosseno para criar um "8" ou círculo
            float posX = Mathf.Cos(timer * 0.5f) * bobAmount * leanAmount; // Balanço lateral suave
            float posY = Mathf.Sin(timer) * bobAmount; // Sobe e desce

            Vector3 targetPos = new Vector3(posX, posY, 0) + initialPosition;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * 10f);
        }
        else
        {
            // 4. Se parado, volta suavemente para a posição inicial
            timer = 0;
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialPosition, Time.deltaTime * 10f);
        }
    }
}
