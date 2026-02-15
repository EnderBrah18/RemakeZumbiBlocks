using UnityEngine;
using DG.Tweening; // Importante!
using UnityEngine.InputSystem;

public class WeaponSway : MonoBehaviour
{
    [Header("Settings")]
    public float swayAmount = 0.02f;
    public float maxSway = 0.06f;
    public float swaySmoothness = 0.2f;

    private Vector3 initialPosition;
    private PlayerInputActions inputActions;
    private Vector2 lookInput;

    void Awake()
    {
        inputActions = new PlayerInputActions();
        initialPosition = transform.localPosition;
    }

    void OnEnable()
    {
        inputActions.Enable();
        // Conecta ao mesmo input de 'Look' que o PlayerLook usa
        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += _ => lookInput = Vector2.zero;
    }

    void OnDisable() => inputActions.Disable();

    void Update()
    {
        // Substituindo o GetAxis antigo pelo valor do Input System
        float moveX = lookInput.x * swayAmount;
        float moveY = lookInput.y * swayAmount;

        moveX = Mathf.Clamp(moveX, -maxSway, maxSway);
        moveY = Mathf.Clamp(moveY, -maxSway, maxSway);

        Vector3 targetPos = new Vector3(moveX, moveY, 0);
        transform.DOLocalMove(initialPosition + targetPos, swaySmoothness);
    }
}
