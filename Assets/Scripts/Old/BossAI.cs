using UnityEngine;
using UnityEngine.AI;

public class BossAI : MonoBehaviour
{
    // ===== COMPONENTES =====
    private NavMeshAgent agent;
    private Animator animator;
    private HealthSystem health;

    // ===== PARÂMETROS DE VIDA =====
    public float maxHealth = 1000f;
    private float currentHealth;

    // ===== ESTADOS DO BOSS =====
    public enum BossState
    {
        Behavior1,  // Vida ≥ 70%
        Behavior2,  // 30% ≤ Vida < 70%
        Behavior3,  // Vida ≤ 30%
        Escape      // Estado de transição
    }
    private BossState currentState;

    // ===== ALVOS E PONTOS DE NAVEGAÇÃO =====
    public Transform player;
    public Transform[] patrolPoints;
    public Transform safePoint; // Ponto seguro para fuga
    private int currentPatrolIndex = 0;

    // ===== TEMPORIZADORES =====
    private float escapeTimer = 0f;
    public float escapeDuration = 3f;

    // ===== CONFIGURAÇÕES POR ESTADO =====
    [Header("Configurações por Estado")]
    public float speedBehavior1 = 3f;
    public float speedBehavior2 = 5f;
    public float speedBehavior3 = 7f;
    public float escapeSpeed = 8f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<HealthSystem>();

        currentHealth = maxHealth;
        SetState(BossState.Behavior1);
    }

    void Update()
    {
        UpdateHealth();
        UpdateStateMachine();
        UpdateAnimations();
    }

        void UpdateHealth()
    {
        currentHealth = health.GetCurrentHealth();
    }

    void CheckHealthThreshold()
    {
        float healthPercentage = (currentHealth / maxHealth) * 100f;

        if (healthPercentage >= 70f && currentState != BossState.Behavior1)
        {
            SetState(BossState.Escape);
        }
        else if (healthPercentage >= 30f && healthPercentage < 70f && 
                 currentState != BossState.Behavior2)
        {
            SetState(BossState.Escape);
        }
        else if (healthPercentage < 30f && currentState != BossState.Behavior3)
        {
            SetState(BossState.Escape);
        }
    }
        void UpdateStateMachine()
    {
        CheckHealthThreshold();

        switch (currentState)
        {
            case BossState.Behavior1:
                ExecuteBehavior1();
                break;
            case BossState.Behavior2:
                ExecuteBehavior2();
                break;
            case BossState.Behavior3:
                ExecuteBehavior3();
                break;
            case BossState.Escape:
                ExecuteEscapeState();
                break;
        }
    }

    void SetState(BossState newState)
    {
        currentState = newState;
        OnStateEnter(newState);
    }

    void OnStateEnter(BossState state)
    {
        switch (state)
        {
            case BossState.Behavior1:
                agent.speed = speedBehavior1;
                break;
            case BossState.Behavior2:
                agent.speed = speedBehavior2;
                break;
            case BossState.Behavior3:
                agent.speed = speedBehavior3;
                break;
            case BossState.Escape:
                agent.speed = escapeSpeed;
                escapeTimer = escapeDuration;
                MoveToSafePoint();
                break;
        }
    }
        void ExecuteBehavior1()
    {
        // Patrulha com baixa agressividade
        Patrol();
        
        // Ataques esporádicos
        if (Vector3.Distance(transform.position, player.position) < 10f)
        {
            agent.SetDestination(player.position);
            if (Vector3.Distance(transform.position, player.position) < 3f)
            {
                animator.SetTrigger("Attack1");
            }
        }
    }

    void ExecuteBehavior2()
    {
        // Perseguição mais rápida com ataques especiais
        agent.SetDestination(player.position);
        
        if (Vector3.Distance(transform.position, player.position) < 4f)
        {
            animator.SetTrigger("Attack2");
        }
    }

    void ExecuteBehavior3()
    {
        // Comportamento agressivo e imprevisível
        Vector3 randomDirection = Random.insideUnitSphere * 5f;
        randomDirection += player.position;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }

        if (Vector3.Distance(transform.position, player.position) < 5f)
        {
            animator.SetTrigger("Attack3");
        }
    }

    void ExecuteEscapeState()
    {
        escapeTimer -= Time.deltaTime;
        
        if (escapeTimer <= 0f)
        {
            // Transição para o próximo estado baseado na vida
            float healthPercentage = (currentHealth / maxHealth) * 100f;
            
            if (healthPercentage >= 70f) SetState(BossState.Behavior1);
            else if (healthPercentage >= 30f) SetState(BossState.Behavior2);
            else SetState(BossState.Behavior3);
        }
    }
        void Patrol()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    void MoveToSafePoint()
    {
        if (safePoint != null)
        {
            agent.SetDestination(safePoint.position);
        }
    }

    // Método para pular entre áreas desconectadas (usando OffMeshLink)
    void HandleJump()
    {
        if (agent.isOnOffMeshLink)
        {
            animator.SetBool("IsJumping", true);
            // Lógica de animação de pulo pode ser implementada aqui
        }
        else
        {
            animator.SetBool("IsJumping", false);
        }
    }
        void UpdateAnimations()
    {
        // Controla animações baseadas na velocidade do NavMeshAgent
        float speed = agent.velocity.magnitude / agent.speed;
        animator.SetFloat("Speed", speed);
        
        // Ativa animação de ataque se estiver próximo do player
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        animator.SetBool("IsAttacking", distanceToPlayer < 4f);
        
        HandleJump();
    }

    // Método para evitar obstáculos dinâmicos
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DynamicObstacle"))
        {
            // Recalcula rota imediatamente
            agent.ResetPath();
            Vector3 newDestination = FindAlternativePath();
            agent.SetDestination(newDestination);
        }
    }

    Vector3 FindAlternativePath()
    {
        // Lógica para encontrar caminho alternativo
        Vector3 randomPoint = transform.position + Random.insideUnitSphere * 10f;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 10f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        
        return transform.position;
    }
}