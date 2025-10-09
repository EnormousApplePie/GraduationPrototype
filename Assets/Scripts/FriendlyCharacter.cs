using UnityEngine;

/// <summary>
/// FriendlyCharacter is an AI-controlled ally that wanders, detects enemies, and attacks them
/// </summary>
public class FriendlyCharacter : BaseCharacter
{
    [Header("Friendly AI Settings")]
    public float detectionRange = 15f;
    public float shootingDistance = 8f;
    public float wanderRadius = 10f;
    public float waypointReachedDistance = 1f;
    public float friendlyMoveSpeed = 5f;
    
    [Header("Fireball Attack")]
    public GameObject fireballPrefab;
    public Transform fireballSpawnPoint;
    public float fireballSpeed = 15f;
    public float fireballLifetime = 3f;
    public float fireballSize = 1f;
    public float fireRate = 0.5f;
    
    [Header("AI Behavior")]
    public string[] enemyTags = {"Enemy"};
    public float newWaypointDelay = 2f;
    
    private enum AIState { Wandering, Chasing, Attacking }
    private AIState currentState = AIState.Wandering;
    
    private Transform enemyTarget;
    private Vector3 currentWaypoint;
    private Vector3 spawnPosition;
    private float lastShotTime;
    private float waypointTimer;
    private Rigidbody friendlyRigidbody;
    
    // For animation system
    private Vector2 currentMovementDirection = Vector2.zero;
    private PlayerAnimationController animationController;
    
    protected override void Start()
    {
        // Set friendly-specific values
        damageableTags = new string[] {"Enemy"}; // Friendlies attack enemies
        
        // Get rigidbody
        friendlyRigidbody = GetComponent<Rigidbody>();
        if (friendlyRigidbody == null)
        {
            friendlyRigidbody = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody
        friendlyRigidbody.useGravity = true;
        friendlyRigidbody.freezeRotation = true;
        friendlyRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        
        // Set fireball spawn point
        if (fireballSpawnPoint == null)
        {
            fireballSpawnPoint = transform;
        }
        
        // Get animation controller
        animationController = GetComponentInChildren<PlayerAnimationController>();
        if (animationController == null)
        {
            Debug.LogWarning($"[FriendlyCharacter] No PlayerAnimationController found on {gameObject.name}");
        }
        
        // Call base Start
        base.Start();
        
        // Store spawn position for wandering
        spawnPosition = transform.position;
        ChooseNewWaypoint();
        
        // Subscribe to death event
        OnDeath += OnFriendlyDeath;
    }
    
    void Update()
    {
        if (isDead) return;
        
        UpdateAI();
    }
    
    void UpdateAI()
    {
        // Look for nearest enemy
        Transform nearestEnemy = FindNearestEnemy();
        
        if (nearestEnemy != null && IsEnemyAlive(nearestEnemy))
        {
            float distanceToEnemy = Vector3.Distance(transform.position, nearestEnemy.position);
            
            if (distanceToEnemy <= detectionRange)
            {
                enemyTarget = nearestEnemy;
                
                if (distanceToEnemy <= shootingDistance)
                {
                    // Attack state
                    currentState = AIState.Attacking;
                    HandleAttacking();
                }
                else
                {
                    // Chase state
                    currentState = AIState.Chasing;
                    HandleChasing();
                }
                return;
            }
        }
        
        // No enemy in range, wander
        enemyTarget = null;
        currentState = AIState.Wandering;
        HandleWandering();
    }
    
    void HandleWandering()
    {
        float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint);
        
        if (distanceToWaypoint < waypointReachedDistance)
        {
            // Reached waypoint, wait before choosing new one
            StopMoving();
            waypointTimer += Time.deltaTime;
            
            if (waypointTimer >= newWaypointDelay)
            {
                ChooseNewWaypoint();
                waypointTimer = 0f;
            }
        }
        else
        {
            // Move towards waypoint
            MoveTowards(currentWaypoint);
        }
    }
    
