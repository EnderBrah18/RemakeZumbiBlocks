using DG.Tweening;
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
    private float xRotation;

    void Awake()
    {
        inputActions = new PlayerInputActions();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += _ => lookInput = Vector2.zero;
    }

    void OnDisable() => inputActions.Disable();

    void Update()
    {
        HandleLook();
    }

    void HandleLook()
    {
        float mouseX = lookInput.x * sensitivity * Time.deltaTime;
        float mouseY = lookInput.y * sensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        // APLICAMOS A ROTAÇÃO:
        // Se a Weapon Camera for filha da Main Camera (que segue a Head), 
        // ela vai seguir isso perfeitamente agora.
        head.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        body.Rotate(Vector3.up * mouseX);
    }

    public void AddRecoil(float force)
    {
        // REMOVEMOS o DOTween que alterava o xRotation diretamente.
        // Em vez disso, vamos dar um "soco" (Punch) na rotação da cabeça.
        // Isso é muito mais limpo e não quebra o limite do Clamp.

        head.DOComplete(); // Para o recoil anterior se estiver atirando rápido
        head.DOPunchRotation(new Vector3(-force, 0, 0), 0.1f, 10, 1);
    }
}

