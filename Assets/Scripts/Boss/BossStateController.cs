using UnityEngine;
using UnityEngine.UI;

public class BossStateController : MonoBehaviour
{
    public enum BossState
    {
        BEHAVIOR_1,  // Vida ≥ 70%
        BEHAVIOR_2,  // 30% ≤ Vida < 70%
        BEHAVIOR_3,  // Vida ≤ 30%
        ESCAPE       // Estado de fuga
    }
    
    [Header("Health Settings")]
    public float maxHealth = 1000f;
    public float currentHealth;
    public Slider healthBar;
    
    [Header("State Parameters")]
    public float escapeDuration = 3f;
    public float behavior1Speed = 3f;
    public float behavior2Speed = 5f;
    public float behavior3Speed = 7f;
    public float escapeSpeed = 8f;
    
    [Header("Escape Settings")]
    public Transform[] safePoints;
    public float escapeDistance = 10f;
    
    private BossState currentState;
    private AITarget_Boss aiController;
    private UnityEngine.AI.NavMeshAgent agent;
    private float escapeTimer = 0f;
    private bool isInTransition = false;
    
    void Start()
    {
        currentHealth = maxHealth;
        aiController = GetComponent<AITarget_Boss>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        
        // Estado inicial
        ChangeState(BossState.BEHAVIOR_1);
        UpdateHealthBar();
    }
    
    void Update()
    {
        // Atualiza barra de vida
        UpdateHealthBar();
        
        // Verifica transições de estado baseado na vida
        CheckStateTransitions();
        
        // Executa lógica do estado atual
        ExecuteCurrentState();
    }
    
    void CheckStateTransitions()
    {
        float healthPercentage = currentHealth / maxHealth;
        BossState targetState = currentState;
        
        // Determina estado alvo baseado na vida
        if (healthPercentage >= 0.7f)
            targetState = BossState.BEHAVIOR_1;
        else if (healthPercentage >= 0.3f)
            targetState = BossState.BEHAVIOR_2;
        else
            targetState = BossState.BEHAVIOR_3;
        
        // Se mudou de estado, ativa estado de fuga
        if (targetState != currentState && !isInTransition)
        {
            StartCoroutine(TransitionToState(targetState));
        }
    }
    
    void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case BossState.BEHAVIOR_1:
                ExecuteBehavior1();
                break;
            case BossState.BEHAVIOR_2:
                ExecuteBehavior2();
                break;
            case BossState.BEHAVIOR_3:
                ExecuteBehavior3();
                break;
            case BossState.ESCAPE:
                ExecuteEscape();
                break;
        }
    }
    
    void ExecuteBehavior1()
    {
        // Comportamento 1: Patrulha com baixa agressividade
        aiController.UpdateSpeed(behavior1Speed);
        // Lógica específica do comportamento 1
    }
    
    void ExecuteBehavior2()
    {
        // Comportamento 2: Agressividade moderada
        aiController.UpdateSpeed(behavior2Speed);
        // Adiciona ataques especiais
        if (Random.Range(0, 100) < 10) // 10% chance por frame
        {
            SpecialAttack();
        }
    }
    
    void ExecuteBehavior3()
    {
        // Comportamento 3: Comportamento imprevisível
        aiController.UpdateSpeed(behavior3Speed);
        
        // Movimentos imprevisíveis
        if (Random.Range(0, 100) < 5)
        {
            DashAttack();
        }
    }
    
    void ExecuteEscape()
    {
        escapeTimer -= Time.deltaTime;
        
        // Move para ponto seguro
        if (safePoints.Length > 0)
        {
            Transform closestSafePoint = GetClosestSafePoint();
            agent.destination = closestSafePoint.position;
            aiController.UpdateSpeed(escapeSpeed);
        }
        
        // Termina estado de fuga
        if (escapeTimer <= 0)
        {
            isInTransition = false;
        }
    }
    
    System.Collections.IEnumerator TransitionToState(BossState newState)
    {
        isInTransition = true;
        
        // Ativa estado de fuga
        ChangeState(BossState.ESCAPE);
        escapeTimer = escapeDuration;
        
        // Espera término da fuga
        yield return new WaitForSeconds(escapeDuration);
        
        // Muda para novo estado
        ChangeState(newState);
    }
    
    void ChangeState(BossState newState)
    {
        // Debug para acompanhar mudanças de estado
        Debug.Log($"Boss mudou de {currentState} para {newState}");
        currentState = newState;
    }
    
    Transform GetClosestSafePoint()
    {
        Transform closest = safePoints[0];
        float closestDistance = Vector3.Distance(transform.position, closest.position);
        
        foreach (Transform point in safePoints)
        {
            float distance = Vector3.Distance(transform.position, point.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = point;
            }
        }
        return closest;
    }
    
    void SpecialAttack()
    {
        // Implemente ataque especial
        Debug.Log("Ataque Especial!");
    }
    
    void DashAttack()
    {
        // Implemente investida rápida
        Debug.Log("Dash Attack!");
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        Debug.Log("Boss Derrotado!");
        // Adicione lógica de morte (animação, som, etc.)
        Destroy(gameObject, 2f);
    }
    
    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
    }
}