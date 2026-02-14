using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [Header("Referências")]
    public Transform head;
    public Transform body;

    [Header("Configuração")]
    public float sensitivity = 120f;
    public float maxLookAngle = 85f;

    private PlayerInputActions inputActions;
    private Vector2 lookInput;

    private float xRotation = 0f;

    void Awake()
    {
        inputActions = new PlayerInputActions();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable()
    {
        inputActions.Enable();

        inputActions.Player.Look.performed += ctx =>
            lookInput = ctx.ReadValue<Vector2>();

        inputActions.Player.Look.canceled += _ =>
            lookInput = Vector2.zero;
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {
        HandleLook();
    }

    void HandleLook()
    {
        float mouseX = lookInput.x * sensitivity * Time.deltaTime;
        float mouseY = lookInput.y * sensitivity * Time.deltaTime;

        // Rotação vertical (cabeça)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        head.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotação horizontal (corpo inteiro)
        body.Rotate(Vector3.up * mouseX);
    }
}
