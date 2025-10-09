using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target; // The player to follow
    public Vector3 offset = new Vector3(0f, 10f, -10f); // Camera offset from player
    public Vector3 cameraRotation = new Vector3(30f, 0f, 0f); // Fixed camera rotation (Euler angles)
    
    [Header("Smoothing")]
    public float followSpeed = 2f; // How fast the camera follows
    public bool smoothRotation = false; // Whether to smooth camera rotation too
    public float rotationSpeed = 2f; // Rotation smoothing speed
    
    [Header("Anti-Jitter Settings")]
    public bool useFixedUpdate = true; // Use FixedUpdate to match player physics timing
    public bool useSmoothDamp = false; // Use SmoothDamp instead of Lerp for better smoothing
    public float smoothTime = 0.3f; // SmoothDamp parameter
    
    [Header("Zoom Settings")]
    public bool enableZoom = true;
    public float zoomSpeed = 2f; // How fast to zoom
    public float minZoom = 5f; // Minimum distance from target
    public float maxZoom = 30f; // Maximum distance from target
	public float zoomSmoothness = 5f; // Legacy Lerp smoothing (unused when useZoomSmoothDamp = true)
	public bool useZoomSmoothDamp = true;
	public float zoomSmoothTime = 0.15f;
    
    [Header("Debug")]
    public bool showZoomDebug = false;
    
    private Vector3 velocity; // For SmoothDamp
    private Rigidbody targetRigidbody; // Cache player's rigidbody for interpolation
    private float currentZoomDistance; // Current zoom distance
    private float targetZoomDistance; // Target zoom distance for smoothing
    
    void Start()
    {
        // If no target is assigned, try to find the player automatically
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                // Fallback: find PlayerController component
                PlayerController playerController = FindFirstObjectByType<PlayerController>();
                if (playerController != null)
                {
                    target = playerController.transform;
                }
            }
        }
        
        // Cache the target's rigidbody for interpolation
        if (target != null)
        {
            targetRigidbody = target.GetComponent<Rigidbody>();
        }
        
        // Set initial position if target is found
        if (target != null)
        {
            transform.position = target.position + offset;
            if (smoothRotation)
            {
                transform.LookAt(target);
            }
            else
            {
                transform.rotation = Quaternion.Euler(cameraRotation);
            }
        }
        
        // Initialize zoom distance from current offset
        currentZoomDistance = offset.magnitude;
        targetZoomDistance = currentZoomDistance;
        
        Debug.Log($"[CameraFollow] Started! Zoom enabled: {enableZoom}, Initial zoom distance: {currentZoomDistance:F2}, Min: {minZoom}, Max: {maxZoom}");
    }
    
    void Update()
    {
        // Debug to confirm Update is running
        if (showZoomDebug)
        {
            Debug.Log("[CameraFollow] Update() called");
        }
        
        // Handle zoom input in Update for immediate response
        if (enableZoom)
        {
            HandleZoom();
        }
        else if (showZoomDebug)
        {
            Debug.Log("[CameraFollow] Zoom is disabled!");
        }
    }
    
    void FixedUpdate()
    {
        // Use FixedUpdate when following rigidbody-based movement to match physics timing
        if (useFixedUpdate && target != null)
        {
            UpdateCameraPosition();
        }
    }
    
    void LateUpdate()
    {
        // Use LateUpdate for transform-based movement or when FixedUpdate is disabled
        if (!useFixedUpdate && target != null)
        {
            UpdateCameraPosition();
        }
    }
    
    void HandleZoom()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            Debug.LogWarning("CameraFollow: Mouse.current is null! Make sure Input System is enabled.");
            return;
        }
        
        // Get scroll wheel input
        Vector2 scrollValue = mouse.scroll.ReadValue();
        float scrollInput = scrollValue.y / 120f; // Normalize scroll value
        
        // Debug: Always log when scrolling is detected
        if (scrollInput != 0f)
        {
            Debug.Log($"[CameraZoom] Scroll detected! Input: {scrollInput}, Raw: {scrollValue.y}, Current zoom: {currentZoomDistance:F2}, Target: {targetZoomDistance:F2}");
            
            // Adjust target zoom distance
            targetZoomDistance -= scrollInput * zoomSpeed;
            targetZoomDistance = Mathf.Clamp(targetZoomDistance, minZoom, maxZoom);
            
            Debug.Log($"[CameraZoom] New target zoom: {targetZoomDistance:F2} (clamped between {minZoom} and {maxZoom})");
        }
        
		// Smoothly interpolate current zoom to target zoom
		float previousZoom = currentZoomDistance;
		if (useZoomSmoothDamp)
		{
			currentZoomDistance = Mathf.SmoothDamp(currentZoomDistance, targetZoomDistance, ref velocity.z, zoomSmoothTime);
		}
		else
		{
			currentZoomDistance = Mathf.Lerp(currentZoomDistance, targetZoomDistance, Time.deltaTime * zoomSmoothness);
		}
        
        if (showZoomDebug && Mathf.Abs(currentZoomDistance - previousZoom) > 0.01f)
        {
            Debug.Log($"[CameraZoom] Zoom changing: {previousZoom:F2} -> {currentZoomDistance:F2}");
        }
    }
    
    void UpdateCameraPosition()
    {
        if (target == null) return;
        
        // Get target position (use interpolated position for rigidbody)
        Vector3 targetPosition = target.position;
        if (targetRigidbody != null && targetRigidbody.interpolation != RigidbodyInterpolation.None)
        {
            // Use the rigidbody's interpolated position for smoother following
            targetPosition = targetRigidbody.position;
        }
        
        // Calculate desired position with zoom applied
        // Maintain the same direction as offset, but adjust distance
        Vector3 offsetDirection = offset.normalized;
        Vector3 zoomedOffset = offsetDirection * currentZoomDistance;
        Vector3 desiredPosition = targetPosition + zoomedOffset;
        
        // Choose smoothing method
        if (useSmoothDamp)
        {
            // SmoothDamp provides more natural movement with automatic deceleration
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        }
        else
        {
            // Traditional lerp smoothing
            float deltaTime = useFixedUpdate ? Time.fixedDeltaTime : Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * deltaTime);
        }
        
        // Handle rotation
        if (smoothRotation)
        {
            // Smooth rotation to look at target
            Vector3 direction = targetPosition - transform.position;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                float deltaTime = useFixedUpdate ? Time.fixedDeltaTime : Time.deltaTime;
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
            }
        }
        else
        {
            // Use fixed camera rotation
            transform.rotation = Quaternion.Euler(cameraRotation);
        }
    }
} 