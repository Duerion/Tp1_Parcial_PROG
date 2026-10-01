using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FirstPersonControllerCC : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float slideSpeed = 12f;
    public float groundAcceleration = 15f;
    
    [Header("Jump & Air Momentum")]
    public float jumpForce = 5f;
    public float gravity = -20f;
    public float fallMultiplier = 1.5f;
    public float airControl = 3f;

    [Header("Wall Run Settings")]
    public float wallRunSpeed = 10f;
    public float wallRunGravity = -2f; 
    public float wallJumpForwardForce = 12f; 
    public float wallJumpUpwardForce = 8f; 

    [Header("Camera & Visuals")]
    public float mouseSensitivity = 10f;
    public float wallRunTilt = 15f;
    public float tiltSpeed = 10f;
    
    [Header("References")]
    public Transform playerCamera;
    public Animator animator;
    public AudioClip stepSound;
    public float stepInterval = 0.5f;

    private CharacterController controller;
    private AudioSource audioSource;
    
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    
    private float xRotation = 0f;
    private float zRotation = 0f;
    private float stepTimer;
    
    private bool isSliding;
    private bool isWallRunning;
    private bool wallLeft;
    private bool wallRight;
    private Vector3 wallNormal;
    
    private float normalHeight;
    private float slideHeight;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction slideAction;

    private void Awake()
    {
        moveAction = new InputAction("Move");
        moveAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        lookAction = new InputAction("Look", binding: "<Mouse>/delta");
        jumpAction = new InputAction("Jump", binding: "<Keyboard>/space");
        slideAction = new InputAction("Slide", binding: "<Keyboard>/leftCtrl");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        slideAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        jumpAction.Disable();
        slideAction.Disable();
    }

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        
        normalHeight = controller.height;
        slideHeight = normalHeight * 0.5f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleCamera();
        HandleMovement();
        HandleAnimations();
        HandleAudio();
    }

    private void HandleCamera()
    {
        Vector2 look = lookAction.ReadValue<Vector2>();
        float mouseX = look.x * mouseSensitivity * 0.05f;
        float mouseY = look.y * mouseSensitivity * 0.05f;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        float targetTilt = 0f;
        if (isWallRunning)
        {
            if (wallLeft) targetTilt = -wallRunTilt;
            if (wallRight) targetTilt = wallRunTilt;
        }

        zRotation = Mathf.Lerp(zRotation, targetTilt, Time.deltaTime * tiltSpeed);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, zRotation);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void CheckWallRun(Vector2 moveInput)
    {
        isWallRunning = false;
        wallLeft = false;
        wallRight = false;
        
        if (controller.isGrounded || moveInput.y <= 0) return;

        RaycastHit hit;
        wallRight = Physics.Raycast(transform.position, transform.right, out hit, 1.2f);
        if (!wallRight)
        {
            wallLeft = Physics.Raycast(transform.position, -transform.right, out hit, 1.2f);
        }

        if (wallRight || wallLeft)
        {
            float surfaceAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (surfaceAngle > 70f && surfaceAngle < 110f)
            {
                isWallRunning = true;
                wallNormal = hit.normal;
            }
        }
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        isSliding = slideAction.IsPressed() && controller.isGrounded;

        CheckWallRun(moveInput);

        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        Vector3 inputDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
        float currentTargetSpeed = isSliding ? slideSpeed : walkSpeed;
        Vector3 targetVelocity = inputDirection * currentTargetSpeed;

        if (isWallRunning)
        {
            verticalVelocity = wallRunGravity;
            
            Vector3 wallRunDir = Vector3.Cross(wallNormal, Vector3.up);
            if (Vector3.Dot(wallRunDir, transform.forward) < 0)
            {
                wallRunDir = -wallRunDir;
            }
            
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, wallRunDir * wallRunSpeed, groundAcceleration * Time.deltaTime);
        }
        else if (controller.isGrounded)
        {
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, groundAcceleration * Time.deltaTime);
        }
        else
        {
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, airControl * Time.deltaTime);
        }

        if (jumpAction.WasPressedThisFrame())
        {
            if (controller.isGrounded)
            {
                verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravity);
            }
            else if (isWallRunning)
            {
                verticalVelocity = wallJumpUpwardForce;
                
                Vector3 jumpDirection = (wallNormal * 1.5f + transform.forward).normalized;
                horizontalVelocity = jumpDirection * wallJumpForwardForce;
                
                isWallRunning = false;
            }
        }

        if (!isWallRunning)
        {
            float currentGravity = (verticalVelocity < 0) ? gravity * fallMultiplier : gravity;
            verticalVelocity += currentGravity * Time.deltaTime;
        }

        Vector3 finalVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);
        controller.Move(finalVelocity * Time.deltaTime);

        controller.height = Mathf.Lerp(controller.height, isSliding ? slideHeight : normalHeight, Time.deltaTime * 10f);
    }

    private void HandleAnimations()
    {
        if (animator == null) return;
        
        animator.SetFloat("Speed", horizontalVelocity.magnitude);
        animator.SetBool("IsSliding", isSliding);
        animator.SetBool("IsWallRunning", isWallRunning);
    }

    private void HandleAudio()
    {
        float speed = horizontalVelocity.magnitude;
        
        if (controller.isGrounded && speed > 0.1f && !isSliding)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                audioSource.PlayOneShot(stepSound);
                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }
}