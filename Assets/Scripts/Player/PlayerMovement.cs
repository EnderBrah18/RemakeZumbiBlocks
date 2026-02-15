using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Referências")]
    public Transform head;
    private CharacterController controller;

    [Header("Movimento")]
    public float speed = 5f;
    public float gravity = -20f;

    [Header("Pulo")]
    public float jumpForce = 5f;

    [Header("Sprint")]
    public float sprintMultiplier = 1.6f;
    public bool isSprinting = false;

    [Header("Estamina")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;
    public float staminaRegenRate = 15f;
    public float staminaSprintCost = 20f;
    public float staminaJumpCost = 15f;
    public float staminaMinToSprint = 5f;

    [Header("Movimento no Ar")]
    public float airControlMultiplier = 0.65f;
    public float airAcceleration = 4f;
    public float airDrag = 2f;

    [Header("Ground Check")]
    public float groundFriction = 15f;
    public float groundAcceleration = 12f;
    public float groundedGraceTime = 0.2f;
    private float lastGroundedTime;

    private float vSpeed = 0f;
    private Vector3 horizontalVelocity;

    private Vector2 moveInput;
    private bool jumpPressed;
    private bool sprintHeld;

    private PlayerInputActions inputActions;

    private bool isReallyGrounded => Time.time - lastGroundedTime <= groundedGraceTime;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();
    }

    void OnEnable()
    {
        inputActions.Enable();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += _ => moveInput = Vector2.zero;

        inputActions.Player.Jump.performed += _ => jumpPressed = true;

        inputActions.Player.Sprint.performed += _ => sprintHeld = true;
        inputActions.Player.Sprint.canceled += _ => sprintHeld = false;
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Start()
    {
        currentStamina = maxStamina;
    }

    void Update()
    {
        GroundCheck();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        HandleStamina();
    }

    void GroundCheck()
    {
        if (controller.isGrounded)
        {
            lastGroundedTime = Time.time;

            if (vSpeed < 0)
                vSpeed = -2f;
        }
    }

    void HandleMovement()
    {
        Vector3 inputDir = (head.forward * moveInput.y + head.right * moveInput.x);
        inputDir.y = 0f;
        inputDir.Normalize();

        float targetSpeed = speed;

        isSprinting = sprintHeld && currentStamina > staminaMinToSprint && inputDir.magnitude > 0.1f;

        if (isSprinting)
            targetSpeed *= sprintMultiplier;

        float controlMultiplier = controller.isGrounded ? 1f : airControlMultiplier;

        Vector3 targetVelocity = inputDir * targetSpeed;

        float accel = controller.isGrounded ? groundAcceleration : airAcceleration;

        Vector3 velocityDiff = targetVelocity - horizontalVelocity;

        horizontalVelocity += velocityDiff * accel * controlMultiplier * Time.deltaTime;

        if (controller.isGrounded && inputDir.magnitude < 0.1f)
        {
            horizontalVelocity = Vector3.Lerp(
                horizontalVelocity,
                Vector3.zero,
                groundFriction * Time.deltaTime
            );
        }

        //  AIR DRAG
        if (!controller.isGrounded)
        {
            horizontalVelocity *= (1f - airDrag * Time.deltaTime);
        }

        //  MANTER COLADO NO CHÃO EM RAMPAS
        if (controller.isGrounded && vSpeed < 0)
            vSpeed = -2f;

        Vector3 finalMove = horizontalVelocity;
        finalMove.y = vSpeed;

        controller.Move(finalMove * Time.deltaTime);
    }

    void HandleJump()
    {
        if (jumpPressed && isReallyGrounded && currentStamina >= staminaJumpCost)
        {
            vSpeed = Mathf.Sqrt(jumpForce * -2f * gravity);
            currentStamina -= staminaJumpCost;
            lastGroundedTime = -999f;
        }

        jumpPressed = false;
    }

    void ApplyGravity()
    {
        vSpeed += gravity * Time.deltaTime;
    }

    void HandleStamina()
    {
        if (isSprinting)
        {
            currentStamina -= staminaSprintCost * Time.deltaTime;
            if (currentStamina < 0)
                currentStamina = 0;
        }
        else
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            if (currentStamina > maxStamina)
                currentStamina = maxStamina;
        }
    }
}