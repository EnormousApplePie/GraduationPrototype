using UnityEngine;

/// <summary>
/// PlayerAnimationController handles 8-directional character animation for both players and enemies
/// Direction Mapping:
/// 0 = Down, 1 = DownLeft, 2 = Left, 3 = UpLeft
/// 4 = Up, 5 = UpRight, 6 = Right, 7 = DownRight
/// 
/// The controller uses diagonalThreshold to determine when to use diagonal directions
/// vs cardinal directions. Higher values make diagonal movement less sensitive.
/// </summary>
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator animator;
    
    [Header("Animation Parameters")]
    public string horizontalParam = "Horizontal";
    public string verticalParam = "Vertical";
    public string isMovingParam = "IsMoving";
    public string directionParam = "Direction"; // 0=Down, 1=DownLeft, 2=Left, 3=UpLeft, 4=Up, 5=UpRight, 6=Right, 7=DownRight
    
    [Header("Blend Tree Support")]
    public bool useBlendTree = false; // Set to true if using 2D Blend Tree instead of state machine
    // When useBlendTree is true, the script sets DirectionX and DirectionY parameters
    // DirectionX: -1 (Left) to 1 (Right), DirectionY: -1 (Down) to 1 (Up)
    
    [Header("Direction Threshold")]
    public float directionThreshold = 0.1f;
    
    [Header("Diagonal Movement")]
    public float diagonalThreshold = 0.4f; // How much input needed to register diagonal movement
    
    [Header("Debug")]
    public bool enableDebug = false;
    
    private PlayerController playerController;
    private EnemyCharacter enemyCharacter;
    private FriendlyCharacter friendlyCharacter;
    private Vector2 lastMovementDirection;
    private int currentDirection = 0; // 0=Down, 1=DownLeft, 2=Left, 3=UpLeft, 4=Up, 5=UpRight, 6=Right, 7=DownRight
    
    // External movement input (for RTS movement)
    private Vector2 externalMovementInput = Vector2.zero;
    private bool useExternalInput = false;
    
    void Start()
    {
        // Get PlayerController from parent object (for players)
        playerController = GetComponentInParent<PlayerController>();
        
        // Get EnemyCharacter from parent object (for enemies)
        enemyCharacter = GetComponentInParent<EnemyCharacter>();
        
        // Get FriendlyCharacter from parent object (for friendlies)
        friendlyCharacter = GetComponentInParent<FriendlyCharacter>();
        
        if (playerController == null && enemyCharacter == null && friendlyCharacter == null)
        {
            if (enableDebug)
            {
                Debug.LogWarning("No PlayerController, EnemyCharacter, or FriendlyCharacter found in parent objects of " + gameObject.name + ". Make sure this script is on a child of the GameObject with one of these components.");
            }
        }
        
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            
            if (animator == null)
            {
                Debug.LogWarning("No Animator component found on " + gameObject.name + ". Please add an Animator component.");
            }
        }
    }
    
    void Update()
    {
        if (animator == null) return;
        if (playerController == null && enemyCharacter == null && friendlyCharacter == null && !useExternalInput) return;
        
        // Get movement input from appropriate controller or external source
        Vector2 movementInput = useExternalInput ? externalMovementInput : GetMovementInput();
        bool isMoving = movementInput.magnitude > directionThreshold;
        
        // Update animator parameters
        animator.SetFloat(horizontalParam, movementInput.x);
        animator.SetFloat(verticalParam, movementInput.y);
        animator.SetBool(isMovingParam, isMoving);
        
        // For blend tree compatibility, set normalized direction values
        if (useBlendTree)
        {
            Vector2 directionVector = GetDirectionVector(currentDirection);
            animator.SetFloat("DirectionX", directionVector.x);
            animator.SetFloat("DirectionY", directionVector.y);
        }
        else
        {
            // Set direction parameter (for state machine approach)
            animator.SetInteger(directionParam, currentDirection);
        }
        
        // Update direction when moving
        if (isMoving)
        {
            UpdateDirection(movementInput);
            lastMovementDirection = movementInput;
        }
        
        // Debug information
        if (enableDebug)
        {
            Debug.Log($"Movement Input: {movementInput} | Magnitude: {movementInput.magnitude:F3} | Threshold: {directionThreshold} | IsMoving: {isMoving} | Direction: {currentDirection} ({GetDirectionName(currentDirection)})");
        }
    }
    
    Vector2 GetMovementInput()
    {
        if (playerController != null)
        {
            // Get movement from PlayerController
            return playerController.GetMovementInput();
        }
        else if (enemyCharacter != null)
        {
            // Get movement from EnemyCharacter (AI movement)
            return enemyCharacter.GetMovementDirection();
        }
        else if (friendlyCharacter != null)
        {
            // Get movement from FriendlyCharacter (AI movement)
            // FriendlyCharacter updates animation through SetMovementInput, so return zero here
            return Vector2.zero;
        }
        
        return Vector2.zero;
    }
    
    string GetDirectionName(int direction)
    {
        switch (direction)
        {
            case 0: return "Down";
            case 1: return "DownLeft";
            case 2: return "Left";
            case 3: return "UpLeft";
            case 4: return "Up";
            case 5: return "UpRight";
            case 6: return "Right";
            case 7: return "DownRight";
            default: return "Unknown";
        }
    }
    
    void UpdateDirection(Vector2 input)
    {
        // Normalize input to get clean direction values
        Vector2 normalizedInput = input.normalized;
        
        // Define the 8 possible directions with their corresponding angles
        Vector2[] directions = {
            new Vector2(0, -1),     // 0: Down
            new Vector2(-1, -1).normalized,  // 1: DownLeft  
            new Vector2(-1, 0),     // 2: Left
            new Vector2(-1, 1).normalized,   // 3: UpLeft
            new Vector2(0, 1),      // 4: Up
            new Vector2(1, 1).normalized,    // 5: UpRight
            new Vector2(1, 0),      // 6: Right
            new Vector2(1, -1).normalized    // 7: DownRight
        };
        
        // Find the direction that best matches the input
        float bestDot = -1f;
        int bestDirection = 0;
        
        for (int i = 0; i < directions.Length; i++)
        {
            float dot = Vector2.Dot(normalizedInput, directions[i]);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestDirection = i;
            }
        }
        
        // Check if we should use diagonal movement
        float absX = Mathf.Abs(normalizedInput.x);
        float absY = Mathf.Abs(normalizedInput.y);
        
        // If both X and Y components are significant, use diagonal direction
        if (absX >= diagonalThreshold && absY >= diagonalThreshold)
        {
            currentDirection = bestDirection;
        }
        else
        {
            // Use cardinal directions only
            if (absX > absY)
            {
                // Horizontal movement is stronger
                currentDirection = normalizedInput.x > 0 ? 6 : 2; // Right or Left
            }
            else
            {
                // Vertical movement is stronger  
                currentDirection = normalizedInput.y > 0 ? 4 : 0; // Up or Down
            }
        }
    }
    
    // Public methods for external control
    public void SetDirection(int direction)
    {
        currentDirection = Mathf.Clamp(direction, 0, 7);
        if (animator != null)
        {
            animator.SetInteger(directionParam, currentDirection);
        }
    }
    
    public void ForceAnimation(string animationName)
    {
        if (animator != null)
        {
            animator.Play(animationName);
        }
    }
    
    public int GetCurrentDirection()
    {
        return currentDirection;
    }
    
    public bool IsMoving()
    {
        if (animator != null)
        {
            return animator.GetBool(isMovingParam);
        }
        return false;
    }

    private Vector2 GetDirectionVector(int direction)
    {
        switch (direction)
        {
            case 0: return new Vector2(0, -1); // Down
            case 1: return new Vector2(-1, -1); // DownLeft
            case 2: return new Vector2(-1, 0); // Left
            case 3: return new Vector2(-1, 1); // UpLeft
            case 4: return new Vector2(0, 1); // Up
            case 5: return new Vector2(1, 1); // UpRight
            case 6: return new Vector2(1, 0); // Right
            case 7: return new Vector2(1, -1); // DownRight
            default: return Vector2.zero;
        }
    }
    
    /// <summary>
    /// Set external movement input (for RTS movement system)
    /// </summary>
    public void SetMovementInput(Vector2 input)
    {
        externalMovementInput = input;
        
        // Enable external input when RTS or Friendly characters are present
        if (playerController == null || GetComponent<RTSMovementController>() != null || GetComponentInParent<RTSMovementController>() != null)
        {
            useExternalInput = true;
        }
    }
    
    /// <summary>
    /// Clear external movement input and return to normal input
    /// </summary>
    public void ClearExternalInput()
    {
        useExternalInput = false;
        externalMovementInput = Vector2.zero;
    }
} 