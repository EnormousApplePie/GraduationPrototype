using UnityEngine;

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
    
    private Vector3 velocity; // For SmoothDamp
    private Rigidbody targetRigidbody; // Cache player's rigidbody for interpolation
    
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
                PlayerController playerController = FindObjectOfType<PlayerController>();
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
        
        // Calculate desired position
        Vector3 desiredPosition = targetPosition + offset;
        
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