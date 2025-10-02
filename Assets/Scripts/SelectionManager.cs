using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Manages player selection and handles input for the new RTS system
/// </summary>
public class SelectionManager : MonoBehaviour
{
    [Header("Selection Settings")]
    public LayerMask playerLayerMask = -1;
    public string playerTag = "Player";
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    private List<PlayerSelectionController> selectedPlayers = new List<PlayerSelectionController>();
    private List<RTSMovementController> selectedPlayerMovements = new List<RTSMovementController>();
    private Camera mainCamera;
    private float lastClickTime = 0f;
    private const float doubleClickTime = 0.3f;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("SelectionManager: No camera found! Please ensure there's a camera in the scene.");
        }
    }
    
    void Update()
    {
        HandleInput();
    }
    
    void HandleInput()
    {
        if (mainCamera == null) return;
        
        // Left click - selection
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleLeftClick();
        }
        
        // Right click - movement/attack (only if players are selected)
        if (Mouse.current.rightButton.wasPressedThisFrame && selectedPlayers.Count > 0)
        {
            HandleRightClick();
        }
    }
    
    void HandleLeftClick()
    {
        // Check if we clicked on a player
        PlayerSelectionController clickedPlayer = GetPlayerAtMouse();
        
        if (clickedPlayer != null)
        {
            // Check for double-click
            float currentTime = Time.time;
            bool isDoubleClick = (currentTime - lastClickTime) < doubleClickTime;
            lastClickTime = currentTime;
            
            if (isDoubleClick && selectedPlayers.Contains(clickedPlayer))
            {
                // Double-click on selected player - follow with camera
                CameraController cameraController = mainCamera.GetComponent<CameraController>();
                if (cameraController != null)
                {
                    cameraController.SetFollowTarget(clickedPlayer.transform);
                }
                
                if (showDebugInfo)
                {
                    Debug.Log($"Double-clicked player: {clickedPlayer.gameObject.name} - following with camera");
                }
            }
            else
            {
                // Handle selection based on shift key
                if (Keyboard.current.leftShiftKey.isPressed)
                {
                    // Shift+click - add/remove from selection
                    if (selectedPlayers.Contains(clickedPlayer))
                    {
                        RemovePlayerFromSelection(clickedPlayer);
                    }
                    else
                    {
                        AddPlayerToSelection(clickedPlayer);
                    }
                }
                else
                {
                    // Single click - select only this player
                    SelectPlayer(clickedPlayer);
                }
                
                if (showDebugInfo)
                {
                    Debug.Log($"Selected players: {selectedPlayers.Count}");
                }
            }
        }
        else
        {
            // Deselect all players
            DeselectAllPlayers();
            
            if (showDebugInfo)
            {
                Debug.Log("Deselected all players");
            }
        }
    }
    
    void HandleRightClick()
    {
        if (selectedPlayers.Count == 0) return;
        
        // Check if we clicked on an enemy
        Transform enemy = GetEnemyAtMouse();
        
        if (enemy != null)
        {
            // Move all selected players to enemy and attack
            foreach (RTSMovementController movement in selectedPlayerMovements)
            {
                if (movement != null)
                {
                    movement.SetEnemyTarget(enemy);
                }
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"Right-clicked enemy: {enemy.name} - {selectedPlayers.Count} players attacking");
            }
        }
        else
        {
            // Move all selected players to clicked position
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            if (mouseWorldPos != Vector3.zero)
            {
                foreach (RTSMovementController movement in selectedPlayerMovements)
                {
                    if (movement != null)
                    {
                        movement.SetTargetPosition(mouseWorldPos);
                        movement.ClearEnemyTarget();
                    }
                }
                
                if (showDebugInfo)
                {
                    Debug.Log($"Right-clicked position: {mouseWorldPos} - {selectedPlayers.Count} players moving");
                }
            }
        }
    }
    
    void AddPlayerToSelection(PlayerSelectionController player)
    {
        if (!selectedPlayers.Contains(player))
        {
            selectedPlayers.Add(player);
            player.SetSelected(true);
            
            RTSMovementController movement = player.GetComponent<RTSMovementController>();
            if (movement != null)
            {
                selectedPlayerMovements.Add(movement);
            }
        }
    }
    
    void RemovePlayerFromSelection(PlayerSelectionController player)
    {
        if (selectedPlayers.Contains(player))
        {
            selectedPlayers.Remove(player);
            player.SetSelected(false);
            
            RTSMovementController movement = player.GetComponent<RTSMovementController>();
            if (movement != null)
            {
                selectedPlayerMovements.Remove(movement);
            }
        }
    }
    
    PlayerSelectionController GetPlayerAtMouse()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, playerLayerMask))
        {
            // Check if hit object is a player
            if (hit.collider.CompareTag(playerTag))
            {
                return hit.collider.GetComponent<PlayerSelectionController>();
            }
        }
        
        return null;
    }
    
    Transform GetEnemyAtMouse()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            // Check if hit object is an enemy
            if (hit.collider.CompareTag("Enemy"))
            {
                return hit.collider.transform;
            }
        }
        
        return null;
    }
    
    Vector3 GetMouseWorldPosition()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        
        // Use the same approach as FireballLauncher for 2D movement
        Ray cameraRay = mainCamera.ScreenPointToRay(mousePosition);
        
        // Create a horizontal plane at the first selected player's height, or 0 if none selected
        float planeY = selectedPlayers.Count > 0 ? selectedPlayers[0].transform.position.y : 0f;
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, planeY, 0));
        
        // Find where the camera ray intersects the ground plane
        if (groundPlane.Raycast(cameraRay, out float distance))
        {
            // Get the intersection point on the ground plane
            Vector3 targetPoint = cameraRay.GetPoint(distance);
            
            // Force horizontal movement (only X-Z, no Y)
            targetPoint.y = planeY;
            
            return targetPoint;
        }
        else
        {
            // Fallback: use camera forward direction projected onto X-Z plane
            Vector3 fallbackDirection = mainCamera.transform.forward;
            fallbackDirection.y = 0f;
            Vector3 fallbackPoint = (selectedPlayers.Count > 0 ? selectedPlayers[0].transform.position : Vector3.zero) + fallbackDirection * 5f;
            
            return fallbackPoint;
        }
    }
    
    void SelectPlayer(PlayerSelectionController player)
    {
        // Deselect all current players
        DeselectAllPlayers();
        
        // Select new player
        AddPlayerToSelection(player);
    }
    
    void DeselectAllPlayers()
    {
        foreach (PlayerSelectionController player in selectedPlayers)
        {
            if (player != null)
            {
                player.SetSelected(false);
            }
        }
        
        selectedPlayers.Clear();
        selectedPlayerMovements.Clear();
    }
    
    /// <summary>
    /// Get all currently selected players
    /// </summary>
    public List<PlayerSelectionController> GetSelectedPlayers()
    {
        return new List<PlayerSelectionController>(selectedPlayers);
    }
    
    /// <summary>
    /// Get the first selected player (for backward compatibility)
    /// </summary>
    public PlayerSelectionController GetSelectedPlayer()
    {
        return selectedPlayers.Count > 0 ? selectedPlayers[0] : null;
    }
    
    /// <summary>
    /// Check if any players are selected
    /// </summary>
    public bool HasSelectedPlayers()
    {
        return selectedPlayers.Count > 0;
    }
    
    /// <summary>
    /// Get the number of selected players
    /// </summary>
    public int GetSelectedPlayerCount()
    {
        return selectedPlayers.Count;
    }
}
