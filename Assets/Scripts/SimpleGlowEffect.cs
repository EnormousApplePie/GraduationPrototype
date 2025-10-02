using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple glow effect for sprites using multiple offset sprites
/// </summary>
public class SimpleGlowEffect : MonoBehaviour
{
    [Header("Hover Settings")]
    public Color glowColor = Color.yellow;
    public float glowSize = 0.05f; // Much smaller base size
    public int glowLayers = 4;
    public bool enableHoverEffect = true;
    public bool animateGlow = true;
    public float glowAnimationSpeed = 2f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    private SpriteRenderer[] spriteRenderers;
    private SpriteRenderer[] glowRenderers;
    private bool isHovered = false;
    private Camera mainCamera;
    private float glowAnimationTime = 0f;
    
    void Start()
    {
        // Get all sprite renderers on this object and children
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        
        // Create glow renderers
        CreateGlowRenderers();
        
        // Get main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
    }
    
    void CreateGlowRenderers()
    {
        glowRenderers = new SpriteRenderer[spriteRenderers.Length * glowLayers];
        
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                for (int j = 0; j < glowLayers; j++)
                {
                    // Create glow GameObject
                    GameObject glowObj = new GameObject($"Glow_{i}_{j}");
                    glowObj.transform.SetParent(spriteRenderers[i].transform); // Parent to the sprite renderer
                    glowObj.transform.localPosition = Vector3.zero;
                    glowObj.transform.localRotation = Quaternion.identity;
                    
                    // Use scale 1 and add glow size
                    glowObj.transform.localScale = Vector3.one + (Vector3.one * glowSize * (j + 1));
                    
                    // Add SpriteRenderer
                    int glowIndex = i * glowLayers + j;
                    glowRenderers[glowIndex] = glowObj.AddComponent<SpriteRenderer>();
                    
                    // Configure glow renderer to match the main sprite renderer
                    glowRenderers[glowIndex].color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.3f / (j + 1));
                    glowRenderers[glowIndex].sortingOrder = spriteRenderers[i].sortingOrder - 1 - j;
                    glowRenderers[glowIndex].sortingLayerName = spriteRenderers[i].sortingLayerName; // Match sorting layer
                    glowRenderers[glowIndex].enabled = false; // Start hidden
                    
                    // Set the glow sprite to match the main sprite
                    glowRenderers[glowIndex].sprite = spriteRenderers[i].sprite;
                }
            }
        }
    }
    
    void Update()
    {
        if (!enableHoverEffect || mainCamera == null) return;
        
        // Check if mouse is over this enemy
        bool mouseOver = IsMouseOverEnemy();
        
        if (mouseOver && !isHovered)
        {
            SetHovered(true);
        }
        else if (!mouseOver && isHovered)
        {
            SetHovered(false);
        }
        
        // Update glow sprites to match the main sprite animation
        if (isHovered)
        {
            UpdateGlowSprites();
            
            // Update glow animation
            if (animateGlow)
            {
                glowAnimationTime += Time.deltaTime * glowAnimationSpeed;
            }
        }
    }
    
    void UpdateGlowSprites()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                for (int j = 0; j < glowLayers; j++)
                {
                    int glowIndex = i * glowLayers + j;
                    if (glowRenderers[glowIndex] != null)
                    {
                        // Copy the current sprite from the main renderer
                        if (spriteRenderers[i].sprite != null)
                        {
                            glowRenderers[glowIndex].sprite = spriteRenderers[i].sprite;
                        }
                        
                        // Use scale 1 and add glow size
                        Vector3 glowAddition = Vector3.one * glowSize * (j + 1);
                        
                        // Add animation to the glow
                        if (animateGlow)
                        {
                            float animationOffset = Mathf.Sin(glowAnimationTime + j * 0.5f) * 0.1f;
                            glowAddition += Vector3.one * animationOffset;
                        }
                        
                        glowRenderers[glowIndex].transform.localScale = Vector3.one + glowAddition;
                        
                        // Update sorting order to match the main sprite
                        glowRenderers[glowIndex].sortingOrder = spriteRenderers[i].sortingOrder - 1 - j;
                    }
                }
            }
        }
    }
    
    bool IsMouseOverEnemy()
    {
        // Get mouse position using new Input System
        Vector2 mousePos = Mouse.current.position.ReadValue();
        
        // Create ray from camera through mouse position
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;
        
        // Check if ray hits this enemy or a child of this enemy
        if (Physics.Raycast(ray, out hit))
        {
            // Check if the hit object is this enemy or a child of this enemy
            Transform hitTransform = hit.collider.transform;
            while (hitTransform != null)
            {
                if (hitTransform == transform)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"Mouse over enemy: {gameObject.name}");
                    }
                    return true;
                }
                hitTransform = hitTransform.parent;
            }
        }
        
        return false;
    }
    
    void SetHovered(bool hovered)
    {
        isHovered = hovered;
        
        if (showDebugInfo)
        {
            Debug.Log($"Enemy {gameObject.name} hovered: {hovered}");
        }
        
        // Show or hide the glow
        for (int i = 0; i < glowRenderers.Length; i++)
        {
            if (glowRenderers[i] != null)
            {
                glowRenderers[i].enabled = hovered;
            }
        }
    }
    
    /// <summary>
    /// Manually set hover state (for external control)
    /// </summary>
    public void SetHoverState(bool hovered)
    {
        SetHovered(hovered);
    }
    
    /// <summary>
    /// Check if this enemy is currently being hovered
    /// </summary>
    public bool IsHovered()
    {
        return isHovered;
    }
    
    void OnDestroy()
    {
        // Clean up the glow renderers
        if (glowRenderers != null)
        {
            for (int i = 0; i < glowRenderers.Length; i++)
            {
                if (glowRenderers[i] != null && glowRenderers[i].gameObject != null)
                {
                    DestroyImmediate(glowRenderers[i].gameObject);
                }
            }
        }
    }
}
