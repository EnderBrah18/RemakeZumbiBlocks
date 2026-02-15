using DG.Tweening;
using UnityEngine;
using UnityEngine.LowLevel;

public class CameraEffects : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Transform head; // O alvo que a Cinemachine segue

    [Header("Head Bob Settings")]
    public float bobAmount = 0.05f;
    public float bobSpeed = 0.2f;

    private Tween bobTween;
    private Vector3 originalHeadPos;

    void Start()
    {
        originalHeadPos = head.localPosition;
    }

    void Update()
    {
        HandleHeadBob();
    }

    void HandleHeadBob()
    {
        float speed = controller.velocity.magnitude;

        // Se estiver no chão e se movendo
        if (controller.isGrounded && speed > 0.1f)
        {
            if (bobTween == null || !bobTween.IsActive())
            {
                // Cria um movimento de "infinito" ou "8" deitado, comum em FPS
                bobTween = head.DOLocalMoveY(originalHeadPos.y + bobAmount, bobSpeed)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);

                // Adiciona um leve balanço horizontal simultâneo
                head.DOLocalMoveX(originalHeadPos.x + (bobAmount * 0.5f), bobSpeed * 2)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }
        else
        {
            // Se parar de andar, volta suavemente para a posição original
            if (bobTween != null)
            {
                head.DOKill(); // Para todos os tweens no objeto head
                bobTween = null;
                head.DOLocalMove(originalHeadPos, 0.3f);
            }
        }
    }
}
