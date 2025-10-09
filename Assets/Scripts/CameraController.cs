using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Independent camera controller for RTS-style camera movement
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Camera Movement")]
    public float moveSpeed = 10f;
    public float zoomSpeed = 5f;
    public float minZoom = 3f;
    public float maxZoom = 20f;
	public float zoomLerpSpeed = 8f; // smoothing speed for zoom transitions
    
    [Header("Player Following")]
    public Transform playerTarget;
    public float followSpeed = 8f;
    public float followDistance = 2f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    private Camera cam;
    private Vector3 targetPosition;
    private bool isFollowingPlayer = false;
    private Vector3 lastMousePosition;
    private bool isDragging = false;
	private bool isZooming = false;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
        
        // Set initial target position
        targetPosition = transform.position;
        
        // Find player if not assigned
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }
    }
    
    void Update()
    {
        HandleKeyboardMovement();
        HandleMouseInput();
        MoveCamera();
    }
    
    void HandleKeyboardMovement()
    {
        Vector3 movement = Vector3.zero;
        
        // WASD movement
        if (Keyboard.current.wKey.isPressed)
            movement += Vector3.forward;
        if (Keyboard.current.sKey.isPressed)
            movement += Vector3.back;
        if (Keyboard.current.aKey.isPressed)
            movement += Vector3.left;
        if (Keyboard.current.dKey.isPressed)
            movement += Vector3.right;
        
        // Apply movement
        if (movement != Vector3.zero)
        {
            targetPosition += movement.normalized * moveSpeed * Time.deltaTime;
            isFollowingPlayer = false; // Stop following when manually moving
            
            if (showDebugInfo)
            {
                Debug.Log($"Camera moving: {movement.normalized}");
            }
        }
    }
    
    void HandleMouseInput()
    {
        // Handle scroll wheel zoom
        HandleZoom();
    }
    
	void HandleZoom()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        
        // Get scroll wheel input
        Vector2 scrollValue = mouse.scroll.ReadValue();
        float scrollInput = scrollValue.y / 120f; // Normalize scroll value
        
		if (scrollInput != 0f)
        {
            // Move camera forward/backward in local space (respects rotation)
            // Positive scroll = zoom in (move forward), negative = zoom out (move backward)
            Vector3 zoomDirection = transform.forward;
			Vector3 movement = zoomDirection * scrollInput * zoomSpeed;
			
			// Calculate new position
			Vector3 newPosition = transform.position + movement;
            
            // Clamp based on distance from origin or height
            // Using Y height as the limiting factor
            float newHeight = newPosition.y;
            if (newHeight >= minZoom && newHeight <= maxZoom)
            {
				// Smooth zoom: set target only and let MoveCamera ease towards it
				targetPosition = newPosition;
				isZooming = true;
                
                if (showDebugInfo)
                {
                    Debug.Log($"[CameraZoom] Moved in local forward direction. New position: {newPosition}, Height: {newHeight:F2}");
                }
            }
            else
            {
				if (showDebugInfo)
                {
                    Debug.Log($"[CameraZoom] Zoom blocked - would exceed limits. Attempted height: {newHeight:F2} (min: {minZoom}, max: {maxZoom})");
                }
            }
        }
    }
    
    void FollowPlayer()
    {
        if (playerTarget != null)
        {
            isFollowingPlayer = true;
            targetPosition = playerTarget.position + Vector3.back * followDistance;
            
            if (showDebugInfo)
            {
                Debug.Log($"Camera following player: {playerTarget.name}");
            }
        }
    }
    
    void CenterOnClickedPoint()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);
        
        // Create a horizontal plane at the camera's height
        float planeY = transform.position.y;
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, planeY, 0));
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            targetPosition = new Vector3(hitPoint.x, transform.position.y, hitPoint.z);
            isFollowingPlayer = false; // Stop following when centering on point
            
            if (showDebugInfo)
            {
                Debug.Log($"Camera centering on: {hitPoint}");
            }
        }
    }
    
    /// <summary>
    /// Center camera on a specific position (called by SelectionManager)
    /// </summary>
    public void CenterOnPosition(Vector3 position)
    {
        targetPosition = new Vector3(position.x, transform.position.y, position.z);
        isFollowingPlayer = false; // Stop following when centering on point
        
        if (showDebugInfo)
        {
            Debug.Log($"Camera centering on position: {targetPosition}");
        }
    }
    
    /// <summary>
    /// Stop following the current target
    /// </summary>
    public void StopFollowing()
    {
        isFollowingPlayer = false;
        
        if (showDebugInfo)
        {
            Debug.Log("Camera stopped following target");
        }
    }
    
    
    void MoveCamera()
    {
        if (isFollowingPlayer && playerTarget != null)
        {
            // Follow player when explicitly requested (double-click)
            Vector3 desiredPosition = playerTarget.position + Vector3.back * followDistance;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        }
		else
        {
			// Normal camera movement and smooth zoom easing
			float lerpSpeed = isZooming ? zoomLerpSpeed : moveSpeed;
			transform.position = Vector3.Lerp(transform.position, targetPosition, lerpSpeed * Time.deltaTime);
			if (isZooming && (transform.position - targetPosition).sqrMagnitude < 0.0004f)
			{
				isZooming = false;
			}
        }
    }
    
    /// <summary>
    /// Set the camera to follow the player
    /// </summary>
    public void SetFollowPlayer(bool follow)
    {
        isFollowingPlayer = follow;
    }
    
    /// <summary>
    /// Set the camera to follow a specific target
    /// </summary>
    public void SetFollowTarget(Transform target)
    {
        playerTarget = target;
        isFollowingPlayer = true;
        
        // Stop any manual camera movement
        targetPosition = transform.position;
        
        if (showDebugInfo)
        {
            Debug.Log($"Camera now following target: {target.name}");
        }
    }
    
    /// <summary>
    /// Get the current camera target position
    /// </summary>
    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }
    
    /// <summary>
    /// Check if the camera is currently following the player
    /// </summary>
    public bool IsFollowingPlayer()
    {
        return isFollowingPlayer;
    }
}