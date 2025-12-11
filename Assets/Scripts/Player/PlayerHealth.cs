using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 500f;
    public float currentHealth;
    public Slider healthBar;
    
    [Header("Feedback Visual")]
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    public Image healthFillImage;
    
    [Header("Death Settings")]
    public float respawnDelay = 5f;
    
    private Animator animator;
    private bool isDead = false;
    
    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
        
        if (healthFillImage != null)
        {
            healthFillImage.color = fullHealthColor;
        }
        
        animator = GetComponent<Animator>();
    }
    
    void Update()
    {
        // Debug danificar
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            TakeDamage(50f);
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0 || isDead) return;
        
        float oldHealth = currentHealth;
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"Player levou {damage} de dano! Vida: {oldHealth} -> {currentHealth}");
        
        UpdateHealthBar();
        
        if (healthFillImage != null)
        {
            float percentage = currentHealth / maxHealth;
            healthFillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, percentage);
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
    }
    
    void Die()
    {
        isDead = true;
        Debug.Log("PLAYER MORREU!");
        
        // Ativa a animação de morte
        if (animator != null)
        {
            animator.SetBool("IsDead", true);
        }
        
        // Desativa o controlador de animação
        PlayerAnimationController controller = GetComponent<PlayerAnimationController>();
        if (controller != null)
        {
            controller.enabled = false;
        }
        
        // Desativa o script de combate, se existir
        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat != null)
        {
            combat.enabled = false;
        }
        
        // Desativa o movimento
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        
        // Inicia a rotina para resetar a cena após 5 segundos
        StartCoroutine(ResetSceneAfterDelay(respawnDelay));
    }
    
    IEnumerator ResetSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Recarrega a cena atual
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    // Método opcional para curar o jogador
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthBar();
    }
}