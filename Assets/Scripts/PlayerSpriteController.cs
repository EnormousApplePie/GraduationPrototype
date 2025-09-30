using UnityEngine;

public class PlayerSpriteController : MonoBehaviour
{
    [Header("Sprite Settings")]
    public SpriteRenderer spriteRenderer;
    
    [Header("Movement Sprites")]
    public Sprite[] idleSprites = new Sprite[4];     // [Down, Left, Right, Up]
    public Sprite[] walkSprites = new Sprite[4];     // [Down, Left, Right, Up]
    
    [Header("Animation")]
    public float animationSpeed = 0.2f;
    public bool isAnimated = true;
    
    private PlayerController playerController;
    private Vector2 lastMovementInput;
    private int currentDirection = 0; // 0=Down, 1=Left, 2=Right, 3=Up
    private bool isMoving = false;
    private float animationTimer = 0f;
    private int animationFrame = 0;
    
    // Direction mapping for 8-directional movement
    private readonly Vector2[] directions = {
        new Vector2(0, -1),   // Down
        new Vector2(-1, 0),   // Left  
        new Vector2(1, 0),    // Right
        new Vector2(0, 1)     // Up
    };
    
    void Start()
    {
        // Get components
        playerController = GetComponent<PlayerController>();
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // If no SpriteRenderer exists, add one
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }
        
        // Set initial sprite
        UpdateSprite();
    }
    
    void Update()
    {
        // Get movement input from PlayerController
        Vector2 movementInput = GetMovementInput();
        
        // Check if player is moving
        isMoving = movementInput.magnitude > 0.1f;
        
        // Update direction based on movement
        if (isMoving)
        {
            currentDirection = GetDirectionFromInput(movementInput);
            lastMovementInput = movementInput;
        }
        
        // Handle animation timing
        if (isMoving && isAnimated)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= animationSpeed)
            {
                animationTimer = 0f;
                animationFrame = (animationFrame + 1) % GetCurrentSpriteArray().Length;
            }
        }
        else
        {
            animationFrame = 0; // Reset to first frame when idle
        }
        
        // Update sprite
        UpdateSprite();
    }
    
    Vector2 GetMovementInput()
    {
        // Try to get movement input from PlayerController
        if (playerController != null)
        {
            // We need to access the movement input from PlayerController
            // We'll add a public property to PlayerController for this
            return playerController.GetMovementInput();
        }
        
        // Fallback: read input directly
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }
    
    int GetDirectionFromInput(Vector2 input)
    {
        // Normalize input
        input = input.normalized;
        
        // Find closest direction
        float bestDot = -1f;
        int bestDirection = 0;
        
        for (int i = 0; i < directions.Length; i++)
        {
            float dot = Vector2.Dot(input, directions[i]);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestDirection = i;
            }
        }
        
        return bestDirection;
    }
    
    Sprite[] GetCurrentSpriteArray()
    {
        if (isMoving && walkSprites[currentDirection] != null)
        {
            return new Sprite[] { walkSprites[currentDirection] };
        }
        else if (idleSprites[currentDirection] != null)
        {
            return new Sprite[] { idleSprites[currentDirection] };
        }
        
        // Fallback to any available sprite
        return new Sprite[] { spriteRenderer.sprite };
    }
    
    void UpdateSprite()
    {
        Sprite[] currentSprites = GetCurrentSpriteArray();
        
        if (currentSprites.Length > 0 && currentSprites[0] != null)
        {
            spriteRenderer.sprite = currentSprites[animationFrame % currentSprites.Length];
        }
    }
    
    // Public method to manually set direction (useful for cutscenes, etc.)
    public void SetDirection(int direction)
    {
        currentDirection = Mathf.Clamp(direction, 0, 3);
        UpdateSprite();
    }
    
    // Public method to set if character is moving (useful for external control)
    public void SetMoving(bool moving)
    {
        isMoving = moving;
        if (!moving) animationFrame = 0;
    }
} 