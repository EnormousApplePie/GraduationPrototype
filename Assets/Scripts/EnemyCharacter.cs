using UnityEngine;

/// <summary>
/// EnemyCharacter extends BaseCharacter with AI movement and enemy-specific behavior
/// </summary>
public class EnemyCharacter : BaseCharacter
{
    [Header("Enemy Settings")]
    public float enemyContactDamage = 15f; // Higher contact damage for enemies
    public float moveSpeed = 2f;
    public float detectionRange = 10f; // How far the enemy can detect the player
    
    [Header("Enemy Health Bar")]
    public Color enemyHealthBarColor = Color.red;
    
    
    [Header("AI Behavior")]
    public bool followPlayer = true;
    public float attackCooldown = 1.5f;
    public float shootingDistance = 8f; // Distance to maintain from target while shooting
    public string[] targetTags = {"Player", "Allied", "Friendly"}; // What to target
    
    [Header("Fireball Attack")]
    public GameObject fireballPrefab;
    public Transform fireballSpawnPoint;
    public float fireballSpeed = 15f;
    public float fireballLifetime = 3f;
    public float fireballSize = 1f;
    public float fireRate = 0.5f;
    
    private Transform currentTarget;
    private Rigidbody enemyRigidbody;
    private Vector3 lastKnownTargetPosition;
    private bool hasSeenTarget = false;
    private float lastShotTime;
    
    // For animation system
    private Vector2 currentMovementDirection = Vector2.zero;
    
    protected override void Start()
    {
        // Set enemy-specific values
        contactDamage = enemyContactDamage;
        damageableTags = new string[] {"Player", "Allied", "Friendly"}; // Enemies can damage player, allied, and friendly characters
        
        // Get rigidbody for movement
        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null)
        {
            enemyRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody
        enemyRigidbody.useGravity = true; // Enable gravity for enemies
        enemyRigidbody.freezeRotation = true;
        enemyRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        
        // Set fireball spawn point
        if (fireballSpawnPoint == null)
        {
            fireballSpawnPoint = transform;
        }
        
        // Call base Start after setting values
        // Base.Start() now automatically sets health bar color to red based on tag
        base.Start();
        
        // Subscribe to death event
        OnDeath += OnEnemyDeath;
    }
    
    void Update()
    {
        if (isDead) return;
        
        // Update AI behavior
        if (followPlayer)
        {
            UpdateAI();
        }
    }
    
    void UpdateAI()
    {
        // Find nearest target
        Transform nearestTarget = FindNearestTarget();
        
        if (nearestTarget != null && IsTargetAlive(nearestTarget))
        {
            currentTarget = nearestTarget;
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            
            if (distanceToTarget <= detectionRange)
            {
                hasSeenTarget = true;
                lastKnownTargetPosition = currentTarget.position;
                
                // Check if within shooting distance
                if (distanceToTarget <= shootingDistance)
                {
                    // Stop and shoot
                    StopMoving();
                    ShootAtTarget();
                }
                else
                {
                    // Move towards target
                    MoveTowardsTarget(currentTarget.position);
                }
            }
            else if (hasSeenTarget)
            {
                // Move towards last known position
                MoveTowardsTarget(lastKnownTargetPosition);
                
                // If we've reached the last known position, stop following
                if (Vector3.Distance(transform.position, lastKnownTargetPosition) < 1f)
                {
                    hasSeenTarget = false;
                    currentMovementDirection = Vector2.zero;
                }
            }
            else
            {
                // No target in range
                currentMovementDirection = Vector2.zero;
            }
        }
        else
        {
            // No valid target found
            currentTarget = null;
            currentMovementDirection = Vector2.zero;
        }
    }
    
    Transform FindNearestTarget()
    {
        Transform nearest = null;
        float nearestDistance = detectionRange;
        
        foreach (string tag in targetTags)
        {
            GameObject[] targets = GameObject.FindGameObjectsWithTag(tag);
            
            foreach (GameObject target in targets)
            {
                if (target == null) continue;
                
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance < nearestDistance)
                {
                    // Check if target is alive
                    BaseCharacter targetChar = target.GetComponent<BaseCharacter>();
                    if (targetChar != null && targetChar.IsAlive())
                    {
                        nearestDistance = distance;
                        nearest = target.transform;
                    }
                }
            }
        }
        
