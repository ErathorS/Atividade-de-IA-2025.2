using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BossStateMachine : MonoBehaviour
{
    public enum BossState
    {
        PATROL,
        CHASE,
        ATTACK,
        ESCAPE,
        DEAD
    }
    
    [Header("Health Settings")]
    public float maxHealth = 1000f;
    public float currentHealth;
    public Slider healthBar;
    
    [Header("State Parameters")]
    public float escapeDuration = 3f;
    public float behavior1Speed = 3f;  // Vida ≥ 70%
    public float behavior2Speed = 5f;  // 30% ≤ Vida < 70%
    public float behavior3Speed = 7f;  // Vida ≤ 30%
    public float escapeSpeed = 8f;
    
    [Header("Escape Settings")]
    public Transform[] safePoints;
    
    private BossNavigationSystem navigation;
    private BossDetection detection;
    private BossCombat combat;
    private Animator animator;
    
    private BossState currentState;
    private BossState healthBasedState;
    private float escapeTimer = 0f;
    private bool isInTransition = false;
    
    void Start()
    {
        navigation = GetComponent<BossNavigationSystem>();
        detection = GetComponent<BossDetection>();
        combat = GetComponent<BossCombat>();
        animator = GetComponent<Animator>();
        
        if (navigation == null) Debug.LogError("BossNavigationSystem não encontrado!");
        if (detection == null) Debug.LogError("BossDetection não encontrado!");
        if (combat == null) Debug.LogError("BossCombat não encontrado!");
        if (animator == null) Debug.LogError("Animator não encontrado!");
        
        currentHealth = maxHealth;
        UpdateHealthBar();
        
        healthBasedState = GetStateByHealth();
        ChangeState(BossState.PATROL);
    }
    
    void Update()
    {
        UpdateHealthBar();
        
        ExecuteCurrentState();
        
        if (!isInTransition && currentState != BossState.ESCAPE && 
            currentState != BossState.ATTACK && currentState != BossState.DEAD)
        {
            CheckHealthBasedTransitions();
        }
        
        if (currentState == BossState.ESCAPE)
        {
            escapeTimer -= Time.deltaTime;
            if (escapeTimer <= 0)
            {
                EndEscapeState();
            }
        }
    }
    
    void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case BossState.PATROL:
                ExecutePatrol();
                break;
            case BossState.CHASE:
                ExecuteChase();
                break;
            case BossState.ATTACK:
                ExecuteAttack();
                break;
            case BossState.ESCAPE:
                ExecuteEscape();
                break;
            case BossState.DEAD:
                ExecuteDead();
                break;
        }
    }
    
    void ExecutePatrol()
    {
        if (detection.IsPlayerDetected())
        {
            ChangeState(BossState.CHASE);
        }
        else if (navigation != null && !navigation.IsPatrolling())
        {
            animator.SetBool("Walking", false);
            animator.SetBool("Running", false);
        }
    }
    
    void ExecuteChase()
    {
        if (detection.GetPlayer() != null)
        {
            navigation.StartChasing(detection.GetPlayer());
        }
        
        if (detection.IsPlayerInAttackRange() && combat.CanAttack())
        {
            ChangeState(BossState.ATTACK);
        }
        else if (!detection.IsPlayerDetected())
        {
            if (navigation.HasReachedDestination())
            {
                StartCoroutine(ReturnToPatrolAfterDelay(2f));
            }
        }
    }
    
    IEnumerator ReturnToPatrolAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (currentState == BossState.CHASE && !detection.IsPlayerDetected())
        {
            ChangeState(BossState.PATROL);
        }
    }
    
    void ExecuteAttack()
    {
        navigation.StopForAttack();
        
        if (!combat.IsAttacking())
        {
            combat.StartAttack();
            
            StartCoroutine(AfterAttackSequence(combat.attackDuration + 0.5f));
        }
    }
    
    IEnumerator AfterAttackSequence(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (currentState == BossState.ATTACK)
        {
            if (detection.IsPlayerDetected())
            {
                if (detection.IsPlayerInAttackRange() && combat.CanAttack())
                {
                    ExecuteAttack();
                }
                else
                {
                    ChangeState(BossState.CHASE);
                }
            }
            else
            {
                ChangeState(BossState.PATROL);
            }
        }
    }
    
    void ExecuteEscape()
    {
        Transform safestPoint = GetSafestPoint();
        if (safestPoint != null)
        {
            navigation.StartEscape(safestPoint);
        }
    }
    
    void ExecuteDead()
    {
        navigation.enabled = false;
        detection.enabled = false;
        combat.enabled = false;
        
        animator.SetBool("Dying", true);
        animator.SetBool("Walking", false);
        animator.SetBool("Running", false);
    }
    
    void CheckHealthBasedTransitions()
    {
        BossState newHealthState = GetStateByHealth();
        
        if (newHealthState != healthBasedState)
        {
            healthBasedState = newHealthState;
            
            StartCoroutine(TransitionWithEscape());
        }
    }
    
    BossState GetStateByHealth()
    {
        float healthPercentage = currentHealth / maxHealth;
        
        if (healthPercentage >= 0.7f)
            return BossState.PATROL; 
        else if (healthPercentage >= 0.3f)
            return BossState.CHASE;  
        else
            return BossState.CHASE;  
    }
    
    IEnumerator TransitionWithEscape()
    {
        isInTransition = true;
        
        ChangeState(BossState.ESCAPE);
        escapeTimer = escapeDuration;
        
        yield return new WaitForSeconds(escapeDuration);
        
        isInTransition = false;
        
        ChangeState(healthBasedState);
    }
    
    void EndEscapeState()
    {
        if (currentState == BossState.ESCAPE)
        {
            ChangeState(healthBasedState);
        }
    }
    
    Transform GetSafestPoint()
    {
        if (safePoints.Length == 0) 
        {
            Debug.LogWarning("Nenhum safe point configurado!");
            return null;
        }
        
        Transform safest = safePoints[0];
        float maxDistance = 0f;
        Transform player = detection.GetPlayer();
        
        if (player == null) return safePoints[0];
        
        foreach (Transform point in safePoints)
        {
            float distance = Vector3.Distance(player.position, point.position);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                safest = point;
            }
        }
        
        //Debug.Log($"Ponto seguro escolhido: {safest.name}, distância do player: {maxDistance}");
        return safest;
    }
    
    void ChangeState(BossState newState)
    {
        if (currentState == newState) return;
        
        Debug.Log($"Boss mudando de {currentState} para {newState} (Vida: {currentHealth}/{maxHealth})");
        
        ExitState(currentState);
        
        currentState = newState;
        
        EnterState(newState);
    }
    
    void EnterState(BossState state)
    {
        switch (state)
        {
            case BossState.PATROL:
                navigation.SetSpeed(behavior1Speed);
                navigation.StartPatrol();
                animator.SetBool("Running", false);
                animator.SetBool("Walking", true);
                break;
                
            case BossState.CHASE:
                float chaseSpeed = GetChaseSpeedByHealth();
                navigation.SetSpeed(chaseSpeed);
                animator.SetBool("Walking", false);
                animator.SetBool("Running", true);
                break;
                
            case BossState.ATTACK:
                animator.SetBool("Running", false);
                animator.SetBool("Walking", false);
                break;
                
            case BossState.ESCAPE:
                navigation.SetSpeed(escapeSpeed);
                animator.SetBool("Running", true);
                animator.SetBool("Walking", false);
                break;
                
            case BossState.DEAD:
                animator.SetBool("Dying", true);
                break;
        }
    }
    
    void ExitState(BossState state)
    {
        switch (state)
        {
            case BossState.ATTACK:
                navigation.ResumeMovement();
                break;
                
            case BossState.CHASE:
                break;
        }
    }
    
    float GetChaseSpeedByHealth()
    {
        float healthPercentage = currentHealth / maxHealth;
        
        if (healthPercentage >= 0.7f)
            return behavior1Speed;
        else if (healthPercentage >= 0.3f)
            return behavior2Speed;
        else
            return behavior3Speed;
    }
    
    public void TakeDamage(float damage)
    {
        if (currentState == BossState.DEAD) return;
        
        float oldHealth = currentHealth;
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        Debug.Log($"Boss levou {damage} de dano! Vida: {oldHealth} -> {currentHealth}");
        
        UpdateHealthBar();
        
        if (currentHealth <= 0)
        {
            ChangeState(BossState.DEAD);
        }
    }
    
    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
    }

}