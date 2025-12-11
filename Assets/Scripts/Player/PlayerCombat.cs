using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackDamage = 20f;
    public float attackCooldown = 1f;
    public float attackWindup = 0.3f;
    public float attackDuration = 0.8f;
    
    [Header("Attack Range - CONE")]
    public float attackRange = 2f;
    public float attackAngle = 45f;
    
    [Header("Layers")]
    public LayerMask enemyLayers;
    public LayerMask obstacleLayers;
    
    [Header("Animation")]
    public string attackTriggerName = "Attack";
    
    [Header("Debug")]
    public bool showDebugGizmos = true;
    public Color attackConeColor = Color.red;
    
    private Animator animator;
    private PlayerHealth playerHealth;
    private PlayerAnimationController movementController;
    
    private float lastAttackTime = 0f;
    private bool isAttacking = false;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
        movementController = GetComponent<PlayerAnimationController>();
        
        if (animator == null)
        {
            Debug.LogError("Animator não encontrado no Player!");
        }
    }
    
    void Update()
    {
        if ((Mouse.current.leftButton.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame) 
            && !isAttacking && CanAttack())
        {
            StartAttack();
        }
    }
    
    bool CanAttack()
    {
        if (playerHealth != null && playerHealth.currentHealth <= 0) return false;
        return Time.time >= lastAttackTime + attackCooldown;
    }
    
    void StartAttack()
    {
        StartCoroutine(AttackSequence());
    }
    
    IEnumerator AttackSequence()
    {
        isAttacking = true;
        
        if (attackWindup > 0)
        {
            yield return new WaitForSeconds(attackWindup);
        }
        
        if (!string.IsNullOrEmpty(attackTriggerName))
        {
            animator.SetTrigger(attackTriggerName);
        }
        
        lastAttackTime = Time.time;
        
        yield return new WaitForSeconds(attackDuration);
        
        isAttacking = false;
    }
    
    public void AnimationEvent_ApplyDamage()
    {
        ApplyAttackDamage();
    }
    
    bool IsInAttackCone(Transform target)
    {
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float angleToTarget = Vector3.Angle(GetAttackDirection(), directionToTarget);
        
        if (angleToTarget <= attackAngle * 0.5f)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            if (distanceToTarget <= attackRange)
            {
                return true;
            }
        }
        
        return false;
    }
    
    bool HasObstacleBetween(Transform target)
    {
        Vector3 start = transform.position + Vector3.up * 1f;
        Vector3 end = target.position + Vector3.up * 0.5f;
        
        RaycastHit hit;
        if (Physics.Linecast(start, end, out hit, obstacleLayers))
        {
            if (hit.transform != target)
            {
                return true;
            }
        }
        
        return false;
    }
    
    Vector3 GetAttackDirection()
    {
        if (movementController != null)
        {
            Vector3 moveDirection = movementController.GetMoveDirection();
            if (moveDirection.magnitude > 0.1f)
            {
                return moveDirection.normalized;
            }
        }
        
        return transform.forward;
    }
    
    void ApplyAttackDamage()
    {
        GameObject[] bosses = GameObject.FindGameObjectsWithTag("Boss");
        
        foreach (GameObject bossObj in bosses)
        {
            Transform boss = bossObj.transform;
            
            if (IsInAttackCone(boss))
            {
                if (!HasObstacleBetween(boss))
                {
                    BossStateMachine bossComponent = boss.GetComponent<BossStateMachine>();
                    if (bossComponent != null)
                    {
                        bossComponent.TakeDamage(attackDamage);
                        Debug.Log($"Player causou {attackDamage} de dano ao Boss!");
                        OnHitEnemy(boss);
                    }
                }
            }
        }
    }
    
    void OnHitEnemy(Transform enemy)
    {
        Debug.Log($"Player acertou: {enemy.name}");
    }
    
    public void TakeDamageFromEnemy(float damage, Transform enemy)
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            
            if (animator != null)
            {
                animator.SetTrigger("TakeDamage");
            }
            
            ApplyKnockback(enemy);
        }
    }
    
    void ApplyKnockback(Transform damageSource)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            Vector3 knockbackDirection = (transform.position - damageSource.position).normalized;
            knockbackDirection.y = 0.3f;
            rb.AddForce(knockbackDirection * 3f, ForceMode.Impulse);
        }
    }
    
    public void AnimationEvent_AttackEnd()
    {
        if (!string.IsNullOrEmpty(attackTriggerName))
        {
            animator.ResetTrigger(attackTriggerName);
        }
        
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        Gizmos.color = attackConeColor;
        
        Vector3 forward = GetAttackDirection();
        Vector3 leftBoundary = Quaternion.Euler(0, -attackAngle / 2, 0) * forward * attackRange;
        Vector3 rightBoundary = Quaternion.Euler(0, attackAngle / 2, 0) * forward * attackRange;
        
        Gizmos.DrawRay(transform.position, leftBoundary);
        Gizmos.DrawRay(transform.position, rightBoundary);
        
        int segments = 20;
        float deltaAngle = attackAngle / segments;
        Vector3 prevPoint = transform.position + Quaternion.Euler(0, -attackAngle / 2, 0) * forward * attackRange;
        
        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = -attackAngle / 2 + deltaAngle * i;
            Vector3 currentPoint = transform.position + Quaternion.Euler(0, currentAngle, 0) * forward * attackRange;
            
            Gizmos.DrawLine(transform.position, currentPoint);
            Gizmos.DrawLine(prevPoint, currentPoint);
            
            prevPoint = currentPoint;
        }
    }
}