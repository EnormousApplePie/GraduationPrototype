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
    public LayerMask collisionLayers = -1; // What layers the fireball collides with
    
    private float timeAlive = 0f;
    private Rigidbody rb;
    private Transform sphereChild; // Reference to the sphere child object
    
    void Start()
    {
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
        
        // Set initial velocity
        rb.linearVelocity = direction.normalized * speed;
        
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
        // Check if we should collide with this object
        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            OnHit(other);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Check if we should collide with this object
        if (((1 << collision.gameObject.layer) & collisionLayers) != 0)
        {
            OnHit(collision.collider);
        }
    }
    
    void OnHit(Collider hitObject)
    {
        // Spawn impact effect if available
        if (impactEffect != null)
        {
            Instantiate(impactEffect, transform.position, Quaternion.identity);
        }
        
        // Optional: Add damage logic here
        // Example: hitObject.GetComponent<Health>()?.TakeDamage(damage);
        
        if (destroyOnCollision)
        {
            Destroy(gameObject);
        }
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
    
    public void SetCollisionLayers(LayerMask layers)
    {
        collisionLayers = layers;
    }
} 