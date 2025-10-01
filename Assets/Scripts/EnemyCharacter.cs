using UnityEngine;

/// <summary>
/// EnemyCharacter extends BaseCharacter with AI movement and enemy-specific behavior
/// </summary>
public class EnemyCharacter : BaseCharacter
{
    [Header("Enemy Settings")]
    public float enemyMaxHealth = 50f;
    public float enemyContactDamage = 15f; // Higher contact damage for enemies
    public float moveSpeed = 2f;
    public float detectionRange = 10f; // How far the enemy can detect the player
    
    [Header("Enemy Health Bar")]
    public Color enemyHealthBarColor = Color.red;
    
    [Header("AI Behavior")]
    public bool followPlayer = true;
    public float attackCooldown = 1.5f;
    
    private Transform playerTarget;
    // Removed unused lastAttackTime field - attack cooldown is handled by contact damage system
    private Rigidbody enemyRigidbody;
    private Vector3 lastKnownPlayerPosition;
    private bool hasSeenPlayer = false;
    
    protected override void Start()
    {
        // Set enemy-specific values
        maxHealth = enemyMaxHealth;
        contactDamage = enemyContactDamage;
        
        // Get rigidbody for movement
        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null)
        {
            enemyRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody
        enemyRigidbody.useGravity = false;
        enemyRigidbody.freezeRotation = true;
        enemyRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        
        // Find player target
        FindPlayerTarget();
        
        // Call base Start after setting values
        base.Start();
        
        // Subscribe to death event
        OnDeath += OnEnemyDeath;
    }
    
    void Update()
    {
        if (isDead) return;
        
        // Update AI behavior
        if (followPlayer && playerTarget != null)
        {
            UpdateAI();
        }
    }
    
    void FindPlayerTarget()
    {
        // Try to find player by tag first
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
            return;
        }
        
        // Fallback: find PlayerCharacter component
        PlayerCharacter playerCharacter = FindFirstObjectByType<PlayerCharacter>();
        if (playerCharacter != null)
        {
            playerTarget = playerCharacter.transform;
        }
    }
    
    void UpdateAI()
    {
        if (playerTarget == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        
        // Check if player is in detection range
        if (distanceToPlayer <= detectionRange)
        {
            hasSeenPlayer = true;
            lastKnownPlayerPosition = playerTarget.position;
            
            // Move towards player
            MoveTowardsTarget(playerTarget.position);
        }
        else if (hasSeenPlayer)
        {
            // Move towards last known position
            MoveTowardsTarget(lastKnownPlayerPosition);
            
            // If we've reached the last known position, stop following
            if (Vector3.Distance(transform.position, lastKnownPlayerPosition) < 1f)
            {
                hasSeenPlayer = false;
            }
        }
    }
    
    void MoveTowardsTarget(Vector3 targetPosition)
    {
        // Calculate direction to target
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f; // Keep movement on horizontal plane
        
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
        
        // Optional: Rotate enemy to face movement direction
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.up);
        }
    }
    
    protected override Color GetHealthBarColor()
    {
        return enemyHealthBarColor;
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
        if (playerTarget == null) return float.MaxValue;
        return Vector3.Distance(transform.position, playerTarget.position);
    }
    
    public bool CanSeePlayer()
    {
        if (playerTarget == null) return false;
        return GetDistanceToPlayer() <= detectionRange;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw line to player if visible
        if (playerTarget != null && CanSeePlayer())
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTarget.position);
        }
    }
}
