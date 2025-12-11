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
    private DamageFlash flash;
    
    [Header("Death Settings")]
    public float respawnDelay = 5f;
    
    private Animator animator;
    private bool isDead = false;
    
    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
        flash = GetComponent<DamageFlash>();
        if (healthFillImage != null)
        {
            healthFillImage.color = fullHealthColor;
        }
        
        animator = GetComponent<Animator>();
    }
    
    void Update()
    {
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
      //para visual no personagem
         if (flash != null)
        flash.Flash();

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
        
        if (animator != null)
        {
            animator.SetBool("IsDead", true);
        }
        
        PlayerAnimationController controller = GetComponent<PlayerAnimationController>();
        if (controller != null)
        {
            controller.enabled = false;
        }
        
        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat != null)
        {
            combat.enabled = false;
        }
        
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
        
        StartCoroutine(ResetSceneAfterDelay(respawnDelay));
    }
    
    IEnumerator ResetSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthBar();
    }
}