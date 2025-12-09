using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class AITarget_Boss : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // Player
    public float attackDistance = 3f;
    public float chaseDistance = 15f;
    
    [Header("Boss Parameters")]
    public float normalSpeed = 3.5f;
    public float fastSpeed = 5f;
    public float rageSpeed = 7f;
    
    [Header("Attack Settings")]
    public float attackCooldown = 2f;
    public int attackDamage = 20;
    
    private NavMeshAgent m_Agent;
    private Animator m_Animator;
    private float m_Distance;
    private float lastAttackTime = 0f;
    private Vector3 m_StartingPoint;
    private bool m_PathCalculated = true;
    
    // Referência para o script de estados do Boss
    private BossStateController stateController;

    void Start()
    {
        m_Agent = GetComponent<NavMeshAgent>();
        m_Animator = GetComponent<Animator>();
        stateController = GetComponent<BossStateController>();
        
        m_StartingPoint = transform.position;
        
        // Configuração inicial do NavMesh Agent
        m_Agent.speed = normalSpeed;
        m_Agent.angularSpeed = 360f;
        m_Agent.acceleration = 8f;
        m_Agent.stoppingDistance = 2f;
    }

    void Update()
    {
        if (target == null) return;
        
        m_Distance = Vector3.Distance(transform.position, target.position);
        
        // Verifica se deve atacar
        if (m_Distance < attackDistance && Time.time > lastAttackTime + attackCooldown)
        {
            Attack();
        }
        
        // Verifica se deve perseguir
        if (m_Distance < chaseDistance && m_Distance > attackDistance)
        {
            Chase();
        }
        else if (m_Distance >= chaseDistance)
        {
            PatrolOrIdle();
        }
    }
    
    void Attack()
    {
        m_Agent.isStopped = true;
        m_Animator.SetBool("Attack", true);
        lastAttackTime = Time.time;
        
        // Causa dano ao player
        PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
        }
        
        // Retoma movimento após ataque
        StartCoroutine(ResumeMovement(1.5f));
    }
    
    void Chase()
    {
        m_Agent.isStopped = false;
        m_Animator.SetBool("Attack", false);
        m_Animator.SetBool("Running", true);
        m_Agent.destination = target.position;
    }
    
    void PatrolOrIdle()
    {
        m_Agent.isStopped = false;
        m_Animator.SetBool("Attack", false);
        m_Animator.SetBool("Running", false);
        
        // Volta para ponto inicial ou patrulha
        if (!m_Agent.hasPath && m_PathCalculated)
        {
            m_Agent.destination = m_StartingPoint;
            m_PathCalculated = false;
        }
        else if (m_Agent.remainingDistance < 0.5f)
        {
            m_PathCalculated = true;
        }
    }
    
    IEnumerator ResumeMovement(float delay)
    {
        yield return new WaitForSeconds(delay);
        m_Agent.isStopped = false;
        m_Animator.SetBool("Attack", false);
    }
    
    // Método para mudar velocidade baseado no estado
    public void UpdateSpeed(float newSpeed)
    {
        m_Agent.speed = newSpeed;
    }
}