    void HandleChasing()
    {
        if (enemyTarget != null)
        {
            MoveTowards(enemyTarget.position);
        }
    }
    
    void HandleAttacking()
    {
        // Stop moving and shoot
        StopMoving();
        ShootAtEnemy();
    }
    
    void ChooseNewWaypoint()
    {
        // Choose random point within wander radius
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        currentWaypoint = spawnPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }
    
    Transform FindNearestEnemy()
    {
        Transform nearest = null;
        float nearestDistance = detectionRange;
        
        foreach (string tag in enemyTags)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(tag);
            
            foreach (GameObject enemy in enemies)
            {
                if (enemy == null) continue;
                
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    EnemyCharacter enemyChar = enemy.GetComponent<EnemyCharacter>();
                    if (enemyChar != null && enemyChar.IsAlive())
                    {
                        nearestDistance = distance;
                        nearest = enemy.transform;
                    }
                }
            }
        }
        
        return nearest;
    }
    
    bool IsEnemyAlive(Transform enemy)
    {
        if (enemy == null) return false;
        
        EnemyCharacter enemyChar = enemy.GetComponent<EnemyCharacter>();
        return enemyChar != null && enemyChar.IsAlive();
    }
    
    void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f; // Keep movement horizontal
        
        if (friendlyRigidbody != null)
        {
            Vector3 newVelocity = new Vector3(direction.x * friendlyMoveSpeed, friendlyRigidbody.linearVelocity.y, direction.z * friendlyMoveSpeed);
            friendlyRigidbody.linearVelocity = newVelocity;
        }
        
        // Update animation direction
        currentMovementDirection = new Vector2(direction.x, direction.z);
        if (animationController != null)
        {
            animationController.SetMovementInput(currentMovementDirection);
        }
    }
    
    void StopMoving()
    {
        currentMovementDirection = Vector2.zero;
        
        if (friendlyRigidbody != null)
        {
            friendlyRigidbody.linearVelocity = new Vector3(0f, friendlyRigidbody.linearVelocity.y, 0f);
        }
        
        if (animationController != null)
        {
            animationController.SetMovementInput(Vector2.zero);
        }
    }
    
    void ShootAtEnemy()
    {
        if (enemyTarget == null || fireballPrefab == null) return;
        
        // Check fire rate cooldown
        if (Time.time - lastShotTime < fireRate) return;
        
        // Calculate direction to enemy
        Vector3 directionToEnemy = (enemyTarget.position - fireballSpawnPoint.position).normalized;
        directionToEnemy.y = 0f; // Keep fireballs horizontal
        
        // Spawn fireball
        GameObject fireball = Instantiate(fireballPrefab, fireballSpawnPoint.position, Quaternion.identity);
        
        // Configure the fireball
        Fireball fireballScript = fireball.GetComponent<Fireball>();
        if (fireballScript != null)
        {
            fireballScript.Initialize(directionToEnemy, fireballSpeed, fireballLifetime, fireballSize);
            fireballScript.SetCollisionTags(new string[] {"Enemy", "Wall", "Obstacle"});
            fireballScript.SetDamageableTags(new string[] {"Enemy"});
        }
        
        // Orient the fireball
        if (directionToEnemy != Vector3.zero)
        {
            fireball.transform.rotation = Quaternion.LookRotation(directionToEnemy);
        }
        
        lastShotTime = Time.time;
    }
    
    void OnFriendlyDeath()
    {
        // Cleanup on death
        StopMoving();
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw shooting distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shootingDistance);
        
        // Draw wander radius
        Vector3 spawnPos = Application.isPlaying ? spawnPosition : transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnPos, wanderRadius);
        
        // Draw current waypoint
        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(currentWaypoint, 0.5f);
            Gizmos.DrawLine(transform.position, currentWaypoint);
        }
        
        // Draw line to enemy target
        if (Application.isPlaying && enemyTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, enemyTarget.position);
        }
    }
}

