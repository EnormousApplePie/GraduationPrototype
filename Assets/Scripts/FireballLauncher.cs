using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// FireballLauncher allows the player to shoot fireballs toward the mouse cursor using left mouse click
/// </summary>
public class FireballLauncher : MonoBehaviour
{
    [Header("Fireball Settings")]
    public GameObject fireballPrefab; // Drag your Fireball VFX prefab here
    public Transform fireballSpawnPoint; // Where fireballs spawn from (optional, defaults to player center)
    
    [Header("Fireball Configuration")]
    public float fireballSpeed = 15f;
    public float fireballLifetime = 3f;
    public float fireballSize = 1f;
    
    [Header("Charge Attack")]
    public bool enableChargeAttack = true;
    public float chargeTime = 2f; // Time to reach max charge
    public float maxChargedSize = 3f; // Maximum size when fully charged
    public float minChargeTime = 0.2f; // Minimum hold time before charging starts
    public float chargedSpeedMultiplier = 0.8f; // Charged fireballs are slightly slower but bigger
    
    [Header("Spawn Position")]
    public float spawnDistanceInFront = 1.5f; // How far in front of player to spawn fireball
    public bool usePlayerFacing = true; // Use player's facing direction for spawn positioning
    
    [Header("Shooting")]
    public float fireRate = 0.5f; // Time between shots (seconds)
    public LayerMask fireballCollisionLayers = -1; // What the fireball can hit
    
    [Header("Mouse Targeting")]
    public Camera targetCamera; // Camera for mouse-to-world conversion (auto-finds main camera)
    public float targetDistance = 10f; // How far ahead to aim when no ground is hit
    public bool force2DMovement = true; // Force fireballs to move only on X-Z plane (horizontal)
    public float fireballHeight = 0f; // Y-height offset for fireball travel (0 = ground level)
    
    [Header("Debug")]
    public bool showDebugRay = true;
    public bool enableDebugLog = false;
    
    private float lastShotTime = 0f;
    private InputAction shootAction;
    private Vector3 lastTargetDirection = Vector3.forward;
    
    // Charge attack variables
    private bool isCharging = false;
    private float chargeStartTime = 0f;
    private GameObject chargingFireball = null;
    private PlayerAnimationController playerAnimController;
    
    // Player facing direction
    private Vector3 playerFacingDirection = Vector3.forward;
    
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
        
        // Auto-set spawn point if not assigned
        if (fireballSpawnPoint == null)
        {
            fireballSpawnPoint = transform;
        }
        
        // Get player animation controller for facing direction
        playerAnimController = GetComponent<PlayerAnimationController>();
        if (playerAnimController == null)
        {
            playerAnimController = GetComponentInChildren<PlayerAnimationController>();
        }
        
        // Setup input action for shooting
        SetupInputActions();
        
