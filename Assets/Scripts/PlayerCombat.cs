using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackDamage = 15f;
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    public float attackRadius = 0.5f;
    
    [Header("Hit Settings")]
    public LayerMask enemyLayers;
    public Transform attackPoint;
    public bool showDebugGizmos = true;
    
    [Header("Combo System")]
    public bool enableCombo = false;
    public int maxComboCount = 3;
    public float comboResetTime = 2f;
    public float[] comboDamageMultipliers = { 1f, 1.2f, 1.5f };
    
    [Header("Animation")]
    public string attackTrigger = "Attack";
    public string[] comboTriggers = { "Attack1", "Attack2", "Attack3" };
    
    // Componentes
    private Animator animator;
    private PlayerHealth playerHealth;
    
    // Estado
    private float lastAttackTime = 0f;
    private bool isAttacking = false;
    private int currentCombo = 0;
    private float lastComboTime = 0f;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
        
        if (animator == null)
        {
            Debug.LogError("Animator não encontrado no Player!");
        }
        
        if (attackPoint == null)
        {
            attackPoint = transform;
            Debug.LogWarning("AttackPoint não definido, usando posição do player");
        }
    }
    
    void Update()
    {
        // Verifica se o combo deve ser resetado
        if (enableCombo && currentCombo > 0 && Time.time > lastComboTime + comboResetTime)
        {
            ResetCombo();
        }
        
        // Input de ataque
        if (Mouse.current.leftButton.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryAttack();
        }
    }
    
    void TryAttack()
    {
        if (playerHealth != null && playerHealth.currentHealth <= 0) return;
        if (isAttacking) return;
        if (!CanAttack()) return;
        
        StartAttack();
    }
    
    bool CanAttack()
    {
        return Time.time >= lastAttackTime + attackCooldown;
    }
    
    void StartAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        // Sistema de combo
        if (enableCombo)
        {
            currentCombo++;
            if (currentCombo > maxComboCount)
            {
                currentCombo = 1;
            }
            
            lastComboTime = Time.time;
            
            // Usa trigger específico do combo
            if (currentCombo <= comboTriggers.Length)
            {
                animator.SetTrigger(comboTriggers[currentCombo - 1]);
            }
            else
            {
                animator.SetTrigger(attackTrigger);
            }
            
            Debug.Log($"Combo {currentCombo} iniciado");
        }
        else
        {
            animator.SetTrigger(attackTrigger);
        }
    }
    
    // MÉTODO CHAMADO PELO EVENTO NA ANIMAÇÃO DE ATAQUE
    public void AnimationEvent_ApplyDamage()
    {
        ApplyAttackDamage();
    }
    
    void ApplyAttackDamage()
    {
        float damage = attackDamage;
        
        // Aplica multiplicador de combo
        if (enableCombo && currentCombo <= comboDamageMultipliers.Length)
        {
            damage *= comboDamageMultipliers[currentCombo - 1];
        }
        
        // Detecta apenas o Boss no alcance
        Collider[] hitEnemies = Physics.OverlapSphere(
            attackPoint.position,
            attackRadius,
            enemyLayers
        );
        
        bool hitBoss = false;
        
        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.CompareTag("Boss"))
            {
                BossStateMachine boss = enemy.GetComponent<BossStateMachine>();
                if (boss != null)
                {
                    boss.TakeDamage(damage);
                    Debug.Log($"Player causou {damage} de dano ao Boss! (Combo: {currentCombo})");
                    hitBoss = true;
                    
                    // Feedback visual/hit effect
                    OnHitBoss(enemy.transform);
                }
            }
        }
        
        // Se não acertou nenhum inimigo
        if (!hitBoss)
        {
            Debug.Log("Ataque do player não acertou o Boss");
        }
    }
    
    void OnHitBoss(Transform boss)
    {
        // Aqui você pode adicionar feedbacks:
        // - Partículas de hit
        // - Som
        // - Camera shake
        // - etc.
        
        Debug.Log($"Acertou o Boss: {boss.name}");
    }
    
    // Chamado quando a animação de ataque termina
    public void AnimationEvent_AttackEnd()
    {
        isAttacking = false;
        
        // Se não tem combo ou atingiu o máximo, reseta
        if (!enableCombo || currentCombo >= maxComboCount)
        {
            ResetCombo();
        }
        
        Debug.Log("Ataque do Player concluído");
    }
    
    void ResetCombo()
    {
        currentCombo = 0;
        
        // Reseta todos os triggers de combo
        if (animator != null)
        {
            foreach (string trigger in comboTriggers)
            {
                animator.ResetTrigger(trigger);
            }
        }
        
        Debug.Log("Combo resetado");
    }
    
    // Método para receber dano (chamado pelo BossCombat)
    public void TakeDamageFromEnemy(float damage, Transform enemy)
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            
            // Feedback de receber dano
            OnTakeDamage(enemy);
        }
    }
    
    void OnTakeDamage(Transform damageSource)
    {
        // Aqui você pode adicionar:
        // - Animação de tomar dano
        // - Som
        // - Efeitos visuais
        // - Knockback
        
        if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
        }
        
        // Exemplo de knockback simples
        Vector3 knockbackDirection = (transform.position - damageSource.position).normalized;
        knockbackDirection.y = 0.2f; // Pequeno impulso para cima
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(knockbackDirection * 5f, ForceMode.Impulse);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // Gizmo para alcance de ataque
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
            
            // Gizmo para direção do ataque
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(attackPoint.position, attackPoint.forward * attackRange);
        }
    }
}