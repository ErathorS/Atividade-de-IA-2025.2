using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class BossWinController : MonoBehaviour
{
    public BossStateMachine bossStateMachine;
    public GameObject winCanvas;
    public TMP_Text winMessageText;
    public GameObject restartButton;
    public GameObject quitButton;
    
    public float timeScaleOnWin = 0f;
    
    private bool bossDefeated = false;
    
    void Start()
    {
        if (winCanvas != null)
        {
            winCanvas.SetActive(false);
        }
        
        if (bossStateMachine == null)
        {
            bossStateMachine = FindObjectOfType<BossStateMachine>();
            if (bossStateMachine == null)
            {
                Debug.LogError("BossStateMachine não encontrado!");
            }
        }
    }
    
    void Update()
    {
        if (!bossDefeated && bossStateMachine != null && bossStateMachine.currentHealth <= 0)
        {
            BossDefeated();
        }
    }
    
    void BossDefeated()
    {
        bossDefeated = true;
        
        Time.timeScale = timeScaleOnWin;
        
        if (winCanvas != null)
        {
            winCanvas.SetActive(true);
        }
        
        DisablePlayerInput();
        
        Debug.Log("Boss derrotado! Jogo pausado.");
    }
    
    void DisablePlayerInput()
    {
        PlayerAnimationController playerMovement = FindObjectOfType<PlayerAnimationController>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
        
        PlayerCombat playerCombat = FindObjectOfType<PlayerCombat>();
        if (playerCombat != null)
        {
            playerCombat.enabled = false;
        }
        
        Rigidbody playerRb = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.isKinematic = true;
        }
    }
    
    void EnablePlayerInput()
    {
        PlayerAnimationController playerMovement = FindObjectOfType<PlayerAnimationController>();
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
        
        PlayerCombat playerCombat = FindObjectOfType<PlayerCombat>();
        if (playerCombat != null)
        {
            playerCombat.enabled = true;
        }
        
        Rigidbody playerRb = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.isKinematic = false;
        }
    }
    
    public void RestartGame()
    {
        Time.timeScale = 1f;
        
        EnablePlayerInput();
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        
        Debug.Log("Reiniciando jogo...");
    }
    
    public void QuitGame()
    {
        Time.timeScale = 1f;
        
        EnablePlayerInput();
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}