        // Validate fireball prefab
        if (fireballPrefab == null)
        {
            Debug.LogWarning("FireballLauncher: No fireball prefab assigned! Please drag your Fireball VFX prefab to the FireballPrefab field.");
        }
    }
    
    void SetupInputActions()
    {
        // Create input action for left mouse click shooting
        shootAction = new InputAction("Shoot", binding: "<Mouse>/leftButton");
        shootAction.started += OnShootStarted;   // Mouse button pressed down
        shootAction.canceled += OnShootCanceled; // Mouse button released
        shootAction.Enable();
    }
    
    void Update()
    {
        // Update target direction based on mouse position
        UpdateTargetDirection();
        
        // Update player facing direction
        UpdatePlayerFacingDirection();
        
        // Handle charge attack logic
        if (isCharging && enableChargeAttack)
        {
            UpdateChargeAttack();
        }
        
        // Draw debug ray
        if (showDebugRay && targetCamera != null)
        {
            Vector3 spawnPos = GetFireballSpawnPosition();
            Debug.DrawRay(spawnPos, lastTargetDirection * 5f, Color.red, 0.1f);
            
            // Draw facing direction ray
            if (usePlayerFacing)
            {
                Debug.DrawRay(fireballSpawnPoint.position, playerFacingDirection * 3f, Color.blue, 0.1f);
            }
        }
    }
    
    void UpdateTargetDirection()
    {
        if (targetCamera == null) return;
        
        // Get mouse position in screen space
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        
        if (force2DMovement)
        {
            // Project mouse ray onto the ground plane (X-Z) for 2D movement
            Ray cameraRay = targetCamera.ScreenPointToRay(mousePosition);
            
            // Create a horizontal plane at the fireball's height
            float planeY = fireballSpawnPoint.position.y + fireballHeight;
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, planeY, 0));
            
            // Find where the camera ray intersects the ground plane
            if (groundPlane.Raycast(cameraRay, out float distance))
            {
                // Get the intersection point on the ground plane
                Vector3 targetPoint = cameraRay.GetPoint(distance);
                
                // Calculate direction from spawn point to target (only X-Z, no Y)
                Vector3 direction = targetPoint - fireballSpawnPoint.position;
                direction.y = 0f; // Force horizontal movement
                
                lastTargetDirection = direction.normalized;
                
                if (enableDebugLog)
                {
                    Debug.Log($"2D Mouse targeting: {targetPoint} | Direction: {lastTargetDirection}");
                }
            }
            else
            {
                // Fallback: use camera forward direction projected onto X-Z plane
                Vector3 fallbackDirection = targetCamera.transform.forward;
                fallbackDirection.y = 0f;
                lastTargetDirection = fallbackDirection.normalized;
                
                if (enableDebugLog)
                {
                    Debug.Log($"2D Fallback direction: {lastTargetDirection}");
                }
            }
        }
        else
        {
            // Original 3D targeting code (for reference)
            Ray cameraRay = targetCamera.ScreenPointToRay(mousePosition);
            
            // Try to hit the ground/objects
            if (Physics.Raycast(cameraRay, out RaycastHit hit))
            {
                // Aim toward the hit point
                Vector3 directionToTarget = (hit.point - fireballSpawnPoint.position).normalized;
                lastTargetDirection = directionToTarget;
                
                if (enableDebugLog)
                {
                    Debug.Log($"3D Mouse targeting hit: {hit.point} | Direction: {directionToTarget}");
                }
            }
            else
            {
                // No hit - aim forward at a fixed distance
                Vector3 worldPoint = cameraRay.origin + cameraRay.direction * targetDistance;
                Vector3 directionToTarget = (worldPoint - fireballSpawnPoint.position).normalized;
                lastTargetDirection = directionToTarget;
                
                if (enableDebugLog)
                {
                    Debug.Log($"3D Mouse targeting no hit - using direction: {directionToTarget}");
                }
            }
        }
    }
    
    void OnShootStarted(InputAction.CallbackContext context)
    {
        if (enableChargeAttack)
        {
            StartChargeAttack();
        }
        else
        {
            ShootFireball();
        }
    }
    
    void OnShootCanceled(InputAction.CallbackContext context)
    {
        if (enableChargeAttack && isCharging)
        {
            ReleaseChargeAttack();
        }
    }
    
    void UpdatePlayerFacingDirection()
    {
        if (!usePlayerFacing) return;
        
        // Get facing direction from player animation controller
        if (playerAnimController != null)
        {
            int currentDirection = playerAnimController.GetCurrentDirection();
            
            // Convert animation direction to world direction
            switch (currentDirection)
            {
                case 0: playerFacingDirection = Vector3.back; break;    // Down
                case 1: playerFacingDirection = new Vector3(-1, 0, -1).normalized; break; // DownLeft
                case 2: playerFacingDirection = Vector3.left; break;    // Left
                case 3: playerFacingDirection = new Vector3(-1, 0, 1).normalized; break;  // UpLeft
                case 4: playerFacingDirection = Vector3.forward; break; // Up
                case 5: playerFacingDirection = new Vector3(1, 0, 1).normalized; break;   // UpRight
                case 6: playerFacingDirection = Vector3.right; break;   // Right
                case 7: playerFacingDirection = new Vector3(1, 0, -1).normalized; break;  // DownRight
                default: playerFacingDirection = Vector3.forward; break;
            }
        }
    }
    
    Vector3 GetFireballSpawnPosition()
    {
        Vector3 basePosition = fireballSpawnPoint.position;
        
        if (usePlayerFacing)
        {
            // Spawn fireball in front of where player is facing
            basePosition += playerFacingDirection * spawnDistanceInFront;
        }
        
        if (force2DMovement)
        {
            basePosition.y = fireballSpawnPoint.position.y + fireballHeight;
        }
        
        return basePosition;
    }
    
    void StartChargeAttack()
    {
        // Check fire rate cooldown
        if (Time.time - lastShotTime < fireRate)
        {
            return;
        }
        
        if (fireballPrefab == null)
        {
            Debug.LogWarning("Cannot start charge attack - no prefab assigned!");
            return;
        }
        
        isCharging = true;
        chargeStartTime = Time.time;
        
        // Create charging fireball
        Vector3 spawnPosition = GetFireballSpawnPosition();
        chargingFireball = Instantiate(fireballPrefab, spawnPosition, Quaternion.identity);
        
        // Configure charging fireball (stationary)
        Fireball fireballScript = chargingFireball.GetComponent<Fireball>();
        if (fireballScript != null)
        {
            fireballScript.Initialize(Vector3.zero, 0f, chargeTime + 1f, fireballSize); // Stationary
        }
        
        // Remove or disable rigidbody movement for charging fireball
        Rigidbody rb = chargingFireball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true; // Make it kinematic so we can move it manually
        }
        
        if (enableDebugLog)
        {
            Debug.Log("Started charging fireball attack");
        }
    }
    
    void UpdateChargeAttack()
    {
        if (chargingFireball == null) return;
        
        float chargeProgress = Mathf.Clamp01((Time.time - chargeStartTime) / chargeTime);
        
        // Only start growing after minimum charge time
        if (Time.time - chargeStartTime >= minChargeTime)
        {
            float currentSize = Mathf.Lerp(fireballSize, maxChargedSize, chargeProgress);
            
            // Use the Fireball's SetSize method to properly scale the sphere child
            Fireball fireballScript = chargingFireball.GetComponent<Fireball>();
            if (fireballScript != null)
            {
                fireballScript.SetSize(currentSize);
            }
            else
            {
                // Fallback to transform scaling if no Fireball script
                chargingFireball.transform.localScale = Vector3.one * currentSize;
            }
        }
        
        // Move charging fireball to stay in front of player
        chargingFireball.transform.position = GetFireballSpawnPosition();
        
        // Orient fireball toward target
        if (lastTargetDirection != Vector3.zero)
        {
            chargingFireball.transform.rotation = Quaternion.LookRotation(lastTargetDirection);
        }
    }
    
    void ReleaseChargeAttack()
    {
        if (chargingFireball == null)
        {
            isCharging = false;
            return;
        }
        
        float chargeProgress = Mathf.Clamp01((Time.time - chargeStartTime) / chargeTime);
        float finalSize = Mathf.Lerp(fireballSize, maxChargedSize, chargeProgress);
        float finalSpeed = fireballSpeed * chargedSpeedMultiplier;
        
        // Configure the fireball for shooting
        Fireball fireballScript = chargingFireball.GetComponent<Fireball>();
        if (fireballScript != null)
        {
            fireballScript.Initialize(lastTargetDirection, finalSpeed, fireballLifetime, finalSize);
            fireballScript.SetCollisionLayers(fireballCollisionLayers);
        }
        
        // Re-enable physics movement
        Rigidbody rb = chargingFireball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = lastTargetDirection * finalSpeed;
        }
        
        // Reset charging state
        isCharging = false;
        chargingFireball = null;
        lastShotTime = Time.time;
        
        if (enableDebugLog)
        {
            Debug.Log($"Released charged fireball! Size: {finalSize:F1}, Speed: {finalSpeed:F1}, Charge: {chargeProgress:P0}");
        }
    }
    
    public void ShootFireball()
    {
        // Check fire rate cooldown
        if (Time.time - lastShotTime < fireRate)
        {
            return; // Still cooling down
        }
        
        if (fireballPrefab == null)
        {
            Debug.LogWarning("Cannot shoot fireball - no prefab assigned!");
            return;
        }
        
        // Spawn fireball at the correct position
        Vector3 spawnPosition = GetFireballSpawnPosition();
        
        GameObject fireball = Instantiate(fireballPrefab, spawnPosition, Quaternion.identity);
        
        // Configure the fireball
        Fireball fireballScript = fireball.GetComponent<Fireball>();
        if (fireballScript != null)
        {
            fireballScript.Initialize(lastTargetDirection, fireballSpeed, fireballLifetime, fireballSize);
            fireballScript.SetCollisionLayers(fireballCollisionLayers);
        }
        else
        {
            // Fallback: Add Fireball script if the prefab doesn't have one
            fireballScript = fireball.AddComponent<Fireball>();
            fireballScript.Initialize(lastTargetDirection, fireballSpeed, fireballLifetime, fireballSize);
            fireballScript.SetCollisionLayers(fireballCollisionLayers);
        }
        
        // Orient the fireball to face the direction it's moving
        if (lastTargetDirection != Vector3.zero)
        {
            fireball.transform.rotation = Quaternion.LookRotation(lastTargetDirection);
        }
        
        // Update cooldown
        lastShotTime = Time.time;
        
        if (enableDebugLog)
        {
            Debug.Log($"Fireball shot! Direction: {lastTargetDirection}, Speed: {fireballSpeed}");
        }
    }
    
    // Public methods for external control
    public void SetFireballSpeed(float speed)
    {
        fireballSpeed = speed;
    }
    
    public void SetFireballSize(float size)
    {
        fireballSize = size;
    }
    
    public void SetFireballLifetime(float lifetime)
    {
        fireballLifetime = lifetime;
    }
    
    public void SetFireRate(float rate)
    {
        fireRate = rate;
    }
    
    void OnDestroy()
    {
        // Clean up input actions
        if (shootAction != null)
        {
            shootAction.started -= OnShootStarted;
            shootAction.canceled -= OnShootCanceled;
            shootAction.Disable();
        }
        
        // Clean up any remaining charging fireball
        if (chargingFireball != null)
        {
            Destroy(chargingFireball);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw targeting gizmos in scene view
        if (fireballSpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(fireballSpawnPoint.position, 0.2f);
            
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(fireballSpawnPoint.position, lastTargetDirection * 3f);
            }
        }
    }
} 