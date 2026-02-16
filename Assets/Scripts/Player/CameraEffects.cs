using DG.Tweening;
using UnityEngine;
using UnityEngine.LowLevel;

public class CameraEffects : MonoBehaviour
{
    [Header("Referências")]
    public CharacterController controller; // Para saber se estamos andando/correndo
    private Vector2 moveInput;

    [Header("Balanço ao Caminhar (Bobbing)")]
    public float walkBobSpeed = 14f;
    public float walkBobAmount = 0.05f;
    public float runBobSpeed = 18f;
    public float runBobAmount = 0.1f;

    [Header("Efeito de Pulo/Queda")]
    public float landShakeDuration = 0.2f;
    public float landShakeForce = 0.15f;

    [Header("Tilt Settings")]
    public float tiltAmount = 2f;
    public float tiltSpeed = 5f;

    private float timer = 0;
    private float defaultPosY;
    private bool wasGrounded;

    void Start()
    {
        defaultPosY = transform.localPosition.y;
    }

    void Update()
    {
        HandleHeadBob();
        HandleLandingEffect();
        HandleTilt();
    }
    private void HandleTilt()
    {
        // moveInput.x é o valor de A (-1) e D (1)
        float targetTilt = -moveInput.x * tiltAmount;

        // Aplica a rotação Z de forma suave
        Quaternion targetRotation = Quaternion.Euler(transform.localEulerAngles.x, transform.localEulerAngles.y, targetTilt);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * tiltSpeed);
    }

    private void HandleHeadBob()
    {
        // Só balança se estiver no chão e se movendo
        float speed = new Vector2(controller.velocity.x, controller.velocity.z).magnitude;

        if (speed > 0.1f && controller.isGrounded)
        {
            // Determina se usa valores de corrida ou caminhada (ex: se vel > 6)
            float currentSpeed = speed > 6f ? runBobSpeed : walkBobSpeed;
            float currentAmount = speed > 6f ? runBobAmount : walkBobAmount;

            timer += Time.deltaTime * currentSpeed;

            // Cálculo do Seno para movimento de sobe e desce
            float newY = defaultPosY + Mathf.Sin(timer) * currentAmount;
            transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
        }
        else
        {
            // Volta para a posição original suavemente quando para
            timer = 0;
            transform.localPosition = Vector3.Lerp(transform.localPosition,
                new Vector3(transform.localPosition.x, defaultPosY, transform.localPosition.z),
                Time.deltaTime * 8f);
        }
    }

    private void HandleLandingEffect()
    {
        // Detecta o exato momento em que o player toca o chão após um pulo
        if (!wasGrounded && controller.isGrounded)
        {
            ApplyLandingShake();
        }
        wasGrounded = controller.isGrounded;
    }

    private void ApplyLandingShake()
    {
        // Pequeno impacto visual ao cair
        transform.localPosition += new Vector3(0, -landShakeForce, 0);
    }
}
