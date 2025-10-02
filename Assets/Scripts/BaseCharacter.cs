using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BaseCharacter provides health, damage, and UI functionality for all characters
/// </summary>
public class BaseCharacter : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isDead = false;
    
    [Header("Health Bar UI")]
    public Image healthBarFill; // Reference to the foreground health bar image
    public Color healthBarColor = Color.green; // Color of the health bar
    
    
    [Header("Damage Settings")]
    public float contactDamage = 10f; // Damage dealt on contact
    public float contactCooldown = 1f; // Time between contact damage
    public string[] damageableTags = {"Enemy"}; // What tags can be damaged by this character
    
    [Header("Visual Effects")]
    public GameObject deathEffect; // Effect to spawn on death
    public GameObject damageEffect; // Effect to spawn when taking damage
    public Color damageFlashColor = Color.red;
    public float damageFlashDuration = 0.1f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    // Private variables
    private float lastContactDamageTime = 0f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float damageFlashTimer = 0f;
    
    // Events
    public System.Action<float, float> OnHealthChanged; // currentHealth, maxHealth
    public System.Action OnDeath;
    public System.Action<float> OnDamageTaken; // damage amount
    
    protected virtual void Start()
    {
        // Initialize health
        currentHealth = maxHealth;
        isDead = false;
        
        // Get sprite renderer for damage flash
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Initialize health bar
        UpdateHealthBar();
        
    }
    
    void Update()
    {
        // Handle damage flash effect
        if (damageFlashTimer > 0f)
        {
            damageFlashTimer -= Time.deltaTime;
            if (damageFlashTimer <= 0f && spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
        
    }
    
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);
        
        // Trigger damage flash
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageFlashColor;
            damageFlashTimer = damageFlashDuration;
        }
        
        // Spawn damage effect
        if (damageEffect != null)
        {
            Instantiate(damageEffect, transform.position, Quaternion.identity);
        }
        
        // Update health bar
        UpdateHealthBar();
        
        // Trigger events
        OnDamageTaken?.Invoke(damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
        }
        
        // Check for death
        if (currentHealth <= 0f && !isDead)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        
        // Update health bar
        UpdateHealthBar();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} healed {amount}. Health: {currentHealth}/{maxHealth}");
        }
    }
    
    void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            float healthPercentage = currentHealth / maxHealth;
            healthBarFill.fillAmount = healthPercentage;
            
            // Update color based on health percentage
            if (healthPercentage > 0.6f)
            {
                healthBarFill.color = Color.green;
            }
            else if (healthPercentage > 0.3f)
            {
                healthBarFill.color = Color.yellow;
            }
            else
            {
                healthBarFill.color = Color.red;
            }
        }
    }
    
    public virtual void Die()
    {
        isDead = true;
        
        // Spawn death effect
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        
        // Hide health bar on death
        if (healthBarFill != null)
        {
            healthBarFill.gameObject.SetActive(false);
        }
        
        // Trigger death event
        OnDeath?.Invoke();
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} has died!");
        }
        
        // Destroy the character after a short delay to allow death effects to play
        Destroy(gameObject, 0.1f);
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    public bool IsAlive()
    {
        return !isDead && currentHealth > 0f;
    }
    
    bool CanDamageTarget(GameObject target)
    {
        // Get our tag and target tag
        string ourTag = gameObject.tag;
        string targetTag = target.tag;
        
        // Allied characters don't damage other Allied characters
        if (ourTag == "Allied" && targetTag == "Allied")
        {
            return false;
        }
        
        // Enemy characters don't damage other Enemy characters
        if (ourTag == "Enemy" && targetTag == "Enemy")
        {
            return false;
        }
        
        // Friendly characters don't damage other Friendly characters
        if (ourTag == "Friendly" && targetTag == "Friendly")
        {
            return false;
        }
        
        // Check if target tag is in our damageable tags list
        foreach (string damageableTag in damageableTags)
        {
            if (targetTag == damageableTag)
            {
                return true;
            }
        }
        
        return false;
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Handle contact damage
        if (contactDamage > 0f && Time.time - lastContactDamageTime >= contactCooldown)
        {
            BaseCharacter otherCharacter = other.GetComponent<BaseCharacter>();
            if (otherCharacter != null && otherCharacter != this)
            {
                // Check if we can damage this character based on tags
                if (CanDamageTarget(other.gameObject))
                {
                    otherCharacter.TakeDamage(contactDamage);
                    lastContactDamageTime = Time.time;
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"{gameObject.name} dealt {contactDamage} contact damage to {otherCharacter.gameObject.name}");
                    }
                }
            }
        }
    }
    
}
