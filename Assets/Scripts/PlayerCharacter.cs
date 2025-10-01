using UnityEngine;

/// <summary>
/// PlayerCharacter extends BaseCharacter with player-specific functionality
/// </summary>
public class PlayerCharacter : BaseCharacter
{
    [Header("Player Settings")]
    public float playerMaxHealth = 100f;
    public float playerContactDamage = 5f; // Lower contact damage for player
    
    [Header("Player Health Bar")]
    public Color playerHealthBarColor = Color.green;
    
    private PlayerController playerController;
    private FireballLauncher fireballLauncher;
    
    protected override void Start()
    {
        // Set player-specific values
        maxHealth = playerMaxHealth;
        contactDamage = playerContactDamage;
        
        // Get player components
        playerController = GetComponent<PlayerController>();
        fireballLauncher = GetComponent<FireballLauncher>();
        
        // Call base Start after setting values
        base.Start();
        
        // Subscribe to death event
        OnDeath += OnPlayerDeath;
    }
    
    protected override Color GetHealthBarColor()
    {
        return playerHealthBarColor;
    }
    
    void OnPlayerDeath()
    {
        // Disable player controls on death
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        if (fireballLauncher != null)
        {
            fireballLauncher.enabled = false;
        }
        
        // You can add game over logic here
        Debug.Log("Player has died! Game Over!");
    }
    
    public override void Die()
    {
        base.Die();
        
        // Additional player death logic can go here
        // For example: trigger game over screen, respawn logic, etc.
    }
    
    // Public methods for external systems
    public bool IsPlayerAlive()
    {
        return IsAlive();
    }
    
    public float GetPlayerHealthPercentage()
    {
        return GetHealthPercentage();
    }
}
