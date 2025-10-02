using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// RTS-style movement controller that moves the player to clicked positions
/// and handles auto-firing at enemies when in range
/// </summary>
public class RTSMovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float stoppingDistance = 0.5f; // How close to get to target before stopping
    
    [Header("Combat Settings")]
    public float shootingDistance = 8f; // Distance to maintain from enemies
    public float autoFireRate = 0.5f; // Time between auto-fire shots
    public string[] enemyTags = {"Enemy"}; // What tags count as enemies
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool showTargetIndicator = true;
    
    private Vector3 targetPosition;
    private bool hasTarget = false;
    private Transform currentEnemyTarget;
    private float lastAutoFireTime = 0f;
    private Rigidbody rb;
    private FireballLauncher fireballLauncher;
    private LineRenderer targetLine; // For showing target line
    private PlayerAnimationController animationController;
    
    void Start()
    {
        // Get components
        rb = GetComponent<Rigidbody>();
        fireballLauncher = GetComponent<FireballLauncher>();
        
        // Try to find PlayerAnimationController on this object or children
        animationController = GetComponent<PlayerAnimationController>();
        if (animationController == null)
        {
            animationController = GetComponentInChildren<PlayerAnimationController>();
        }
        if (animationController == null)
        {
            animationController = GetComponentInParent<PlayerAnimationController>();
        }
        
        // If still null, try to find by searching all objects
        if (animationController == null)
        {
            PlayerAnimationController[] allAnimators = FindObjectsOfType<PlayerAnimationController>();
            if (allAnimators.Length > 0)
            {
                // Find the one that's closest to this object
                float closestDistance = float.MaxValue;
                foreach (PlayerAnimationController anim in allAnimators)
                {
                    float distance = Vector3.Distance(transform.position, anim.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        animationController = anim;
                    }
                }
            }
        }
        
        // Setup rigidbody for movement
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = true;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        // Create target line renderer
        if (showTargetIndicator)
        {
            CreateTargetLine();
        }
    }
    
    void Update()
    {
        HandleMouseInput();
        HandleMovement();
        HandleAutoFire();
        UpdateTargetLine();
    }
    
    void HandleMouseInput()
    {
        // This is now handled by SelectionManager
        // The RTSMovementController no longer handles input directly
    }
    
    Vector3 GetMouseWorldPosition()
    {
        Camera cam = Camera.main;
        if (cam == null) 
        {
            cam = FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                return Vector3.zero;
            }
        }
        
        // Get mouse position in screen space (same as FireballLauncher)
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        
        // Use the exact same logic as FireballLauncher for 2D movement
        Ray cameraRay = cam.ScreenPointToRay(mousePosition);
        
        // Create a horizontal plane at the player's height (same as fireball height)
        float planeY = transform.position.y;
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, planeY, 0));
        
        // Find where the camera ray intersects the ground plane
        if (groundPlane.Raycast(cameraRay, out float distance))
        {
            // Get the intersection point on the ground plane
            Vector3 targetPoint = cameraRay.GetPoint(distance);
            
            // Force horizontal movement (only X-Z, no Y)
            targetPoint.y = transform.position.y;
            
            return targetPoint;
        }
        else
        {
            // Fallback: use camera forward direction projected onto X-Z plane
            Vector3 fallbackDirection = cam.transform.forward;
            fallbackDirection.y = 0f;
            Vector3 fallbackPoint = transform.position + fallbackDirection * 5f; // 5 units ahead
            
            return fallbackPoint;
        }
    }
    
    
    Transform GetEnemyAtMouse()
    {
        Camera cam = Camera.main;
        if (cam == null) 
        {
            cam = FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                return null;
            }
        }
        
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);
        
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            // Check if hit object has an enemy tag
            if (IsEnemyTag(hit.collider.gameObject.tag))
            {
                return hit.transform;
            }
        }
        
        return null;
    }
    
    public void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
        hasTarget = true;
    }
    
    public void SetEnemyTarget(Transform enemy)
    {
        currentEnemyTarget = enemy;
        
        // Set target position to move towards the enemy
        if (enemy != null)
        {
            SetTargetPosition(enemy.position);
        }
    }
    
    public void ClearEnemyTarget()
    {
        currentEnemyTarget = null;
    }
    
    bool IsEnemyTag(string tag)
    {
        foreach (string enemyTag in enemyTags)
        {
            if (tag == enemyTag)
            {
                return true;
            }
        }
        
        return false;
    }
    
    void HandleMovement()
    {
        if (!hasTarget) return;
        
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);
        
        // Check if we have an enemy target and are within shooting range
        if (currentEnemyTarget != null)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, currentEnemyTarget.position);
            
            // If we're within shooting range of the enemy, stop moving
            if (distanceToEnemy <= shootingDistance)
            {
                hasTarget = false;
                rb.linearVelocity = Vector3.zero;
                
                // Update animation controller with no movement
                if (animationController != null)
                {
                    animationController.SetMovementInput(Vector2.zero);
                }
                return;
            }
        }
        
        // Stop if close enough to target (normal movement)
        if (distance <= stoppingDistance)
        {
            hasTarget = false;
            rb.linearVelocity = Vector3.zero;
            
            // Update animation controller with no movement
            if (animationController != null)
            {
                animationController.SetMovementInput(Vector2.zero);
            }
            return;
        }
        
        // Move towards target
        Vector3 movement = direction * moveSpeed;
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
        
        // Update animation controller with movement direction
        if (animationController != null)
        {
            // Convert 3D direction to 2D for animation
            Vector2 animDirection = new Vector2(direction.x, direction.z);
            animationController.SetMovementInput(animDirection);
        }
    }
    
    
    void HandleAutoFire()
    {
        if (currentEnemyTarget == null) return;
        if (fireballLauncher == null) return;
        
        float distanceToEnemy = Vector3.Distance(transform.position, currentEnemyTarget.position);
        
        // Check if enemy is in shooting range
        if (distanceToEnemy <= shootingDistance)
        {
            // Auto-fire at enemy
            if (Time.time - lastAutoFireTime >= autoFireRate)
            {
                Vector3 fireDirection = (currentEnemyTarget.position - transform.position).normalized;
                fireballLauncher.ShootFireballInDirection(fireDirection);
                lastAutoFireTime = Time.time;
                
            }
        }
    }
    
    void CreateTargetLine()
    {
        GameObject lineObj = new GameObject("TargetLine");
        lineObj.transform.SetParent(transform);
        targetLine = lineObj.AddComponent<LineRenderer>();
        targetLine.material = new Material(Shader.Find("Sprites/Default"));
        targetLine.material.color = Color.red;
        targetLine.startWidth = 0.1f;
        targetLine.endWidth = 0.1f;
        targetLine.positionCount = 2;
        targetLine.enabled = false;
    }
    
    void UpdateTargetLine()
    {
        if (targetLine == null) return;
        
        if (hasTarget)
        {
            targetLine.enabled = true;
            targetLine.SetPosition(0, transform.position);
            targetLine.SetPosition(1, targetPosition);
        }
        else if (currentEnemyTarget != null)
        {
            targetLine.enabled = true;
            targetLine.material.color = Color.yellow;
            targetLine.SetPosition(0, transform.position);
            targetLine.SetPosition(1, currentEnemyTarget.position);
        }
        else
        {
            targetLine.enabled = false;
        }
    }
    
    // Public methods for external control
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    public void SetShootingDistance(float distance)
    {
        shootingDistance = distance;
    }
    
    public bool HasTarget()
    {
        return hasTarget;
    }
    
    public Transform GetCurrentEnemyTarget()
    {
        return currentEnemyTarget;
    }
}
