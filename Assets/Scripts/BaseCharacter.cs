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
    public GameObject healthBarPrefab; // Prefab for health bar UI
    public Vector3 healthBarOffset = new Vector3(0, 1.5f, 0); // Offset above character
    public float healthBarScale = 1f; // Scale of the health bar
    
    [Header("Damage Settings")]
    public float contactDamage = 10f; // Damage dealt on contact
    public float contactCooldown = 1f; // Time between contact damage
    public LayerMask damageLayers = -1; // What layers can be damaged by this character
    
    [Header("Visual Effects")]
    public GameObject deathEffect; // Effect to spawn on death
    public GameObject damageEffect; // Effect to spawn when taking damage
    public Color damageFlashColor = Color.red;
    public float damageFlashDuration = 0.1f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    // Private variables
    private GameObject healthBarInstance;
    private Slider healthBarSlider;
    private Image healthBarFill;
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
        
        // Create health bar UI
        CreateHealthBar();
        
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
        
        // Update health bar position
        if (healthBarInstance != null)
        {
            healthBarInstance.transform.position = transform.position + healthBarOffset;
        }
    }
    
    void CreateHealthBar()
    {
        if (healthBarPrefab == null)
        {
            // Create a simple health bar if no prefab is assigned
            CreateDefaultHealthBar();
        }
        else
        {
            // Use the assigned prefab
            healthBarInstance = Instantiate(healthBarPrefab, transform.position + healthBarOffset, Quaternion.identity);
            healthBarInstance.transform.SetParent(transform);
            healthBarInstance.transform.localScale = Vector3.one * healthBarScale;
            
            // Find the slider component
            healthBarSlider = healthBarInstance.GetComponentInChildren<Slider>();
            if (healthBarSlider != null)
            {
                healthBarFill = healthBarSlider.fillRect.GetComponent<Image>();
            }
        }
    }
    
    void CreateDefaultHealthBar()
    {
        // Create a simple health bar UI
        GameObject canvas = new GameObject("HealthBarCanvas");
        canvas.transform.SetParent(transform);
        canvas.transform.localPosition = healthBarOffset;
        canvas.transform.localScale = Vector3.one * healthBarScale;
        
        Canvas canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.WorldSpace;
        canvasComponent.sortingOrder = 10; // Above other sprites
        
        // Create slider
        GameObject sliderObj = new GameObject("HealthBar");
        sliderObj.transform.SetParent(canvas.transform);
        sliderObj.transform.localPosition = Vector3.zero;
        sliderObj.transform.localScale = Vector3.one;
        
        healthBarSlider = sliderObj.AddComponent<Slider>();
        healthBarSlider.minValue = 0f;
        healthBarSlider.maxValue = 1f;
        healthBarSlider.value = 1f;
        
        // Create background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObj.transform);
        background.transform.localPosition = Vector3.zero;
        background.transform.localScale = Vector3.one;
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // Create fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform);
        fillArea.transform.localPosition = Vector3.zero;
        fillArea.transform.localScale = Vector3.one;
        
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.anchoredPosition = Vector2.zero;
        
        // Create fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform);
        fill.transform.localPosition = Vector3.zero;
        fill.transform.localScale = Vector3.one;
        
        healthBarFill = fill.AddComponent<Image>();
        healthBarFill.color = GetHealthBarColor();
        
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        healthBarInstance = canvas;
    }
    
    protected virtual Color GetHealthBarColor()
    {
        // Override this in derived classes for different colors
        return Color.green;
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
        
        UpdateHealthBar();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} healed {amount}. Health: {currentHealth}/{maxHealth}");
        }
    }
    
    void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            float healthPercentage = currentHealth / maxHealth;
            healthBarSlider.value = healthPercentage;
            
            // Update color based on health
            if (healthBarFill != null)
            {
                healthBarFill.color = GetHealthBarColor();
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
        
        // Hide health bar
        if (healthBarInstance != null)
        {
            healthBarInstance.SetActive(false);
        }
        
        // Trigger death event
        OnDeath?.Invoke();
        
        if (showDebugInfo)
        {
            Debug.Log($"{gameObject.name} has died!");
        }
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    public bool IsAlive()
    {
        return !isDead && currentHealth > 0f;
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Handle contact damage
        if (contactDamage > 0f && Time.time - lastContactDamageTime >= contactCooldown)
        {
            BaseCharacter otherCharacter = other.GetComponent<BaseCharacter>();
            if (otherCharacter != null && otherCharacter != this)
            {
                // Check if we can damage this character
                if (((1 << other.gameObject.layer) & damageLayers) != 0)
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
    
    void OnDestroy()
    {
        // Clean up health bar
        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance);
        }
    }
}
