using UnityEngine;
using UnityEngine.InputSystem;

public class player : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 6f;              // Velocidad fija (unidades por segundo)
    public Rigidbody rb;

    [Header("Salto y Gravedad")]
    public float jumpHeight = 3f;
    public int maxJumps = 2;
    public float gravity = -9.81f;        // Gravedad del personaje (negativa)
    public float fallMultiplier = 2.5f;   // Multiplicador de gravedad al caer

    [Header("Reset por caída")]
    public float fallLimitY = -45f;

    [Header("Cámara Tercera Persona")]
    public Transform camaraPrincipal;
    public float mouseSensitivity = 0.5f;
    public float cameraDistance = 5f;
    public Vector3 cameraOffset = new Vector3(0f, 1.5f, 0f);

    private float rotacionX = 0f;
    private float rotacionY = 0f;
    private int jumpsRemaining;
    private Vector3 moveDirection;
    private bool isGrounded;
    private Vector3 respawnPosition;      // Último checkpoint alcanzado

    void Start()
    {
        if (camaraPrincipal == null)
        {
            camaraPrincipal = Camera.main.transform;
        }

        // Desactivamos la gravedad de Unity para usar la nuestra
        rb.useGravity = false;

        // Quitamos la fricción del collider para que no frene el movimiento en el suelo
        Collider col = GetComponent<Collider>();
        PhysicsMaterial noFriction = new PhysicsMaterial("SinFriccion");
        noFriction.dynamicFriction = 0f;
        noFriction.staticFriction = 0f;
        noFriction.frictionCombine = PhysicsMaterialCombine.Minimum;
        col.material = noFriction;

        // Punto inicial de reaparición hasta cruzar un checkpoint
        respawnPosition = transform.position;
        jumpsRemaining = maxJumps;

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 1. VECTORES DE DIRECCIÓN BASADOS EN LA CÁMARA
        Vector3 camForward = camaraPrincipal.forward;
        Vector3 camRight = camaraPrincipal.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        // 2. ENTRADA DE MOVIMIENTO
        moveDirection = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) moveDirection += camForward;
        if (Keyboard.current.sKey.isPressed) moveDirection -= camForward;
        if (Keyboard.current.dKey.isPressed) moveDirection += camRight;
        if (Keyboard.current.aKey.isPressed) moveDirection -= camRight;

        // 3. SALTO (máximo 2)
        if (Keyboard.current.spaceKey.wasPressedThisFrame && jumpsRemaining > 0)
        {
            float jumpVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            Vector3 vel = rb.linearVelocity;
            vel.y = jumpVelocity;
            rb.linearVelocity = vel;

            jumpsRemaining--;
            isGrounded = false;
        }

        // 4. RESET SI CAE DEMASIADO
        if (transform.position.y < fallLimitY)
        {
            ResetPosition();
        }
    }

    void FixedUpdate()
    {
        // MOVIMIENTO A VELOCIDAD FIJA (no toca la velocidad vertical)
        Vector3 horizontalVelocity = moveDirection.normalized * speed;
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);

        // GRAVEDAD PROPIA: solo cuando NO está tocando el suelo
        if (!isGrounded)
        {
            float multiplier = rb.linearVelocity.y < 0 ? fallMultiplier : 1f;
            rb.AddForce(Vector3.up * gravity * multiplier, ForceMode.Acceleration);
        }

        // Se vuelve a calcular en OnCollisionStay durante este paso de física
        isGrounded = false;
    }

    // Detecta el suelo: si tocamos algo con la normal hacia arriba, recuperamos los saltos
    void OnCollisionStay(Collision collision)
    {
        // Ignoramos el momento del despegue (aún subiendo)
        if (rb.linearVelocity.y > 0.1f) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                jumpsRemaining = maxJumps;
                return;
            }
        }
    }

    // Lo llama el script Checkpoint al atravesar un checkpoint
    public void SetCheckpoint(Vector3 newPosition)
    {
        respawnPosition = newPosition;
    }

    void ResetPosition()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = respawnPosition;
        transform.position = respawnPosition;
        jumpsRemaining = maxJumps;
    }

    void LateUpdate()
    {
        // 5. ROTACIÓN CON EL MOUSE
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        rotacionY += mouseDelta.x * mouseSensitivity;
        rotacionX -= mouseDelta.y * mouseSensitivity;
        rotacionX = Mathf.Clamp(rotacionX, -80f, 80f);

        camaraPrincipal.rotation = Quaternion.Euler(rotacionX, rotacionY, 0f);

        // 6. POSICIÓN DE TERCERA PERSONA (ÓRBITA)
        Vector3 targetPosition = transform.position + cameraOffset;
        camaraPrincipal.position = targetPosition - camaraPrincipal.forward * cameraDistance;
    }
}