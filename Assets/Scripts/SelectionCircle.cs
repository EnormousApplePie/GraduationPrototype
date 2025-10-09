using UnityEngine;

/// <summary>
/// Draws and animates a circular ring under a unit for hover/selection feedback.
/// Uses a LineRenderer to approximate a circle on the XZ plane and supports pulsing animation.
/// </summary>
public class SelectionCircle : MonoBehaviour
{
	[Header("Appearance")]
	public float baseRadius = 0.6f;
	public int segments = 48;
	public float lineWidth = 0.06f;
	public Color hoverColor = new Color(1f, 0.85f, 0.1f, 0.9f);
	public Color selectedColor = new Color(0.2f, 0.9f, 0.2f, 1f);
	public float heightOffset = 0.02f;

	[Header("Anchor / Offset")]
	public Transform anchor; // Optional anchor transform (e.g., a child at feet)
	public bool useLocalOffset = true; // Offset in local XZ space (respects rotation)
	public Vector2 offsetXZ = Vector2.zero; // XZ offset for centering the ring at feet

	[Header("Local Plane Offset (preferred)")]
	public float offsetForward = 0f; // +forward/-back in local Z
	public float offsetRight = 0f;   // +right/-left in local X

	[Header("Orientation")]
	public Transform rotationTarget; // Optional transform to match rotation (e.g., sprite root rotated 45°)
	public bool matchSpriteRotation = true; // If true, auto-uses first SpriteRenderer's transform
	public Vector3 extraLocalEuler = Vector3.zero; // Additional rotation tweaks for the ring object

	[Header("Animation")]
	public bool enablePulse = true;
	public float pulseAmplitude = 0.08f; // +/- scale on radius
	public float pulseSpeed = 3f; // cycles per second

	[Header("Sorting / Rendering Order")]
	public string sortingLayerName = "Default";
	public int sortingOrderOffsetBelowSprite = 10; // how far below the sprite to render

	[Header("Visibility Toggles")]
	public bool enableHover = true; // allow mouse-hover visibility
	public bool enableDragHover = true; // allow drag-rectangle hover visibility
	public bool enablePersistent = true; // allow persistent highlight visibility

	private LineRenderer lineRenderer;
	private Transform ringRoot; // Child transform that holds the LineRenderer
	private float[] cachedAngles;
	private Vector3[] ringPositions;
	private bool hoveredByMouse = false;
	private bool hoveredByDrag = false;
	private bool persistent = false;
	private bool isSelected = false;
	private float time;
	private SpriteRenderer referenceSprite;

	void Awake()
	{
		EnsureLineRenderer();
	}

	void Start()
	{
		// Try to find a reference sprite on this object or children to match sorting
		referenceSprite = GetComponentInChildren<SpriteRenderer>();
		if (referenceSprite != null)
		{
			ApplySortingFrom(referenceSprite);
			// Estimate a radius from sprite bounds if baseRadius not set explicitly
			if (baseRadius <= 0.01f)
			{
				baseRadius = Mathf.Max(referenceSprite.bounds.extents.x, referenceSprite.bounds.extents.z);
				if (baseRadius < 0.35f) baseRadius = 0.35f; // sensible minimum
			}
		}
		BuildAngleCache();
		UpdateVisibility();
	}

	void Update()
	{
		if (!lineRenderer.enabled) return;
		time += Time.deltaTime;
		float radius = baseRadius;
		if (enablePulse)
		{
			float pulse = Mathf.Sin(time * Mathf.PI * 2f * pulseSpeed) * pulseAmplitude;
			// Stronger pulse when selected
			float selectedBoost = isSelected ? 1.0f : 0.5f;
			radius *= 1f + pulse * selectedBoost;
		}
		UpdateRingTransform();
		UpdateRingPositions(radius);
	}

