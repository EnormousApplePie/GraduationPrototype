using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// SelectionManager handles player selection, movement commands, and attack commands
/// Features: Click to select, drag selection rectangle, right-click movement/attack, persistent enemy highlighting
/// </summary>
public class SelectionManager : MonoBehaviour
{
    [Header("Selection Settings")]
    public string playerTag = "Player";
    public string alliedTag = "Allied";
    public string enemyTag = "Enemy";
    public string groundTag = "Ground";
    
    [Header("Drag Selection")]
    public Color selectionBoxColor = new Color(0f, 1f, 0f, 0.3f);
    public Color selectionBorderColor = new Color(0f, 1f, 0f, 1f);
    public float borderWidth = 2f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    private List<PlayerSelectionController> selectedPlayers = new List<PlayerSelectionController>();
    private Camera mainCamera;
    private Mouse mouse;
    
    // Persistent enemy highlight
    private SimpleGlowEffect currentlyHighlightedEnemy = null;
    
    // Drag selection
    private bool isDragging = false;
    private Vector2 dragStartPosition;
    private Vector2 dragCurrentPosition;
    private Texture2D selectionTexture;
    private Texture2D borderTexture;
	private HashSet<GameObject> dragHoverUnits = new HashSet<GameObject>();
    
    void Start()
    {
        mainCamera = Camera.main;
        
        // Fallback: try to find any camera
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("SelectionManager: No camera found! Please add a camera to your scene and tag it as 'MainCamera'.");
        }
        
        mouse = Mouse.current;
        if (mouse == null)
        {
            Debug.LogError("SelectionManager: Mouse not detected!");
        }
        
