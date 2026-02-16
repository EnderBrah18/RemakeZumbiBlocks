using DG.Tweening;
using UnityEngine;
using UnityEngine.LowLevel;

public class CameraEffects : MonoBehaviour
{
    public PlayerLook playerLook; // Arraste o script PlayerLook aqui
    public CharacterController controller;

    [Header("Bobbing")]
    public float walkBobSpeed = 14f;
    public float walkBobAmount = 0.05f;
    public float runBobSpeed = 18f;
    public float runBobAmount = 0.1f;

    [Header("Tilt Settings Custom")]
    public float mouseTiltAmount = 0.5f; // Valor menor para o mouse não enjoar
    public float strafeTiltAmount = 2.0f; // Valor maior para as teclas A e D
    public float tiltSpeed = 5f;

    [Header("Landing Shake Custom")]
    public float landShakeAmount = 0.2f;
    public float landShakeDuration = 0.15f;

    private float timer = 0;
    private bool wasGrounded;

    void Update()
    {
        if (playerLook == null) return;

        HandleHeadBob();
        HandleTilt();
        HandleLandingEffect();
    }

    private void HandleTilt()
    {
        // 1. Tilt pelo Mouse (Olhar lateralmente)
        float mouseX = playerLook.GetLookInput().x;
        float mouseTilt = -mouseX * mouseTiltAmount;

        // 2. Tilt pelo Teclado (A e D) usando o New Input System
        // Pegamos o valor diretamente do PlayerLook que configuramos acima
        float strafeInput = playerLook.GetMoveInput().x;
        float strafeTilt = -strafeInput * strafeTiltAmount;

        // 3. Soma os dois e aplica
        float targetTilt = mouseTilt + strafeTilt;

        playerLook.tiltOffset = Mathf.Lerp(playerLook.tiltOffset, targetTilt, Time.deltaTime * tiltSpeed);
    }

    private void HandleHeadBob()
    {
        float speed = new Vector2(controller.velocity.x, controller.velocity.z).magnitude;

        if (speed > 0.1f && controller.isGrounded)
        {
            float currentSpeed = speed > 6f ? runBobSpeed : walkBobSpeed;
            float currentAmount = speed > 6f ? runBobAmount : walkBobAmount;

            timer += Time.deltaTime * currentSpeed;

            // Enviamos o valor para o PlayerLook em vez de aplicar no transform
            playerLook.bobOffset = new Vector3(0, Mathf.Sin(timer) * currentAmount, 0);
        }
        else
        {
            timer = 0;
            playerLook.bobOffset = Vector3.Lerp(playerLook.bobOffset, Vector3.zero, Time.deltaTime * 8f);
        }
    }

    private void HandleLandingEffect()
    {
        if (!wasGrounded && controller.isGrounded)
        {
            // Shake vertical (eixo Y) ao cair
            playerLook.ApplyPositionShake(new Vector3(0, landShakeAmount, 0), landShakeDuration);
        }
        wasGrounded = controller.isGrounded;
    }
}
