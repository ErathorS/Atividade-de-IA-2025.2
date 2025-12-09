using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Jump")]
    public float jumpForce = 5f;
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;

    [Header("Particles")]
    public ParticleSystem moveParticles;

    private Rigidbody rb;
    private Vector2 moveInput;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    private void Update()
    {
        ReadInput();
        HandleParticles();
        HandleJump();
    }

    private void FixedUpdate()
    {
        Move();
        RotateToDirection();
    }

    // ========================================
    // INPUT
    // ========================================
    void ReadInput()
    {
        float x = 0;
        float y = 0;

        if (Keyboard.current.aKey.isPressed) x = -1;
        if (Keyboard.current.dKey.isPressed) x = 1;
        if (Keyboard.current.wKey.isPressed) y = 1;
        if (Keyboard.current.sKey.isPressed) y = -1;

        moveInput = new Vector2(x, y).normalized;
    }

    // ========================================
    // MOVIMENTO
    // ========================================
    void Move()
    {
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);

        rb.linearVelocity = new Vector3(
            direction.x * moveSpeed,
            rb.linearVelocity.y,
            direction.z * moveSpeed
        );
    }

    // ========================================
    // ROTAÇÃO SUAVE PARA A DIREÇÃO DO INPUT
    // ========================================
    void RotateToDirection()
    {
        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);

            Quaternion targetRot = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    // ========================================
    // PULO
    // ========================================
    void HandleJump()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            // Partículas ao pular (opcional)
            moveParticles.Play();
        }
    }

    bool IsGrounded()
    {
        return Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
    }

    // ========================================
    // PARTÍCULAS (agora só no chão)
    // ========================================
    void HandleParticles()
    {
        if (moveParticles == null) return;

        bool moving = moveInput.sqrMagnitude > 0.01f;
        bool grounded = IsGrounded();

        if (moving && grounded)
        {
            if (!moveParticles.isPlaying)
                moveParticles.Play();
        }
        else
        {
            if (moveParticles.isPlaying)
                moveParticles.Stop();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}
