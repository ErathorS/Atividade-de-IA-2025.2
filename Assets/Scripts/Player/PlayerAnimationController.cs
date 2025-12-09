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
    
    [Header("Attack")]
    public float attackDamage = 15f;
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    
    [Header("Animation Parameters")]
    public string moveSpeedParam = "MoveSpeed";
    public string isGroundedParam = "IsGrounded";
    public string isRunningParam = "IsRunning";
    public string attackTriggerParam = "Attack";
    public string jumpTriggerParam = "Jump";
    
    // Componentes
    private Animator animator;
    private Rigidbody rb;
    private CharacterController controller;
    private Vector3 moveDirection;
    private bool isGrounded;
    private bool isRunning = false;
    private bool isAttacking = false;
    private float lastAttackTime = 0f;
    private float currentSpeed;
    
    // Input
    private Vector2 moveInput;
    private bool jumpInput;
    private bool runInput;
    private bool attackInput;
    
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
        
        // Ataca se pressionado
        if (attackInput && CanAttack())
        {
            StartAttack();
        }
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
        runInput = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        
        // Pulo (Space)
        jumpInput = Keyboard.current.spaceKey.wasPressedThisFrame;
        
        // Ataque (Botão esquerdo do mouse ou E)
        attackInput = Mouse.current.leftButton.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame;
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
        
        // Atacando (controlado pelo trigger)
        if (isAttacking)
        {
            // O trigger já foi ativado, a animação cuida do resto
        }
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
    
    bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown && !isAttacking;
    }
    
    void StartAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        animator.SetTrigger(attackTriggerParam);
        
        // O dano será aplicado pelo evento na animação
        Debug.Log("Player atacando!");
    }
    
    // MÉTODO CHAMADO PELO EVENTO NA ANIMAÇÃO DE ATAQUE
    public void AnimationEvent_ApplyDamage()
    {
        ApplyAttackDamage();
    }
    
    void ApplyAttackDamage()
    {
        // Detecta inimigos na frente do player
        RaycastHit[] hits = Physics.SphereCastAll(
            transform.position + Vector3.up * 0.5f,
            0.5f,
            transform.forward,
            attackRange
        );
        
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Boss"))
            {
                BossStateMachine boss = hit.collider.GetComponent<BossStateMachine>();
                if (boss != null)
                {
                    boss.TakeDamage(attackDamage);
                    Debug.Log($"Player causou {attackDamage} de dano ao Boss!");
                }
            }
        }
    }
    
    // Chamado quando a animação de ataque termina
    public void AnimationEvent_AttackEnd()
    {
        isAttacking = false;
        animator.ResetTrigger(attackTriggerParam);
        Debug.Log("Ataque do Player concluído");
    }
    
    void OnDrawGizmosSelected()
    {
        // Gizmo para alcance de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f + transform.forward * attackRange, 0.5f);
        
        // Gizmo para verificação de chão
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}