using UnityEngine;
using System.Collections;

public class BossDetection : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRange = 20f;
    public float attackRange = 3f;
    public float fieldOfView = 90f;
    public float timeToDetect = 1f;
    public float timeToLoseSight = 3f;
    
    [Header("Layers")]
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;
    
    private Transform player;
    private float detectionTimer = 0f;
    private float lostSightTimer = 0f;
    private bool playerDetected = false;
    private bool playerInAttackRange = false;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }
    
    void Update()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Verifica se o player está no alcance
        if (distanceToPlayer <= detectionRange)
        {
            // Verifica se está no campo de visão
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
            
            if (angleToPlayer <= fieldOfView * 0.5f)
            {
                // Verifica se há obstáculos entre o boss e o player
                if (!Physics.Linecast(transform.position, player.position, obstacleLayer))
                {
                    detectionTimer += Time.deltaTime;
                    lostSightTimer = 0f;
                    
                    if (detectionTimer >= timeToDetect)
                    {
                        playerDetected = true;
                    }
                }
            }
            else
            {
                LostSight();
            }
        }
        else
        {
            LostSight();
        }
        
        // Verifica se está no alcance de ataque
        playerInAttackRange = distanceToPlayer <= attackRange;
    }
    
    void LostSight()
    {
        lostSightTimer += Time.deltaTime;
        
        if (lostSightTimer >= timeToLoseSight)
        {
            playerDetected = false;
            detectionTimer = 0f;
        }
    }
    
    public bool IsPlayerDetected()
    {
        return playerDetected;
    }
    
    public bool IsPlayerInAttackRange()
    {
        return playerInAttackRange;
    }
    
    public Transform GetPlayer()
    {
        return player;
    }
    
    public float GetDistanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;
        return Vector3.Distance(transform.position, player.position);
    }
    
    void OnDrawGizmosSelected()
    {
        // Gizmo de alcance de detecção
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Gizmo de alcance de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Gizmo de campo de visão
        Gizmos.color = Color.cyan;
        Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfView / 2, 0) * transform.forward * detectionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, fieldOfView / 2, 0) * transform.forward * detectionRange;
        
        Gizmos.DrawRay(transform.position, leftBoundary);
        Gizmos.DrawRay(transform.position, rightBoundary);
        Gizmos.DrawLine(transform.position + leftBoundary, transform.position + rightBoundary);
    }
}