using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player selection with visual feedback
/// </summary>
public class PlayerSelectionController : MonoBehaviour
{
	[Header("Selection Settings (legacy sprite outline)")]
	public Color selectionColor = Color.green;
	public float selectionGlowSize = 0.1f;
	public int selectionGlowLayers = 3;
	public bool enableSelectionEffect = true;
		
	[Header("Hover Settings (legacy sprite outline)")]
	public bool enableHoverEffect = true;
	public Color hoverColor = Color.yellow;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
	private SpriteRenderer[] spriteRenderers;
	private SpriteRenderer[] selectionRenderers;
    private bool isSelected = false;
	private bool isHovered = false;
    private Camera mainCamera;
	private SelectionCircle selectionCircle;
    
    void Start()
    {
        // Get all sprite renderers on this object and children
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        
		// Create selection renderers only if no SelectionCircle is present
		
		// Add or find selection circle for bottom-ring feedback
		selectionCircle = GetComponent<SelectionCircle>();
		if (selectionCircle == null)
		{
			selectionCircle = gameObject.AddComponent<SelectionCircle>();
		}
		else
		{
			// When a SelectionCircle exists, disable legacy sprite outline visuals by default
			enableSelectionEffect = false;
			enableHoverEffect = false;
		}
		
		if (enableSelectionEffect || enableHoverEffect)
		{
			CreateSelectionRenderers();
		}
        
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
                    
					// Configure selection renderer (render behind main sprite so health bars stay on top)
					selectionRenderers[selectionIndex].color = new Color(selectionColor.r, selectionColor.g, selectionColor.b, 0.4f / (j + 1));
					selectionRenderers[selectionIndex].sortingOrder = spriteRenderers[i].sortingOrder - 1 - j;
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
		if (mainCamera == null) return;
		if (!enableSelectionEffect && !enableHoverEffect && selectionCircle == null) return;
		
		// Update selection/hover sprites to match the main sprite animation
		if ((enableSelectionEffect || enableHoverEffect) && (isSelected || isHovered))
		{
			UpdateSelectionSprites();
		}
		
		// Update selection circle states
		if (selectionCircle != null)
		{
			selectionCircle.SetSelected(isSelected);
			selectionCircle.SetHovered(!isSelected && isHovered);
			// Match sorting to primary sprite for consistent layering
			if (spriteRenderers != null && spriteRenderers.Length > 0 && spriteRenderers[0] != null)
			{
				selectionCircle.ApplySortingFrom(spriteRenderers[0]);
			}
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
                        
					// Update sorting order to stay behind the main sprite
					selectionRenderers[selectionIndex].sortingOrder = spriteRenderers[i].sortingOrder - 1 - j;
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
		
		UpdateSelectionVisibility();
    }
	
	/// <summary>
	/// Set hover visual state (does not change selection state)
	/// </summary>
	public void SetHovered(bool hovered)
	{
		if (!enableHoverEffect) return;
		isHovered = hovered;
		UpdateSelectionVisibility();
	}

	void UpdateSelectionVisibility()
	{
		// If legacy outline renderers are not created (using SelectionCircle-only), skip safely
		if (selectionRenderers == null || selectionRenderers.Length == 0)
		{
			return;
		}
		
		bool shouldShow = (enableSelectionEffect && isSelected) || (enableHoverEffect && isHovered);
		for (int i = 0; i < selectionRenderers.Length; i++)
		{
			var sr = selectionRenderers[i];
			if (sr != null)
			{
				sr.enabled = shouldShow;
				// Color by state (hover when not selected)
				sr.color = isSelected ? new Color(selectionColor.r, selectionColor.g, selectionColor.b, sr.color.a) : new Color(hoverColor.r, hoverColor.g, hoverColor.b, sr.color.a);
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

