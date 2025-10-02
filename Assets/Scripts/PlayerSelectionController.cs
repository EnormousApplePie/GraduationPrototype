using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player selection with visual feedback
/// </summary>
public class PlayerSelectionController : MonoBehaviour
{
    [Header("Selection Settings")]
    public Color selectionColor = Color.green;
    public float selectionGlowSize = 0.1f;
    public int selectionGlowLayers = 3;
    public bool enableSelectionEffect = true;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    private SpriteRenderer[] spriteRenderers;
    private SpriteRenderer[] selectionRenderers;
    private bool isSelected = false;
    private Camera mainCamera;
    
    void Start()
    {
        // Get all sprite renderers on this object and children
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        
        // Create selection renderers
        CreateSelectionRenderers();
        
        // Get main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
    }
    
    void CreateSelectionRenderers()
    {
        selectionRenderers = new SpriteRenderer[spriteRenderers.Length * selectionGlowLayers];
        
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                for (int j = 0; j < selectionGlowLayers; j++)
                {
                    // Create selection GameObject
                    GameObject selectionObj = new GameObject($"Selection_{i}_{j}");
                    selectionObj.transform.SetParent(spriteRenderers[i].transform);
                    selectionObj.transform.localPosition = Vector3.zero;
                    selectionObj.transform.localRotation = Quaternion.identity;
                    
                    // Use scale 1 and add selection size
                    selectionObj.transform.localScale = Vector3.one + (Vector3.one * selectionGlowSize * (j + 1));
                    
                    // Add SpriteRenderer
                    int selectionIndex = i * selectionGlowLayers + j;
                    selectionRenderers[selectionIndex] = selectionObj.AddComponent<SpriteRenderer>();
                    
                    // Configure selection renderer
                    selectionRenderers[selectionIndex].color = new Color(selectionColor.r, selectionColor.g, selectionColor.b, 0.4f / (j + 1));
                    selectionRenderers[selectionIndex].sortingOrder = spriteRenderers[i].sortingOrder + 1 + j; // Render in front
                    selectionRenderers[selectionIndex].sortingLayerName = spriteRenderers[i].sortingLayerName;
                    selectionRenderers[selectionIndex].enabled = false; // Start hidden
                    
                    // Set the selection sprite to match the main sprite
                    selectionRenderers[selectionIndex].sprite = spriteRenderers[i].sprite;
                }
            }
        }
    }
    
    void Update()
    {
        if (!enableSelectionEffect || mainCamera == null) return;
        
        // Update selection sprites to match the main sprite animation
        if (isSelected)
        {
            UpdateSelectionSprites();
        }
    }
    
    void UpdateSelectionSprites()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                for (int j = 0; j < selectionGlowLayers; j++)
                {
                    int selectionIndex = i * selectionGlowLayers + j;
                    if (selectionRenderers[selectionIndex] != null)
                    {
                        // Copy the current sprite from the main renderer
                        if (spriteRenderers[i].sprite != null)
                        {
                            selectionRenderers[selectionIndex].sprite = spriteRenderers[i].sprite;
                        }
                        
                        // Update the selection scale
                        Vector3 selectionAddition = Vector3.one * selectionGlowSize * (j + 1);
                        selectionRenderers[selectionIndex].transform.localScale = Vector3.one + selectionAddition;
                        
                        // Update sorting order to match the main sprite
                        selectionRenderers[selectionIndex].sortingOrder = spriteRenderers[i].sortingOrder + 1 + j;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Set the selection state of this player
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (showDebugInfo)
        {
            Debug.Log($"Player {gameObject.name} selected: {selected}");
        }
        
        // Show or hide the selection
        for (int i = 0; i < selectionRenderers.Length; i++)
        {
            if (selectionRenderers[i] != null)
            {
                selectionRenderers[i].enabled = selected;
            }
        }
    }
    
    /// <summary>
    /// Check if this player is currently selected
    /// </summary>
    public bool IsSelected()
    {
        return isSelected;
    }
    
    void OnDestroy()
    {
        // Clean up the selection renderers
        if (selectionRenderers != null)
        {
            for (int i = 0; i < selectionRenderers.Length; i++)
            {
                if (selectionRenderers[i] != null && selectionRenderers[i].gameObject != null)
                {
                    DestroyImmediate(selectionRenderers[i].gameObject);
                }
            }
        }
    }
}
