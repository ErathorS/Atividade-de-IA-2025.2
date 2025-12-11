using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 10f;
    
    [Header("Jump")]
    public float jumpForce = 5f;
    public Transform groundCheck;
    public float groundDistance = 0.3f;
    public LayerMask groundMask;
    
    [Header("Animation Parameters")]
    public string moveSpeedParam = "MoveSpeed";
    public string isGroundedParam = "IsGrounded";
    public string isRunningParam = "IsRunning";
    public string jumpTriggerParam = "Jump";
    
    // Componentes
    private Animator animator;
    private Rigidbody rb;
    private CharacterController controller;
    private Vector3 moveDirection;
    private bool isGrounded;
    private bool isRunning = false;
    private float currentSpeed;
    
    // Input
    private Vector2 moveInput;
    private bool jumpInput;
    private bool runInput;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<CharacterController>();
        
        if (animator == null)
        {
            Debug.LogError("Animator não encontrado no Player!");
        }
        
        if (rb != null)
        {
            rb.freezeRotation = true;
        }
    }
    
    void Update()
    {
        // Coleta inputs
        GetInput();
        
        // Verifica chão
        CheckGround();
        
        // Atualiza animações
        UpdateAnimations();
    }
    
    void FixedUpdate()
    {
        // Movimentação física
        HandleMovement();
        
        // Pulo
        if (jumpInput && isGrounded)
        {
            Jump();
        }
    }
    
    void GetInput()
    {
        // Movimento WASD
        float x = 0, z = 0;
        
        if (Keyboard.current.wKey.isPressed) z = 1;
        if (Keyboard.current.sKey.isPressed) z = -1;
        if (Keyboard.current.aKey.isPressed) x = -1;
        if (Keyboard.current.dKey.isPressed) x = 1;
        
        moveInput = new Vector2(x, z).normalized;
        
        // Correr (Shift)
        runInput = Keyboard.current.leftShiftKey.isPressed;
        
        // Pulo (Space)
        jumpInput = Keyboard.current.spaceKey.wasPressedThisFrame;
    }
    
    void HandleMovement()
    {
        if (controller != null)
        {
            // Movimentação com CharacterController
            Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
            move = transform.TransformDirection(move);
            
            // Aplica gravidade
            if (!controller.isGrounded)
            {
                moveDirection.y -= 9.81f * Time.deltaTime;
            }
            else
            {
                moveDirection.y = -0.5f;
            }
            
            // Velocidade baseada em walk/run
            currentSpeed = runInput ? runSpeed : walkSpeed;
            move *= currentSpeed;
            move.y = moveDirection.y;
            
            controller.Move(move * Time.deltaTime);
            
            // Rotação na direção do movimento
            if (moveInput.magnitude > 0.1f)
            {
                Vector3 lookDirection = new Vector3(moveInput.x, 0, moveInput.y);
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else if (rb != null)
        {
            // Movimentação com Rigidbody (alternativa)
            Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
            currentSpeed = runInput ? runSpeed : walkSpeed;
            Vector3 targetVelocity = move * currentSpeed;
            
            // Preserva velocidade Y para pulo/gravidade
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, 10f * Time.deltaTime);
            
            // Rotação
            if (moveInput.magnitude > 0.1f)
            {
                Vector3 lookDirection = new Vector3(moveInput.x, 0, moveInput.y);
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    void CheckGround()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }
        else if (controller != null)
        {
            isGrounded = controller.isGrounded;
        }
        else
        {
            isGrounded = true; // Fallback
        }
    }
    
    void UpdateAnimations()
    {
        if (animator == null) return;
        
        // Velocidade de movimento (0-1 normalizada)
        float normalizedSpeed = Mathf.Clamp01(currentSpeed / runSpeed * moveInput.magnitude);
        animator.SetFloat(moveSpeedParam, normalizedSpeed);
        
        // Estado no chão
        animator.SetBool(isGroundedParam, isGrounded);
        
        // Correndo
        animator.SetBool(isRunningParam, runInput && moveInput.magnitude > 0.1f);
    }
    
    void Jump()
    {
        if (rb != null)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            animator.SetTrigger(jumpTriggerParam);
        }
        else if (controller != null)
        {
            moveDirection.y = jumpForce;
            animator.SetTrigger(jumpTriggerParam);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Gizmo para verificação de chão
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}