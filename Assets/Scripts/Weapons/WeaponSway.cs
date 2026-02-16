using UnityEngine;
using DG.Tweening; // Importante!
using UnityEngine.InputSystem;

public class WeaponSway : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private PlayerLook playerLook; // Arraste o objeto com o script PlayerLook aqui

    [Header("Configurações de Sway")]
    [SerializeField] private float intensity = 0.5f;
    [SerializeField] private float maxAmount = 0.05f;
    [SerializeField] private float smoothing = 8f;

    private Vector3 initialPosition;

    void Start()
    {
        initialPosition = transform.localPosition;

        // Auto-busca se esquecer de arrastar no Inspector
        if (playerLook == null)
            playerLook = GetComponentInParent<PlayerLook>();
    }

    void Update()
    {
        if (playerLook == null) return;

        // Pegamos o valor já processado pelo New Input System no PlayerLook
        Vector2 lookInput = playerLook.GetLookInput();

        // Calculamos o deslocamento (Note que não usamos mais GetAxis)
        float moveX = Mathf.Clamp(lookInput.x * intensity, -maxAmount, maxAmount);
        float moveY = Mathf.Clamp(lookInput.y * intensity, -maxAmount, maxAmount);

        Vector3 targetPosition = new Vector3(
            initialPosition.x - moveX,
            initialPosition.y - moveY,
            initialPosition.z
        );

        // Aplica o movimento suave
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * smoothing);
    }
}