	void EnsureLineRenderer()
	{
		if (lineRenderer == null)
		{
			Transform existing = transform.Find("SelectionCircleRenderer");
			if (existing == null)
			{
				GameObject go = new GameObject("SelectionCircleRenderer");
				go.transform.SetParent(transform);
				go.transform.localPosition = Vector3.zero;
				go.transform.localRotation = Quaternion.identity;
				go.transform.localScale = Vector3.one;
				ringRoot = go.transform;
			}
			else
			{
				ringRoot = existing;
			}
			lineRenderer = ringRoot.GetComponent<LineRenderer>();
			if (lineRenderer == null)
			{
				lineRenderer = ringRoot.gameObject.AddComponent<LineRenderer>();
			}
			lineRenderer.loop = true;
			lineRenderer.useWorldSpace = false; // Build the ring in local space of ringRoot
			lineRenderer.positionCount = Mathf.Max(segments, 3);
			lineRenderer.startWidth = lineWidth;
			lineRenderer.endWidth = lineWidth;
			lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			lineRenderer.receiveShadows = false;
			lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
			// Align line width/orientation with the ring object's Z axis, not the camera
			lineRenderer.alignment = LineAlignment.TransformZ;
		}
	}

	void BuildAngleCache()
	{
		segments = Mathf.Max(segments, 3);
		if (cachedAngles == null || cachedAngles.Length != segments)
		{
			cachedAngles = new float[segments];
			ringPositions = new Vector3[segments];
		}
		float step = Mathf.PI * 2f / segments;
		for (int i = 0; i < segments; i++)
		{
			cachedAngles[i] = step * i;
		}
		lineRenderer.positionCount = segments;
	}

	void UpdateRingTransform()
	{
		if (ringRoot == null) return;
		// Determine rotation target
		Transform rotTarget = rotationTarget;
		if (rotTarget == null && matchSpriteRotation && referenceSprite != null)
		{
			rotTarget = referenceSprite.transform;
		}
		Quaternion targetLocalRotation = rotTarget != null ? rotTarget.localRotation : Quaternion.identity;
		targetLocalRotation *= Quaternion.Euler(extraLocalEuler);
		ringRoot.localRotation = targetLocalRotation;

		// Compute world position using anchor, offset, and a small push along ring's local forward
		Vector3 baseWorld = anchor != null ? anchor.position : transform.position;
		// Preferred local-plane offsets (right/forward)
		Vector3 localPlaneOffset = new Vector3(offsetRight, 0f, offsetForward);
		Vector3 worldOffset = useLocalOffset ? transform.TransformVector(localPlaneOffset) : localPlaneOffset;
		// Backward-compat: also apply legacy offsetXZ if non-zero
		if (offsetXZ.sqrMagnitude > 0f)
		{
			Vector3 legacyLocal = new Vector3(offsetXZ.x, 0f, offsetXZ.y);
			worldOffset += useLocalOffset ? transform.TransformVector(legacyLocal) : legacyLocal;
		}
		Vector3 ringForwardWorld = transform.rotation * targetLocalRotation * Vector3.forward;
		Vector3 worldLift = ringForwardWorld * heightOffset;
		Vector3 worldPos = baseWorld + worldOffset + worldLift;
		ringRoot.position = worldPos;
	}

	void UpdateRingPositions(float radius)
	{
		// Build ring in ringRoot local XY plane (Z is normal)
		for (int i = 0; i < segments; i++)
		{
			float a = cachedAngles[i];
			float x = Mathf.Cos(a) * radius;
			float y = Mathf.Sin(a) * radius;
			ringPositions[i] = new Vector3(x, y, 0f);
		}
		lineRenderer.SetPositions(ringPositions);
	}

	void UpdateVisibility()
	{
		bool isHovered = (enableHover && hoveredByMouse) || (enableDragHover && hoveredByDrag);
		bool visible = isSelected || isHovered || (enablePersistent && persistent);
		lineRenderer.enabled = visible;
		if (visible)
		{
			lineRenderer.startColor = lineRenderer.endColor = (isSelected ? selectedColor : hoverColor);
		}
	}

	public void SetSelected(bool selected)
	{
		isSelected = selected;
		UpdateVisibility();
	}

	public void SetHovered(bool hovered)
	{
		hoveredByMouse = hovered;
		UpdateVisibility();
	}

	public void SetDragHover(bool hovered)
	{
		hoveredByDrag = hovered;
		UpdateVisibility();
	}

	public void SetPersistent(bool value)
	{
		persistent = value;
		UpdateVisibility();
	}

	public void ApplySortingFrom(SpriteRenderer sprite)
	{
		if (sprite == null || lineRenderer == null) return;
		var rend = lineRenderer.GetComponent<Renderer>();
		rend.sortingLayerID = sprite.sortingLayerID;
		rend.sortingLayerName = sprite.sortingLayerName;
		rend.sortingOrder = sprite.sortingOrder - sortingOrderOffsetBelowSprite;
	}
}


