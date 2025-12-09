using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class BossNavigationSystem : MonoBehaviour
{
    [System.Serializable]
    public class PatrolRoute
    {
        public Transform[] waypoints; // Ordem: [0] Plataforma3, [1] Plataforma2, [2] Plataforma1, [3] PontoDePulo
        public int jumpWaypointIndex = 3; // O último waypoint é o ponto de pulo
        public float waitTimeAtWaypoint = 2f;
    }
    
    [Header("Configurações Gerais")]
    public float patrolSpeed = 3.5f;
    public float chaseSpeed = 6f;
    public float escapeSpeed = 8f;
    public float attackStopDistance = 2f;
    
    [Header("Rota de Patrulha")]
    public PatrolRoute patrolRoute;
    
    [Header("Configuração de Pulo")]
    public float jumpCooldown = 5f;
    public float jumpAnimationDuration = 1.5f;
    public float jumpPrepareTime = 0.5f;
    
    // Componentes
    [HideInInspector] public NavMeshAgent agent;
    private Animator animator;
    
    // Estado
    private bool isPatrolling = false;
    private bool isChasing = false;
    private bool isAttacking = false;
    private bool isEscaping = false;
    private bool isJumping = false;
    private bool isWaiting = false;
    
    private Transform currentTarget;
    private int currentWaypointIndex = 0;
    private float lastJumpTime = 0f;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        if (agent != null)
        {
            agent.speed = patrolSpeed;
            agent.angularSpeed = 360f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.5f;
            agent.autoTraverseOffMeshLink = true;
        }
        
        if (patrolRoute != null && patrolRoute.waypoints != null && patrolRoute.waypoints.Length > 0)
        {
            StartPatrol();
        }
    }
    
    void Update()
    {
        if (isJumping || isWaiting || isAttacking) return;
        
        UpdateAnimation();
        
        if (isPatrolling && !isChasing && !isEscaping)
        {
            PatrolLogic();
        }
    }
    
    // ========================================
    // PATRULHA COM CICLO CORRETO
    // ========================================
    public void StartPatrol()
    {
        isPatrolling = true;
        isChasing = false;
        isEscaping = false;
        
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.5f;
        
        // Começa do waypoint 0 (Plataforma 3)
        currentWaypointIndex = 0;
        
        if (patrolRoute != null && patrolRoute.waypoints.Length > 0)
        {
            MoveToWaypoint(currentWaypointIndex);
            Debug.Log($"Patrulha iniciada no waypoint {currentWaypointIndex}: {patrolRoute.waypoints[currentWaypointIndex].name}");
        }
    }
    
    void PatrolLogic()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            Debug.Log($"Chegou no waypoint {currentWaypointIndex} ({patrolRoute.waypoints[currentWaypointIndex].name})");
            
            // Verifica se é o ponto de pulo
            if (currentWaypointIndex == patrolRoute.jumpWaypointIndex)
            {
                TryToJump();
            }
            else
            {
                StartCoroutine(WaitAtWaypoint());
            }
        }
    }
    
    void MoveToWaypoint(int index)
    {
        if (patrolRoute == null || patrolRoute.waypoints == null || 
            index >= patrolRoute.waypoints.Length || patrolRoute.waypoints[index] == null)
        {
            Debug.LogError($"Waypoint {index} inválido!");
            return;
        }
        
        currentWaypointIndex = index;
        currentTarget = patrolRoute.waypoints[index];
        
        Debug.Log($"Movendo para waypoint {index}: {currentTarget.name}");
        
        if (agent != null)
        {
            agent.SetDestination(currentTarget.position);
            agent.isStopped = false;
        }
    }
    
    void MoveToNextWaypoint()
    {
        // Lógica: 0→1→2→3(pulo)→0→1→2→3...
        int nextIndex = (currentWaypointIndex + 1) % patrolRoute.waypoints.Length;
        
        Debug.Log($"Saindo do waypoint {currentWaypointIndex}, indo para {nextIndex}");
        MoveToWaypoint(nextIndex);
    }
    
    IEnumerator WaitAtWaypoint()
    {
        isWaiting = true;
        animator.SetBool("Walking", false);
        
        Debug.Log($"Esperando {patrolRoute.waitTimeAtWaypoint} segundos no waypoint {currentWaypointIndex}");
        yield return new WaitForSeconds(patrolRoute.waitTimeAtWaypoint);
        
        MoveToNextWaypoint();
        isWaiting = false;
    }
    
    void TryToJump()
    {
        Debug.Log($"Chegou no ponto de pulo (waypoint {currentWaypointIndex})");
        
        if (Time.time < lastJumpTime + jumpCooldown)
        {
            Debug.Log("Pulo em cooldown, indo para próximo waypoint");
            // Se não pode pular, vai direto para o próximo waypoint
            MoveToNextWaypoint();
            return;
        }
        
        StartCoroutine(JumpSequence());
    }
    
    IEnumerator JumpSequence()
    {
        isJumping = true;
        
        // 1. Para completamente
        agent.isStopped = true;
        agent.ResetPath();
        
        // 2. Preparação para pulo
        Debug.Log("Preparando para pular...");
        animator.SetBool("Walking", false);
        animator.SetBool("Jumping", true);
        
        yield return new WaitForSeconds(jumpPrepareTime);
        
        // 3. Encontra o OffMeshLink mais próximo
        OffMeshLink jumpLink = FindNearestJumpLink();
        
        if (jumpLink != null && jumpLink.activated)
        {
            Debug.Log($"Pulando via OffMeshLink: {jumpLink.startTransform.name} -> {jumpLink.endTransform.name}");
            yield return StartCoroutine(TraverseOffMeshLink(jumpLink));
        }
        else
        {
            Debug.LogWarning("OffMeshLink não encontrado! Pulando manualmente...");
            yield return StartCoroutine(ManualJump());
        }
        
        // 4. Após o pulo, volta para o waypoint 0 (Plataforma 3)
        Debug.Log("Pulo concluído! Voltando para Plataforma 3 (waypoint 0)");
        
        animator.SetBool("Jumping", false);
        lastJumpTime = Time.time;
        
        // IMPORTANTE: Após pular, volta para o waypoint 0
        currentWaypointIndex = 0; // Reseta para começar do início
        MoveToWaypoint(0);
        
        isJumping = false;
        agent.isStopped = false;
    }
    
    OffMeshLink FindNearestJumpLink()
    {
        OffMeshLink[] allLinks = FindObjectsOfType<OffMeshLink>();
        OffMeshLink nearestLink = null;
        float nearestDistance = float.MaxValue;
        
        foreach (OffMeshLink link in allLinks)
        {
            if (!link.activated) continue;
            
            float distanceToStart = Vector3.Distance(transform.position, link.startTransform.position);
            
            if (distanceToStart < nearestDistance && distanceToStart < 5f)
            {
                nearestDistance = distanceToStart;
                nearestLink = link;
            }
        }
        
        return nearestLink;
    }
    
    IEnumerator TraverseOffMeshLink(OffMeshLink link)
    {
        float traverseTime = jumpAnimationDuration;
        float elapsedTime = 0f;
        Vector3 startPos = link.startTransform.position;
        Vector3 endPos = link.endTransform.position;
        
        Debug.Log($"Iniciando travessia do link: {startPos} -> {endPos}");
        
        while (elapsedTime < traverseTime)
        {
            float t = elapsedTime / traverseTime;
            
            // Movimento com curva de pulo (parábola)
            Vector3 newPos = Vector3.Lerp(startPos, endPos, t);
            float height = Mathf.Sin(t * Mathf.PI) * 2f; // Altura máxima no meio do pulo
            newPos.y += height;
            
            transform.position = newPos;
            
            // Olha na direção do movimento
            if (t > 0.1f && t < 0.9f)
            {
                Vector3 direction = (endPos - startPos).normalized;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Garante posição final exata
        transform.position = endPos;
        agent.Warp(endPos);
        
        Debug.Log($"Travessia concluída! Posição atual: {transform.position}");
    }
    
    IEnumerator ManualJump()
    {
        // Posição de destino após o pulo (waypoint 0 - Plataforma 3)
        Vector3 jumpTarget = patrolRoute.waypoints[0].position;
        Vector3 startPosition = transform.position;
        
        float jumpTime = jumpAnimationDuration;
        float elapsedTime = 0f;
        
        Debug.Log($"Pulando manualmente para: {jumpTarget}");
        
        while (elapsedTime < jumpTime)
        {
            float t = elapsedTime / jumpTime;
            
            // Curva de pulo suave
            Vector3 newPos = Vector3.Lerp(startPosition, jumpTarget, t);
            float height = Mathf.Sin(t * Mathf.PI) * 3f; // Pulo mais alto
            newPos.y += height;
            
            transform.position = newPos;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        transform.position = jumpTarget;
        agent.Warp(jumpTarget);
    }
    
    // ========================================
    // MÉTODOS PARA OUTROS ESTADOS (mantidos)
    // ========================================
    public void StartChasing(Transform player)
    {
        isChasing = true;
        isPatrolling = false;
        isWaiting = false;
        isJumping = false;
        
        StopAllCoroutines();
        
        agent.speed = chaseSpeed;
        agent.stoppingDistance = attackStopDistance;
        currentTarget = player;
        
        if (agent != null && player != null)
        {
            agent.SetDestination(player.position);
            agent.isStopped = false;
        }
    }
    
    public void StopChasing()
    {
        isChasing = false;
        StartPatrol();
    }
    
    public void StopForAttack()
    {
        agent.isStopped = true;
        isAttacking = true;
    }
    
    public void ResumeMovement()
    {
        agent.isStopped = false;
        isAttacking = false;
    }
    
    public void StartEscape(Transform safePoint)
    {
        isEscaping = true;
        isPatrolling = false;
        isChasing = false;
        isJumping = false;
        
        StopAllCoroutines();
        
        agent.speed = escapeSpeed;
        agent.stoppingDistance = 0.5f;
        
        if (safePoint != null)
        {
            agent.SetDestination(safePoint.position);
            agent.isStopped = false;
        }
    }
    
    void UpdateAnimation()
    {
        if (agent == null) return;
        
        if (isJumping)
        {
            return;
        }
        else if (isChasing && agent.velocity.magnitude > 0.1f)
        {
            animator.SetBool("Running", true);
            animator.SetBool("Walking", false);
            animator.SetBool("Jumping", false);
        }
        else if ((isPatrolling || isEscaping) && agent.velocity.magnitude > 0.1f)
        {
            animator.SetBool("Walking", true);
            animator.SetBool("Running", false);
            animator.SetBool("Jumping", false);
        }
        else if (!isWaiting)
        {
            animator.SetBool("Walking", false);
            animator.SetBool("Running", false);
            animator.SetBool("Jumping", false);
        }
    }
    
    public bool HasReachedDestination()
    {
        return agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending;
    }
    
    public bool IsChasing()
    {
        return isChasing;
    }
    
    public bool IsPatrolling()
    {
        return isPatrolling && !isWaiting && !isJumping;
    }
    
    public void SetSpeed(float speed)
    {
        agent.speed = speed;
    }
    
    public void SetStoppingDistance(float distance)
    {
        agent.stoppingDistance = distance;
    }
    
    // Método para debug da rota
    public string GetCurrentRouteInfo()
    {
        string info = $"Rota: ";
        for (int i = 0; i < patrolRoute.waypoints.Length; i++)
        {
            if (patrolRoute.waypoints[i] == null) continue;
            
            string waypointName = patrolRoute.waypoints[i].name;
            if (i == currentWaypointIndex)
                info += $"[{waypointName}] ";
            else if (i == patrolRoute.jumpWaypointIndex)
                info += $"({waypointName}) ";
            else
                info += $"{waypointName} ";
            
            if (i < patrolRoute.waypoints.Length - 1)
                info += "→ ";
        }
        info += $"(Próximo: {currentWaypointIndex})";
        
        return info;
    }
    
    void OnDrawGizmos()
    {
        if (patrolRoute == null || patrolRoute.waypoints == null) return;
        
        for (int i = 0; i < patrolRoute.waypoints.Length; i++)
        {
            if (patrolRoute.waypoints[i] == null) continue;
            
            // Cores diferentes para diferentes tipos de waypoints
            if (i == currentWaypointIndex)
            {
                Gizmos.color = Color.green; // Waypoint atual
            }
            else if (i == patrolRoute.jumpWaypointIndex)
            {
                Gizmos.color = Color.yellow; // Ponto de pulo
            }
            else
            {
                Gizmos.color = Color.cyan; // Waypoints normais
            }
            
            // Desenha o waypoint
            float size = (i == currentWaypointIndex) ? 0.6f : 0.4f;
            Gizmos.DrawSphere(patrolRoute.waypoints[i].position, size);
            
            // Desenha linha para o próximo waypoint
            int nextIndex = (i + 1) % patrolRoute.waypoints.Length;
            if (patrolRoute.waypoints[nextIndex] != null)
            {
                Gizmos.color = new Color(0, 1, 1, 0.5f);
                Gizmos.DrawLine(
                    patrolRoute.waypoints[i].position,
                    patrolRoute.waypoints[nextIndex].position
                );
            }
            
            // Rótulo
            #if UNITY_EDITOR
            string label = $"{i}: {patrolRoute.waypoints[i].name}";
            if (i == patrolRoute.jumpWaypointIndex) label += " (PULO)";
            if (i == currentWaypointIndex) label += " [ATUAL]";
            
            UnityEditor.Handles.Label(
                patrolRoute.waypoints[i].position + Vector3.up * 0.5f,
                label
            );
            #endif
        }
    }
}