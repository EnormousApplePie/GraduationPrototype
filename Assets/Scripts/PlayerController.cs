using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float deceleration = 10f;
    
    private Rigidbody rb;
    private Vector2 movementInput;
    private Vector3 currentVelocity;
    private InputAction moveAction;
    
    // Public property for sprite controller to access movement input
    public Vector2 GetMovementInput() => movementInput;
    
    void Start()
    {
        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();
        
        // If no Rigidbody exists, add one
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody for smooth movement
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth visual movement
        rb.linearDamping = 0f; // No drag for precise movement
        rb.angularDamping = 0f; // No angular drag
        
        // Freeze rotation to prevent the player from tipping over
        rb.freezeRotation = true;
        
        // Setup input actions
        SetupInputActions();
    }
    
    void SetupInputActions()
    {
        // Create direct input action for WASD movement
        moveAction = new InputAction("Move", binding: "<Keyboard>/wasd");
        moveAction.AddCompositeBinding("Dpad")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        
        moveAction.Enable();
    }
    
    void Update()
    {
        // Read input from the move action
        if (moveAction != null && moveAction.enabled)
        {
            movementInput = moveAction.ReadValue<Vector2>();
        }
    }
    
    void FixedUpdate()
    {
        if (movementInput.magnitude > 0)
        {
            // Direct movement when there's input - no smoothing
            Vector3 movement = new Vector3(movementInput.x, 0f, movementInput.y).normalized * moveSpeed;
            rb.MovePosition(transform.position + movement * Time.fixedDeltaTime);
            
            // Update current velocity for smooth stopping later
            currentVelocity = movement;
        }
        else
        {
            // Smooth deceleration when no input
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, deceleration * Time.fixedDeltaTime);
            
            // Apply deceleration movement
            if (currentVelocity.magnitude > 0.01f)
            {
                rb.MovePosition(transform.position + currentVelocity * Time.fixedDeltaTime);
            }
        }
    }
    
    void OnDestroy()
    {
        // Clean up input actions
        if (moveAction != null)
        {
            moveAction.Disable();
        }
    }
}
