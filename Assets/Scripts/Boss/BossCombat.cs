using UnityEngine;
using System.Collections;

public class BossCombat : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackDamage = 20f;
    public float attackCooldown = 2f;
    public float attackWindup = 0.5f;
    public float attackDuration = 1f;
    
    [Header("Attack Range")]
    public float attackRadius = 2.5f;
    public float attackAngle = 45f;
    
    [Header("Obstacle Detection")]
    public LayerMask obstacleLayer; 
    
    [Header("Animation")]
    public string attackTriggerName = "Attacking";
    
    private Animator animator;
    private Transform player;
    private float lastAttackTime = 0f;
    private bool isAttacking = false;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        
        if (!HasParameter("Attacking", animator))
        {
            Debug.LogError("Parâmetro 'Attacking' não encontrado no Animator! Adicione um Trigger com esse nome.");
        }
    }
    
    bool HasParameter(string paramName, Animator animator)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
    
    public bool CanAttack()
    {
        if (player == null || isAttacking) return false;
        
        float distance = Vector3.Distance(transform.position, player.position);
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        
        return distance <= attackRadius && 
               angleToPlayer <= attackAngle * 0.5f &&
               Time.time >= lastAttackTime + attackCooldown;
    }
    
    public void StartAttack()
    {
        if (isAttacking) return;
        
        StartCoroutine(AttackSequence());
    }
    
    IEnumerator AttackSequence()
    {
        isAttacking = true;
        
        yield return new WaitForSeconds(attackWindup);
        
        Debug.Log("Ativando trigger 'Attacking' no Animator");
        animator.SetTrigger(attackTriggerName);
        
        lastAttackTime = Time.time;
        
        yield return new WaitForSeconds(attackDuration);
        
        isAttacking = false;
    }
    
    public void AnimationEvent_ApplyDamage()
    {
        Debug.Log("Evento de animação: Aplicando dano!");
        ApplyAttackDamage();
    }
    
    bool HasObstacleBetween(Transform target)
    {
        Vector3 start = transform.position + Vector3.up * 1f;
        Vector3 end = target.position + Vector3.up * 0.5f;
        
        RaycastHit hit;
        if (Physics.Linecast(start, end, out hit, obstacleLayer))
        {
            if (hit.transform != target)
            {
                Debug.Log($"Boss: Obstáculo detectado: {hit.transform.name}");
                return true;
            }
        }
        
        return false;
    }
    
    void ApplyAttackDamage()
    {
        if (player == null)
        {
            Debug.LogWarning("Player não encontrado para aplicar dano!");
            return;
        }
        
        float distance = Vector3.Distance(transform.position, player.position);
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        
        //Debug.Log($"Verificando ataque - Distância: {distance}, Alcance: {attackRadius}, Ângulo: {angleToPlayer}");
        
        if (distance <= attackRadius && angleToPlayer <= attackAngle * 0.5f)
        {
            if (HasObstacleBetween(player))
            {
                Debug.Log("Ataque bloqueado por obstáculo!");
                return;
            }
            
            PlayerCombat playerCombat = player.GetComponent<PlayerCombat>();
            if (playerCombat != null)
            {
                playerCombat.TakeDamageFromEnemy(attackDamage, transform);
                Debug.Log($"Boss causou {attackDamage} de dano ao player via PlayerCombat!");
            }
            else
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    Debug.Log($"Boss causou {attackDamage} de dano ao player via PlayerHealth!");
                }
                else
                {
                    Debug.LogWarning("Componente PlayerHealth não encontrado no player!");
                }
            }
        }
        else
        {
            Debug.Log("Ataque não acertou: fora do alcance ou ângulo!");
        }
    }
    
    public bool IsAttacking()
    {
        return isAttacking;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Vector3 leftAttack = Quaternion.Euler(0, -attackAngle / 2, 0) * transform.forward * attackRadius;
        Vector3 rightAttack = Quaternion.Euler(0, attackAngle / 2, 0) * transform.forward * attackRadius;
        
        Gizmos.DrawRay(transform.position, leftAttack);
        Gizmos.DrawRay(transform.position, rightAttack);
        Gizmos.DrawLine(transform.position + leftAttack, transform.position + rightAttack);
        
        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 start = transform.position + Vector3.up * 1f;
            Vector3 end = player.position + Vector3.up * 0.5f;
            Gizmos.DrawLine(start, end);
        }
    }
}