        return nearest;
    }
    
    bool IsTargetAlive(Transform target)
    {
        if (target == null) return false;
        
        BaseCharacter targetChar = target.GetComponent<BaseCharacter>();
        return targetChar != null && targetChar.IsAlive();
    }
    
    void StopMoving()
    {
        currentMovementDirection = Vector2.zero;
        if (enemyRigidbody != null)
        {
            enemyRigidbody.linearVelocity = new Vector3(0f, enemyRigidbody.linearVelocity.y, 0f);
        }
    }
    
    void ShootAtTarget()
    {
        if (currentTarget == null || fireballPrefab == null) return;
        
        // Check fire rate cooldown
        if (Time.time - lastShotTime < fireRate) return;
        
        // Calculate direction to target
        Vector3 directionToTarget = (currentTarget.position - fireballSpawnPoint.position).normalized;
        directionToTarget.y = 0f; // Keep fireballs horizontal
        
        // Spawn fireball
        GameObject fireball = Instantiate(fireballPrefab, fireballSpawnPoint.position, Quaternion.identity);
        
        // Configure the fireball
        Fireball fireballScript = fireball.GetComponent<Fireball>();
        if (fireballScript != null)
        {
            fireballScript.Initialize(directionToTarget, fireballSpeed, fireballLifetime, fireballSize);
            fireballScript.SetCollisionTags(new string[] {"Player", "Allied", "Friendly", "Wall", "Obstacle"});
            fireballScript.SetDamageableTags(new string[] {"Player", "Allied", "Friendly"});
        }
        
        // Orient the fireball
        if (directionToTarget != Vector3.zero)
        {
            fireball.transform.rotation = Quaternion.LookRotation(directionToTarget);
        }
        
        lastShotTime = Time.time;
    }
    
    void MoveTowardsTarget(Vector3 targetPosition)
    {
        // Calculate direction to target
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f; // Keep movement on horizontal plane
        
        // Store movement direction for animation system
        currentMovementDirection = new Vector2(direction.x, direction.z);
        
        // Move the enemy
        if (enemyRigidbody != null)
        {
            Vector3 movement = direction * moveSpeed * Time.deltaTime;
            enemyRigidbody.MovePosition(transform.position + movement);
        }
        else
        {
            // Fallback to transform movement
            transform.Translate(direction * moveSpeed * Time.deltaTime);
        }
        
        // Note: Rotation is handled by sprite animations, not transform rotation
    }
    
    
    void OnEnemyDeath()
    {
        // Disable AI movement
        if (enemyRigidbody != null)
        {
            enemyRigidbody.linearVelocity = Vector3.zero;
        }
        
        // You can add death effects, score, etc. here
        Debug.Log($"Enemy {gameObject.name} has died!");
        
        // Optional: Add score or other rewards
        // GameManager.Instance?.AddScore(10);
    }
    
    public override void Die()
    {
        base.Die();
        
        // Additional enemy death logic can go here
        // For example: drop items, spawn effects, etc.
    }
    
    // Public methods for external systems
    public bool IsEnemyAlive()
    {
        return IsAlive();
    }
    
    public float GetDistanceToPlayer()
    {
        if (currentTarget == null) return float.MaxValue;
        return Vector3.Distance(transform.position, currentTarget.position);
    }
    
    public bool CanSeePlayer()
    {
        if (currentTarget == null) return false;
        return GetDistanceToPlayer() <= detectionRange;
    }
    
    // Method for animation system to get movement direction
    public Vector2 GetMovementDirection()
    {
        return currentMovementDirection;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw shooting distance
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shootingDistance);
        
        // Draw line to current target if visible
        if (currentTarget != null && CanSeePlayer())
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}
