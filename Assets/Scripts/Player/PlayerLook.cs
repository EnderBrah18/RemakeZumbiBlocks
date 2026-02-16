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
    void LateUpdate()
    {
        // Garante que a câmera (Head) esteja exatamente na posição do corpo (Body)
        // Dica: No seu Body, crie um objeto vazio chamado "CameraAnchor" na altura dos olhos
        // e arraste ele para uma nova variável 'anchor' aqui, se quiser mais controle.
        head.position = body.position + new Vector3(0, 0.8f, 0); // Ajuste a altura (0.8f) conforme seu modelo
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
        head.DOComplete();

        // Adicionamos um pequeno valor aleatório no eixo Y e Z (Horizontal e Inclinação)
        float randomSideRecoil = Random.Range(-force * 0.2f, force * 0.2f);

        head.DOPunchRotation(new Vector3(-force, randomSideRecoil, randomSideRecoil), 0.1f, 5, 0.5f);
    }

    public Vector2 GetLookInput()
    {
        return lookInput;
    }
}

