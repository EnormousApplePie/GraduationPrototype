using UnityEngine;

/// <summary>
/// EnemySpawner handles spawning enemies off-screen and managing enemy population
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab; // Enemy prefab to spawn
    public float spawnInterval = 3f; // Time between spawns
    public int maxEnemies = 10; // Maximum number of enemies at once
    public float spawnDistance = 15f; // How far off-screen to spawn
    
    [Header("Spawn Area")]
    public float spawnHeight = 0f; // Y position for spawning
    public bool spawnOnLeft = true; // Spawn on left side
    public bool spawnOnRight = true; // Spawn on right side
    public float spawnVariation = 2f; // Random variation in spawn position
    
    [Header("Camera Reference")]
    public Camera targetCamera; // Camera to determine screen bounds
    public float screenMargin = 2f; // Extra margin beyond screen edge
    
    [Header("Debug")]
    public bool showSpawnAreas = true;
    public bool enableDebugLog = false;
    
    private float lastSpawnTime = 0f;
    private int currentEnemyCount = 0;
    private Transform playerTarget;
    
    void Start()
    {
        // Auto-find camera if not assigned
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindFirstObjectByType<Camera>();
            }
        }
        
        // Find player target
        FindPlayerTarget();
        
        // Validate enemy prefab
        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: No enemy prefab assigned! Please assign an enemy prefab.");
        }
    }
    
    void Update()
    {
        // Update enemy count
        UpdateEnemyCount();
        
        // Check if we should spawn a new enemy
        if (ShouldSpawnEnemy())
        {
            SpawnEnemy();
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
    
    void UpdateEnemyCount()
    {
        // Count active enemies
        EnemyCharacter[] enemies = FindObjectsByType<EnemyCharacter>(FindObjectsSortMode.None);
        currentEnemyCount = 0;
        
        foreach (EnemyCharacter enemy in enemies)
        {
            if (enemy != null && enemy.IsEnemyAlive())
            {
                currentEnemyCount++;
            }
        }
    }
    
    bool ShouldSpawnEnemy()
    {
        // Check if we have room for more enemies
        if (currentEnemyCount >= maxEnemies)
        {
            return false;
        }
        
        // Check spawn interval
        if (Time.time - lastSpawnTime < spawnInterval)
        {
            return false;
        }
        
        // Check if player exists
        if (playerTarget == null)
        {
            return false;
        }
        
        return true;
    }
    
    void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("Cannot spawn enemy - no prefab assigned!");
            return;
        }
        
        // Determine spawn side
        bool spawnLeft = Random.Range(0f, 1f) < 0.5f;
        
        // Check if the chosen side is enabled
        if (spawnLeft && !spawnOnLeft)
        {
            spawnLeft = false;
        }
        else if (!spawnLeft && !spawnOnRight)
        {
            spawnLeft = true;
        }
        
        // Calculate spawn position
        Vector3 spawnPosition = CalculateSpawnPosition(spawnLeft);
        
        // Spawn the enemy
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        
        // Configure the enemy
        EnemyCharacter enemyCharacter = newEnemy.GetComponent<EnemyCharacter>();
        if (enemyCharacter == null)
        {
            // Add EnemyCharacter component if not present
            enemyCharacter = newEnemy.AddComponent<EnemyCharacter>();
        }
        
        // Subscribe to enemy death to update count
        enemyCharacter.OnDeath += OnEnemyDeath;
        
        // Update spawn time
        lastSpawnTime = Time.time;
        currentEnemyCount++;
        
        if (enableDebugLog)
        {
            Debug.Log($"Spawned enemy at {spawnPosition}. Total enemies: {currentEnemyCount}");
        }
    }
    
    Vector3 CalculateSpawnPosition(bool spawnLeft)
    {
        if (targetCamera == null || playerTarget == null)
        {
            // Fallback spawn position
            Vector3 fallbackPos = playerTarget != null ? playerTarget.position : Vector3.zero;
            fallbackPos.x += spawnLeft ? -spawnDistance : spawnDistance;
            fallbackPos.y = spawnHeight;
            return fallbackPos;
        }
        
        // Get camera bounds
        float cameraHeight = targetCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * targetCamera.aspect;
        
        // Calculate spawn position relative to camera
        Vector3 cameraPos = targetCamera.transform.position;
        Vector3 playerPos = playerTarget.position;
        
        float spawnX;
        if (spawnLeft)
        {
            // Spawn to the left of the screen
            spawnX = cameraPos.x - (cameraWidth / 2f) - spawnDistance - screenMargin;
        }
        else
        {
            // Spawn to the right of the screen
            spawnX = cameraPos.x + (cameraWidth / 2f) + spawnDistance + screenMargin;
        }
        
        // Add random variation
        spawnX += Random.Range(-spawnVariation, spawnVariation);
        
        // Use player's Z position for depth
        float spawnZ = playerPos.z;
        
        return new Vector3(spawnX, spawnHeight, spawnZ);
    }
    
    void OnEnemyDeath()
    {
        // Enemy death is handled by UpdateEnemyCount, but we can add additional logic here
        if (enableDebugLog)
        {
            Debug.Log("Enemy died, updating count...");
        }
    }
    
    // Public methods for external control
    public void SetSpawnInterval(float interval)
    {
        spawnInterval = interval;
    }
    
    public void SetMaxEnemies(int max)
    {
        maxEnemies = max;
    }
    
    public int GetCurrentEnemyCount()
    {
        return currentEnemyCount;
    }
    
    public void ForceSpawnEnemy()
    {
        if (currentEnemyCount < maxEnemies)
        {
            SpawnEnemy();
        }
    }
    
    public void ClearAllEnemies()
    {
        EnemyCharacter[] enemies = FindObjectsByType<EnemyCharacter>(FindObjectsSortMode.None);
        foreach (EnemyCharacter enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.TakeDamage(enemy.maxHealth); // Kill instantly
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showSpawnAreas || targetCamera == null) return;
        
        // Draw spawn areas
        Gizmos.color = Color.red;
        
        float cameraHeight = targetCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * targetCamera.aspect;
        Vector3 cameraPos = targetCamera.transform.position;
        
        // Left spawn area
        if (spawnOnLeft)
        {
            Vector3 leftSpawnPos = new Vector3(
                cameraPos.x - (cameraWidth / 2f) - spawnDistance - screenMargin,
                spawnHeight,
                cameraPos.z
            );
            Gizmos.DrawWireCube(leftSpawnPos, new Vector3(2f, 2f, 2f));
        }
        
        // Right spawn area
        if (spawnOnRight)
        {
            Vector3 rightSpawnPos = new Vector3(
                cameraPos.x + (cameraWidth / 2f) + spawnDistance + screenMargin,
                spawnHeight,
                cameraPos.z
            );
            Gizmos.DrawWireCube(rightSpawnPos, new Vector3(2f, 2f, 2f));
        }
    }
}
