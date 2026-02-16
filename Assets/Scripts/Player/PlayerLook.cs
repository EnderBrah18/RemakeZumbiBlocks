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

    // VARIÁVEIS PARA OS EFEITOS
    [HideInInspector] public Vector3 bobOffset;    // Vem do CameraEffects
    [HideInInspector] public Vector3 shakeOffset;  // Vem do PlayerStats (Dano)
    [HideInInspector] public float tiltOffset;     // Vem do CameraEffects
    private Vector2 moveInput;

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

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += _ => moveInput = Vector2.zero;
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
        head.position = body.position + new Vector3(0, 0.8f, 0) + bobOffset;
    }

    void HandleLook()
    {
        float mouseX = lookInput.x * sensitivity * Time.deltaTime;
        float mouseY = lookInput.y * sensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        // SOMAMOS TUDO AQUI: Rotação do Mouse + Shake + Tilt lateral
        head.localRotation = Quaternion.Euler(xRotation + shakeOffset.x, shakeOffset.y, tiltOffset + shakeOffset.z);
        body.Rotate(Vector3.up * mouseX);
    }

    public void AddRecoil(float force)
    {
        // Podemos usar o DOTween para animar a variável shakeOffset em vez do transform direto
        DOTween.To(() => shakeOffset, x => shakeOffset = x, new Vector3(-force, 0, 0), 0.1f)
               .OnComplete(() => DOTween.To(() => shakeOffset, x => shakeOffset = x, Vector3.zero, 0.2f));
    }

    public Vector2 GetLookInput() => lookInput;
    public Vector2 GetMoveInput()
    {
        return moveInput;
    }

    public void ApplyRotationShake(Vector3 strength, float duration)
    {
        // Interrompe shakes anteriores para não acumular estranhamente
        DOTween.Kill("lookShake");

        // Anima a variável shakeOffset que já usamos no HandleLook
        DOTween.Shake(() => shakeOffset, x => shakeOffset = x, duration, strength)
               .SetId("lookShake");
    }

    public void ApplyPositionShake(Vector3 strength, float duration)
    {
        DOTween.Kill("bobShake");

        // Anima a variável bobOffset (que afeta a posição no LateUpdate)
        DOTween.Shake(() => bobOffset, x => bobOffset = x, duration, strength)
               .SetId("bobShake");
    }
}

