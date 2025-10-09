using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Centralized hover highlighter: raycasts from the mouse each frame and toggles
/// SimpleGlowEffect hover state on the unit under the cursor. Works alongside
/// SelectionManager's persistent highlight and avoids per-object raycasts.
/// </summary>
public class HoverManager : MonoBehaviour
{
	[Header("Hover Targets")]
	public string[] hoverableTags = { "Player", "Allied", "Enemy", "Friendly" };
	public bool includeUntagged = false; // Allow hover on untagged if they have SimpleGlowEffect

	[Header("Raycast Settings")]
	public float maxRayDistance = 1000f;
	public LayerMask raycastLayers = ~0; // All layers by default

	[Header("Debug")]
	public bool showDebugInfo = false;

	private Camera mainCamera;
	// Deprecated hover systems
	private SimpleGlowEffect currentHoverGlow;
	private PlayerSelectionController currentHoverSelection;
	private Transform currentHoverRoot;

	void Start()
	{
		mainCamera = Camera.main;
		if (mainCamera == null)
		{
			mainCamera = FindFirstObjectByType<Camera>();
		}
	}

	void Update()
	{
		if (mainCamera == null || Mouse.current == null)
		{
			return;
		}

		// Raycast from mouse to world
		Vector2 mousePos = Mouse.current.position.ReadValue();
		Ray ray = mainCamera.ScreenPointToRay(mousePos);
		if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, raycastLayers))
		{
			HandleHoverHit(hit.collider.transform);
		}
		else
		{
			ClearCurrentHover();
		}
	}

    void HandleHoverHit(Transform hitTransform)
	{
        // Find a root transform that carries a SelectionCircle / controller
        Transform root = FindHoverRoot(hitTransform);
		if (root == null)
		{
			ClearCurrentHover();
			return;
		}

        // Ignore if no acceptable tag found anywhere up the chain (unless includeUntagged)
        if (!IsTransformHoverable(hitTransform))
		{
			ClearCurrentHover();
			return;
		}

		// If hovering a new object, swap states
        if (root != currentHoverRoot)
		{
			ClearCurrentHover();
			currentHoverRoot = root;
        }

        // Refresh hover each frame for current object
        SelectionCircle circle = currentHoverRoot != null ? currentHoverRoot.GetComponent<SelectionCircle>() : null;
        if (circle != null)
        {
            circle.SetHovered(true);
        }
        // Turn off legacy hover systems
        currentHoverGlow = null;
        currentHoverSelection = null;
        if (showDebugInfo)
        {
            Debug.Log($"[HoverManager] Hover ON -> {currentHoverRoot?.name}");
        }
	}

	bool IsTagHoverable(string tag)
	{
		if (includeUntagged && tag == "Untagged") return true;
		for (int i = 0; i < hoverableTags.Length; i++)
		{
			if (tag == hoverableTags[i]) return true;
		}
		return false;
	}

    Transform FindHoverRoot(Transform start)
	{
		Transform t = start;
		while (t != null)
		{
            // Prefer a transform that actually owns one of the components
            if (t.GetComponent<SelectionCircle>() != null || t.GetComponent<PlayerSelectionController>() != null || t.GetComponent<SimpleGlowEffect>() != null)
			{
				return t;
			}
			t = t.parent;
		}
        // If nothing found in parents, try children of the original hit for robustness
        SelectionCircle childCircle = start.GetComponentInChildren<SelectionCircle>();
        if (childCircle != null) return childCircle.transform;
        PlayerSelectionController childPsc = start.GetComponentInChildren<PlayerSelectionController>();
        if (childPsc != null) return childPsc.transform;
        return null;
	}

    bool IsTransformHoverable(Transform start)
    {
        Transform t = start;
        while (t != null)
        {
            if (IsTagHoverable(t.tag)) return true;
            t = t.parent;
        }
        return includeUntagged; // allow untagged if enabled
    }

	void ClearCurrentHover()
	{
		// Legacy hover off - no-op
		SelectionCircle circle = currentHoverRoot != null ? currentHoverRoot.GetComponent<SelectionCircle>() : null;
		if (circle != null)
		{
			circle.SetHovered(false);
		}
		currentHoverGlow = null;
		currentHoverSelection = null;
		currentHoverRoot = null;
		if (showDebugInfo)
		{
			Debug.Log($"[HoverManager] Hover OFF -> {currentHoverRoot?.name}");
		}
	}

	void OnDisable()
	{
		ClearCurrentHover();
	}
}


