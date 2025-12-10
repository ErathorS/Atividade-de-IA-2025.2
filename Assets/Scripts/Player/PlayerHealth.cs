using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
    
    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
        
        if (healthFillImage != null)
        {
            healthFillImage.color = fullHealthColor;
        }
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
        if (currentHealth <= 0) return;
        
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
        Debug.Log("PLAYER MORREU!");
        
        PlayerAnimationController controller = GetComponent<PlayerAnimationController>();
        if (controller != null)
        {
            controller.enabled = false;
        }
        
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Die");
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
    }
}
