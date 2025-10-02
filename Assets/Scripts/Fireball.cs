using UnityEngine;

/// <summary>
/// Fireball projectile that moves toward a target direction with configurable speed, size, and lifetime
/// </summary>
public class Fireball : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 10f;
    public Vector3 direction = Vector3.forward;
    
    [Header("Lifetime")]
    public float lifetime = 3f;
    public bool destroyOnCollision = true;
    
    [Header("Size")]
    public float size = 1f;
    public string sphereChildName = "Sphere"; // Name of the sphere child to scale
    
    [Header("Effects")]
    public GameObject impactEffect; // Optional impact VFX
    public string[] collisionTags = {"Enemy", "Wall", "Obstacle"}; // What tags the fireball collides with
    
    [Header("Damage")]
    public float baseDamage = 20f; // Base damage when not charged
    public float chargedDamageMultiplier = 2f; // Damage multiplier for charged fireballs
    public string[] damageableTags = {"Enemy"}; // What tags can be damaged
    
    private float timeAlive = 0f;
    private Rigidbody rb;
    private Transform sphereChild; // Reference to the sphere child object
    
    void Start()
    {
        Debug.Log($"Fireball spawned at {transform.position} with direction {direction} and speed {speed}");
        
        // Find the sphere child object to scale
        FindAndCacheSphereChild();
        
        // Apply size scaling to the sphere child
        ApplySizeScaling();
        
        // Get or add rigidbody for physics-based movement
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody
        rb.useGravity = false; // Fireballs don't fall
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement
        rb.isKinematic = false; // Ensure it's not kinematic so we can set velocity
        
        // Set initial velocity
        rb.linearVelocity = direction.normalized * speed;
        
        Debug.Log($"Fireball velocity set to {rb.linearVelocity}");
        
        // Auto-destroy after lifetime
        Destroy(gameObject, lifetime);
    }
    
    void FindAndCacheSphereChild()
    {
        // First try to find by name
        sphereChild = transform.Find(sphereChildName);
        
        // If not found by name, try to find any child with "Sphere" in the name
        if (sphereChild == null)
        {
            foreach (Transform child in transform)
            {
                if (child.name.ToLower().Contains("sphere"))
                {
                    sphereChild = child;
                    break;
                }
            }
        }
        
        // If still not found, try to find by component (MeshRenderer, etc.)
        if (sphereChild == null)
        {
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length > 0)
            {
                sphereChild = renderers[0].transform;
            }
        }
        
        if (sphereChild == null)
        {
            Debug.LogWarning($"Fireball: Could not find sphere child object '{sphereChildName}' in {gameObject.name}. Falling back to transform scaling.");
        }
    }
    
    void ApplySizeScaling()
    {
        if (sphereChild != null)
        {
            // Scale the sphere child object
            sphereChild.localScale = Vector3.one * size;
        }
        else
        {
            // Fallback: scale the entire transform if sphere child not found
            transform.localScale = Vector3.one * size;
        }
    }
    
    void Update()
    {
        timeAlive += Time.deltaTime;
        
        // Optional: Add rotation for visual effect
        transform.Rotate(0, 0, 360f * Time.deltaTime);
    }
    
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"🔥 FIREBALL HIT SOMETHING: {other.name} (tag: {other.tag})");
        
        // Check if we should collide with this object based on tags
        if (ShouldCollideWithTarget(other.gameObject))
        {
            Debug.Log($"✅ Fireball collision tag check passed for {other.name}");
            OnHit(other);
        }
        else
        {
            Debug.Log($"❌ Fireball collision tag check failed for {other.name} (tag {other.tag} not in collisionTags)");
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Check if we should collide with this object based on tags
        if (ShouldCollideWithTarget(collision.gameObject))
        {
            OnHit(collision.collider);
        }
    }
    
    void OnHit(Collider hitObject)
    {
        Debug.Log($"Fireball OnHit triggered with {hitObject.name} (tag: {hitObject.tag})");
        
        // Spawn impact effect if available
        if (impactEffect != null)
        {
            Instantiate(impactEffect, transform.position, Quaternion.identity);
        }
        
        // Deal damage to characters
        BaseCharacter character = hitObject.GetComponent<BaseCharacter>();
        if (character != null)
        {
            Debug.Log($"Found BaseCharacter component on {hitObject.name}");
            
            // Check if we can damage this character based on tags
            if (CanDamageTarget(hitObject.gameObject))
            {
                float damage = CalculateDamage();
                character.TakeDamage(damage);
                
                Debug.Log($"Fireball hit {hitObject.name} for {damage} damage!");
            }
            else
            {
                Debug.Log($"Fireball cannot damage {hitObject.name} (tag: {hitObject.tag}) - not in damageable tags");
            }
        }
        else
        {
            Debug.Log($"No BaseCharacter component found on {hitObject.name}");
        }
        
        if (destroyOnCollision)
        {
            Destroy(gameObject);
        }
    }
    
    bool ShouldCollideWithTarget(GameObject target)
    {
        // Get target tag
        string targetTag = target.tag;
        
        // Check if target tag is in our collision tags list
        foreach (string collisionTag in collisionTags)
        {
            if (targetTag == collisionTag)
            {
                return true;
            }
        }
        
        return false;
    }
    
    bool CanDamageTarget(GameObject target)
    {
        // Get target tag
        string targetTag = target.tag;
        
        Debug.Log($"CanDamageTarget check: target={target.name}, tag={targetTag}, damageableTags={string.Join(",", damageableTags)}");
        
        // Check if target tag is in our damageable tags list
        foreach (string damageableTag in damageableTags)
        {
            if (targetTag == damageableTag)
            {
                Debug.Log($"CanDamageTarget: YES - {targetTag} matches {damageableTag}");
                return true;
            }
        }
        
        Debug.Log($"CanDamageTarget: NO - {targetTag} not in damageable tags");
        return false;
    }
    
    float CalculateDamage()
    {
        // Calculate damage based on size (charge level)
        float chargeLevel = size / 1f; // Normalize size to charge level
        float damage = baseDamage * (1f + (chargeLevel - 1f) * chargedDamageMultiplier);
        return damage;
    }
    
    // Public methods for external configuration
    public void Initialize(Vector3 shootDirection, float projectileSpeed, float projectileLifetime, float projectileSize)
    {
        direction = shootDirection;
        speed = projectileSpeed;
        lifetime = projectileLifetime;
        size = projectileSize;
        
        // Apply the new size immediately if already started
        if (sphereChild != null || Application.isPlaying)
        {
            if (sphereChild == null) FindAndCacheSphereChild();
            ApplySizeScaling();
        }
    }
    
    // Public method to update size during runtime (for charge attacks)
    public void SetSize(float newSize)
    {
        size = newSize;
        ApplySizeScaling();
    }
    
    public void SetCollisionTags(string[] tags)
    {
        collisionTags = tags;
    }
    
    public void SetDamageableTags(string[] tags)
    {
        damageableTags = tags;
    }
} 