        CreateSelectionTextures();
    }
    
    void CreateSelectionTextures()
    {
        // Create selection box texture
        selectionTexture = new Texture2D(1, 1);
        selectionTexture.SetPixel(0, 0, selectionBoxColor);
        selectionTexture.Apply();
        
        // Create border texture
        borderTexture = new Texture2D(1, 1);
        borderTexture.SetPixel(0, 0, selectionBorderColor);
        borderTexture.Apply();
    }
    
    void Update()
    {
        if (mouse == null || mainCamera == null) return;
        
        HandleInput();
    }
    
    void HandleInput()
    {
        // Start drag selection
        if (mouse.leftButton.wasPressedThisFrame)
        {
            dragStartPosition = mouse.position.ReadValue();
            isDragging = true;
        }
        
		// Update drag position and live drag-hover feedback
		if (isDragging)
		{
			dragCurrentPosition = mouse.position.ReadValue();
			// Live update drag-hover circles while dragging
			UpdateDragHover(dragStartPosition, dragCurrentPosition);
		}
        
		// End drag selection
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            if (isDragging)
            {
                Vector2 dragEndPosition = mouse.position.ReadValue();
                float dragDistance = Vector2.Distance(dragStartPosition, dragEndPosition);
                
                if (dragDistance > 5f) // Threshold to differentiate from click
                {
                    HandleDragSelection(dragStartPosition, dragEndPosition);
                }
                else
                {
                    HandleLeftClick();
                }
                
				// Clear drag-hover for all previously hovered units
				ClearAllDragHover();
                isDragging = false;
            }
        }
        
        // Right click for movement/attack
        if (mouse.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
        }
    }
    
    void HandleLeftClick()
    {
        PlayerSelectionController player = GetPlayerAtMouse();
        
        if (player != null)
        {
            // Select single player
            DeselectAllPlayers();
            SelectPlayer(player);
            
            if (showDebugInfo)
            {
                Debug.Log($"Selected player: {player.gameObject.name}");
            }
        }
        else
        {
            // Clicked on empty space, deselect all
            DeselectAllPlayers();
            
            if (showDebugInfo)
            {
                Debug.Log("Deselected all players");
            }
        }
    }
    
    void HandleDragSelection(Vector2 startPos, Vector2 endPos)
    {
        DeselectAllPlayers();
        
        // Get all player and allied characters
        List<GameObject> allUnits = new List<GameObject>();
        allUnits.AddRange(GameObject.FindGameObjectsWithTag(playerTag));
        allUnits.AddRange(GameObject.FindGameObjectsWithTag(alliedTag));
        
        // Create screen space rectangle
        Rect selectionRect = GetScreenRect(startPos, endPos);
        
		int selectedCount = 0;
		foreach (GameObject unit in allUnits)
        {
            if (unit == null) continue;
            
            Vector3 screenPos = mainCamera.WorldToScreenPoint(unit.transform.position);
            
            if (selectionRect.Contains(screenPos))
            {
                PlayerSelectionController psc = unit.GetComponent<PlayerSelectionController>();
                if (psc != null)
                {
                    SelectPlayer(psc);
                    selectedCount++;
					// Drag-hover feedback using SelectionCircle
					SelectionCircle circle = unit.GetComponent<SelectionCircle>();
					if (circle != null)
					{
						circle.SetDragHover(true);
						dragHoverUnits.Add(unit);
					}
                }
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Drag selected {selectedCount} units");
        }
    }
    
    Rect GetScreenRect(Vector2 screenPos1, Vector2 screenPos2)
    {
        // Create a rectangle from two corner points
        Vector2 bottomLeft = Vector2.Min(screenPos1, screenPos2);
        Vector2 topRight = Vector2.Max(screenPos1, screenPos2);
        return Rect.MinMaxRect(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y);
    }
    
    void HandleRightClick()
    {
        if (selectedPlayers.Count == 0) 
        {
            if (showDebugInfo)
            {
                Debug.Log("Right-clicked but no players selected");
            }
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Right-clicked with {selectedPlayers.Count} players selected");
        }
        
        Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
        RaycastHit hit;
        
        // Raycast to find what we clicked on (no layer mask, use tags instead)
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // Check if it's an enemy
            if (hitObject.CompareTag(enemyTag))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"Hit object has enemy tag: {hitObject.name}");
                }
                
                EnemyCharacter enemy = hitObject.GetComponent<EnemyCharacter>();
                if (enemy != null)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"Enemy component found. Is alive: {enemy.IsAlive()}");
                    }
                    
                    if (enemy.IsAlive())
                    {
                        // Clear previous persistent highlight
                        ClearPersistentHighlight();
                        
                        // Set new persistent highlight (prefer SelectionCircle)
                        SelectionCircle enemyCircle = hitObject.GetComponent<SelectionCircle>();
                        if (enemyCircle != null)
                        {
                            enemyCircle.SetPersistent(true);
                        }
                        else
                        {
                            // Fallback to legacy glow if present
                            SimpleGlowEffect glowEffect = hitObject.GetComponent<SimpleGlowEffect>();
                            if (glowEffect != null)
                            {
                                glowEffect.SetPersistentHighlight(true);
                                currentlyHighlightedEnemy = glowEffect;
                            }
                        }
                        
                        // Command all selected players to attack this enemy
                        foreach (PlayerSelectionController player in selectedPlayers)
                        {
                            RTSMovementController movement = player.GetComponent<RTSMovementController>();
                            if (movement != null)
                            {
                                movement.SetEnemyTarget(hitObject.transform);
                            }
                        }
                        
                        if (showDebugInfo)
                        {
                            Debug.Log($"[SelectionManager] Attacking enemy: {hitObject.name}");
                        }
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning($"Hit enemy-tagged object {hitObject.name} but no EnemyCharacter component found!");
                }
            }
            
            // If not an enemy, treat as ground/movement target
            ClearPersistentHighlight();
            
            // Move all selected players to the clicked position (Y-axis locking is handled by RTSMovementController)
            foreach (PlayerSelectionController player in selectedPlayers)
            {
                RTSMovementController movement = player.GetComponent<RTSMovementController>();
                if (movement != null)
                {
                    movement.SetTargetPosition(hit.point);
                }
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"Moving {selectedPlayers.Count} unit(s) to {hit.point}");
            }
        }
    }
    
    void ClearPersistentHighlight()
    {
        if (currentlyHighlightedEnemy != null)
        {
            currentlyHighlightedEnemy.SetPersistentHighlight(false);
            currentlyHighlightedEnemy = null;
        }
        // Also clear any SelectionCircle persistent flags in the scene (limited scope: last attacked only)
        // We won't search globally; rely on setting a new one on next attack.
    }
    
    
    PlayerSelectionController GetPlayerAtMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            if (showDebugInfo)
            {
                Debug.Log($"Hit object: {hitObject.name}, Tag: {hitObject.tag}");
            }
            
            // Check if it's a player or allied unit
            if (hitObject.CompareTag(playerTag) || hitObject.CompareTag(alliedTag))
            {
                PlayerSelectionController psc = hitObject.GetComponent<PlayerSelectionController>();
                if (psc == null && showDebugInfo)
                {
                    Debug.Log($"Player {hitObject.name} doesn't have PlayerSelectionController component!");
                }
                return psc;
            }
        }
        else if (showDebugInfo)
        {
            Debug.Log("No object hit at mouse position");
        }
        
        return null;
    }
    
    void SelectPlayer(PlayerSelectionController player)
    {
        if (!selectedPlayers.Contains(player))
        {
            selectedPlayers.Add(player);
			player.SetSelected(true);
			// Prefer SelectionCircle for selection visuals if present
			SelectionCircle circle = player.GetComponent<SelectionCircle>();
			if (circle != null)
			{
				circle.SetSelected(true);
			}
        }
    }
    
    void DeselectAllPlayers()
    {
        ClearPersistentHighlight();
		ClearAllDragHover();
        
        foreach (PlayerSelectionController player in selectedPlayers)
        {
            if (player != null)
            {
				player.SetSelected(false);
				SelectionCircle circle = player.GetComponent<SelectionCircle>();
				if (circle != null)
				{
					circle.SetSelected(false);
					circle.SetDragHover(false); // clear drag-hover when deselecting
				}
            }
        }
        selectedPlayers.Clear();
    }

	void UpdateDragHover(Vector2 startPos, Vector2 endPos)
	{
		// Recompute hover set and toggle circles efficiently
		Rect selectionRect = GetScreenRect(startPos, endPos);
		HashSet<GameObject> newHover = new HashSet<GameObject>();
		
		// Consider players and allies
		List<GameObject> allUnits = new List<GameObject>();
		allUnits.AddRange(GameObject.FindGameObjectsWithTag(playerTag));
		allUnits.AddRange(GameObject.FindGameObjectsWithTag(alliedTag));
		
		foreach (GameObject unit in allUnits)
		{
			if (unit == null) continue;
			Vector3 screenPos = mainCamera.WorldToScreenPoint(unit.transform.position);
			if (selectionRect.Contains(screenPos))
			{
				newHover.Add(unit);
				if (!dragHoverUnits.Contains(unit))
				{
					SelectionCircle c = unit.GetComponent<SelectionCircle>();
					if (c != null) c.SetDragHover(true);
				}
			}
		}
		// Turn off for units no longer hovered
		foreach (GameObject unit in dragHoverUnits)
		{
			if (!newHover.Contains(unit))
			{
				SelectionCircle c = unit != null ? unit.GetComponent<SelectionCircle>() : null;
				if (c != null) c.SetDragHover(false);
			}
		}
		dragHoverUnits = newHover;
	}

	void ClearAllDragHover()
	{
		foreach (GameObject unit in dragHoverUnits)
		{
			SelectionCircle c = unit != null ? unit.GetComponent<SelectionCircle>() : null;
			if (c != null) c.SetDragHover(false);
		}
		dragHoverUnits.Clear();
	}
    
    void OnGUI()
    {
        if (isDragging)
        {
            // Draw selection rectangle
            Rect rect = GetScreenRect(dragStartPosition, dragCurrentPosition);
            
            // Convert to GUI coordinates (inverted Y)
            rect.y = Screen.height - rect.y - rect.height;
            
            // Draw filled rectangle
            GUI.DrawTexture(rect, selectionTexture);
            
            // Draw border
            // Top
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, borderWidth), borderTexture);
            // Bottom
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - borderWidth, rect.width, borderWidth), borderTexture);
            // Left
            GUI.DrawTexture(new Rect(rect.x, rect.y, borderWidth, rect.height), borderTexture);
            // Right
            GUI.DrawTexture(new Rect(rect.x + rect.width - borderWidth, rect.y, borderWidth, rect.height), borderTexture);
        }
    }
    
    void OnDestroy()
    {
        // Clean up textures
        if (selectionTexture != null)
        {
            Destroy(selectionTexture);
        }
        if (borderTexture != null)
        {
            Destroy(borderTexture);
        }
    }
    
    // Public methods for external access
    public List<PlayerSelectionController> GetSelectedPlayers()
    {
        return new List<PlayerSelectionController>(selectedPlayers);
    }
    
    public int GetSelectedCount()
    {
        return selectedPlayers.Count;
    }
}
