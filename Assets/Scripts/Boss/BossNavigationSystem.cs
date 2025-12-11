using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class BossNavigationSystem : MonoBehaviour
{
    [System.Serializable]
    public class PatrolRoute
    {
        public Transform[] waypoints;
        public int jumpWaypointIndex = 3; 
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
  
    public void StartPatrol()
    {
        isPatrolling = true;
        isChasing = false;
        isEscaping = false;
        
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.5f;
        
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
            MoveToNextWaypoint();
            return;
        }
        
        StartCoroutine(JumpSequence());
    }
    
    IEnumerator JumpSequence()
    {
        isJumping = true;
        
        agent.isStopped = true;
        agent.ResetPath();
        
        Debug.Log("Preparando para pular...");
        animator.SetBool("Walking", false);
        animator.SetBool("Jumping", true);
        
        yield return new WaitForSeconds(jumpPrepareTime);
        
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
        
        Debug.Log("Pulo concluído! Voltando para Plataforma 3 (waypoint 0)");
        
        animator.SetBool("Jumping", false);
        lastJumpTime = Time.time;
        
        currentWaypointIndex = 0; 
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
            
            Vector3 newPos = Vector3.Lerp(startPos, endPos, t);
            float height = Mathf.Sin(t * Mathf.PI) * 2f;
            newPos.y += height;
            
            transform.position = newPos;
            
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
        
        transform.position = endPos;
        agent.Warp(endPos);
        
        Debug.Log($"Travessia concluída! Posição atual: {transform.position}");
    }
    
    IEnumerator ManualJump()
    {
        Vector3 jumpTarget = patrolRoute.waypoints[0].position;
        Vector3 startPosition = transform.position;
        
        float jumpTime = jumpAnimationDuration;
        float elapsedTime = 0f;
        
        Debug.Log($"Pulando manualmente para: {jumpTarget}");
        
        while (elapsedTime < jumpTime)
        {
            float t = elapsedTime / jumpTime;
            
            Vector3 newPos = Vector3.Lerp(startPosition, jumpTarget, t);
            float height = Mathf.Sin(t * Mathf.PI) * 3f;
            newPos.y += height;
            
            transform.position = newPos;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        transform.position = jumpTarget;
        agent.Warp(jumpTarget);
    }

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
            
            if (i == currentWaypointIndex)
            {
                Gizmos.color = Color.green; 
            }
            else if (i == patrolRoute.jumpWaypointIndex)
            {
                Gizmos.color = Color.yellow; 
            }
            else
            {
                Gizmos.color = Color.cyan; 
            }
            
            float size = (i == currentWaypointIndex) ? 0.6f : 0.4f;
            Gizmos.DrawSphere(patrolRoute.waypoints[i].position, size);
            
            int nextIndex = (i + 1) % patrolRoute.waypoints.Length;
            if (patrolRoute.waypoints[nextIndex] != null)
            {
                Gizmos.color = new Color(0, 1, 1, 0.5f);
                Gizmos.DrawLine(
                    patrolRoute.waypoints[i].position,
                    patrolRoute.waypoints[nextIndex].position
                );
            }
            
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