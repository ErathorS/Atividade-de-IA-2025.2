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
    
    // Componentes
    private BossNavigationSystem navigation;
    private BossDetection detection;
    private BossCombat combat;
    private Animator animator;
    
    // Estado atual
    private BossState currentState;
    private BossState healthBasedState;
    private float escapeTimer = 0f;
    private bool isInTransition = false;
    
    void Start()
    {
        // Inicializa componentes
        navigation = GetComponent<BossNavigationSystem>();
        detection = GetComponent<BossDetection>();
        combat = GetComponent<BossCombat>();
        animator = GetComponent<Animator>();
        
        // Verifica componentes
        if (navigation == null) Debug.LogError("BossNavigationSystem não encontrado!");
        if (detection == null) Debug.LogError("BossDetection não encontrado!");
        if (combat == null) Debug.LogError("BossCombat não encontrado!");
        if (animator == null) Debug.LogError("Animator não encontrado!");
        
        // Configuração inicial
        currentHealth = maxHealth;
        UpdateHealthBar();
        
        // Estado inicial baseado na saúde
        healthBasedState = GetStateByHealth();
        ChangeState(BossState.PATROL);
    }
    
    void Update()
    {
        // Atualiza barra de vida
        UpdateHealthBar();
        
        // Executa lógica do estado atual
        ExecuteCurrentState();
        
        // Verifica transições de saúde (exceto em estados temporários)
        if (!isInTransition && currentState != BossState.ESCAPE && 
            currentState != BossState.ATTACK && currentState != BossState.DEAD)
        {
            CheckHealthBasedTransitions();
        }
        
        // Contador para estado de fuga
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
        // Se detectou jogador, muda para perseguição
        if (detection.IsPlayerDetected())
        {
            ChangeState(BossState.CHASE);
        }
        // Se não há rota de patrulha configurada, fica parado
        else if (navigation != null && !navigation.IsPatrolling())
        {
            animator.SetBool("Walking", false);
            animator.SetBool("Running", false);
        }
    }
    
    void ExecuteChase()
    {
        // Persegue o jogador
        if (detection.GetPlayer() != null)
        {
            navigation.StartChasing(detection.GetPlayer());
        }
        
        // Verifica se pode atacar
        if (detection.IsPlayerInAttackRange() && combat.CanAttack())
        {
            ChangeState(BossState.ATTACK);
        }
        // Se perdeu o jogador, volta a patrulhar
        else if (!detection.IsPlayerDetected())
        {
            // Espera um pouco antes de voltar a patrulhar
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
        // Para o movimento
        navigation.StopForAttack();
        
        // Inicia ataque se não estiver atacando
        if (!combat.IsAttacking())
        {
            combat.StartAttack();
            
            // Após atacar, decide o próximo estado
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
                    // Continua atacando
                    ExecuteAttack();
                }
                else
                {
                    // Volta a perseguir
                    ChangeState(BossState.CHASE);
                }
            }
            else
            {
                // Volta a patrulhar
                ChangeState(BossState.PATROL);
            }
        }
    }
    
    void ExecuteEscape()
    {
        // Move para ponto seguro
        Transform safestPoint = GetSafestPoint();
        if (safestPoint != null)
        {
            navigation.StartEscape(safestPoint);
        }
    }
    
    void ExecuteDead()
    {
        // Desativa todos os sistemas
        navigation.enabled = false;
        detection.enabled = false;
        combat.enabled = false;
        
        // Animação de morte
        animator.SetBool("Dying", true);
        animator.SetBool("Walking", false);
        animator.SetBool("Running", false);
    }
    
    void CheckHealthBasedTransitions()
    {
        BossState newHealthState = GetStateByHealth();
        
        // Se a saúde mudou significativamente
        if (newHealthState != healthBasedState)
        {
            healthBasedState = newHealthState;
            
            // Inicia transição com fuga
            StartCoroutine(TransitionWithEscape());
        }
    }
    
    BossState GetStateByHealth()
    {
        float healthPercentage = currentHealth / maxHealth;
        
        if (healthPercentage >= 0.7f)
            return BossState.PATROL;  // Comportamento 1
        else if (healthPercentage >= 0.3f)
            return BossState.CHASE;   // Comportamento 2
        else
            return BossState.CHASE;   // Comportamento 3 (com velocidade aumentada)
    }
    
    IEnumerator TransitionWithEscape()
    {
        isInTransition = true;
        
        // Entra em estado de fuga
        ChangeState(BossState.ESCAPE);
        escapeTimer = escapeDuration;
        
        // Espera a duração da fuga
        yield return new WaitForSeconds(escapeDuration);
        
        // Sai do estado de transição
        isInTransition = false;
        
        // Vai para o estado baseado na saúde atual
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
        
        Debug.Log($"Ponto seguro escolhido: {safest.name}, distância do player: {maxDistance}");
        return safest;
    }
    
    void ChangeState(BossState newState)
    {
        if (currentState == newState) return;
        
        Debug.Log($"Boss mudando de {currentState} para {newState} (Vida: {currentHealth}/{maxHealth})");
        
        // Executa ações ao sair do estado atual
        ExitState(currentState);
        
        // Atualiza estado
        currentState = newState;
        
        // Executa ações ao entrar no novo estado
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
                // Para a perseguição
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