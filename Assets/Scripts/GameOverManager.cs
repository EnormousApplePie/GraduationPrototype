using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Shows a full-screen end screen when either all owned units (Player/Allied) die
/// or when all Enemy units die. Includes a Reset button to reload the current scene.
/// </summary>
public class GameOverManager : MonoBehaviour
{
	[Header("Teams")]
	public string[] ownedTags = { "Player", "Allied" };
	public string enemyTag = "Enemy";

	[Header("Hardcoded Counters (temporary)")]
	public bool useHardcodedCounts = true;
	public int expectedOwnedCount = 2;
	public int expectedEnemyCount = 14;
	private int ownedRemaining = 0;
	private int enemyRemaining = 0;

	[Header("Overlay UI (optional)")]
	public Canvas overlayCanvas; // If null, will be created at runtime
	public Text endText; // Big title text
	public Button resetButton;
	public Image background;
	[Tooltip("If true and no prefab is found, the manager creates a runtime overlay.")]
	public bool createOverlayIfMissing = false;
	[Header("Auto-Find Settings")]
	public string overlayRootName = "GameOverUI"; // Name of the prefab root in scene

	private readonly HashSet<BaseCharacter> ownedAlive = new HashSet<BaseCharacter>();
	private readonly HashSet<BaseCharacter> enemyAlive = new HashSet<BaseCharacter>();
	private bool gameOverShown = false;

	void Start()
	{
		// Find all characters and subscribe to death
		CollectCharacters();
		// Initialize counters
		if (useHardcodedCounts)
		{
			ownedRemaining = expectedOwnedCount;
			enemyRemaining = expectedEnemyCount;
		}
		else
		{
			ownedRemaining = ownedAlive.Count;
			enemyRemaining = enemyAlive.Count;
		}
		// Build overlay if needed
		EnsureOverlay();
		HideOverlay();
	}

	void CollectCharacters()
	{
		ownedAlive.Clear();
		enemyAlive.Clear();
		// Owned
		for (int t = 0; t < ownedTags.Length; t++)
		{
			var objs = GameObject.FindGameObjectsWithTag(ownedTags[t]);
			for (int i = 0; i < objs.Length; i++)
			{
				var c = objs[i].GetComponent<BaseCharacter>();
				if (c != null && c.IsAlive())
				{
					if (!ownedAlive.Contains(c))
					{
						ownedAlive.Add(c);
						c.OnDeath += () => OnCharacterDeath(c, true);
					}
				}
			}
		}
		// Enemies
		var enemyObjs = GameObject.FindGameObjectsWithTag(enemyTag);
		for (int i = 0; i < enemyObjs.Length; i++)
		{
			var c = enemyObjs[i].GetComponent<BaseCharacter>();
			if (c != null && c.IsAlive())
			{
				if (!enemyAlive.Contains(c))
				{
					enemyAlive.Add(c);
					c.OnDeath += () => OnCharacterDeath(c, false);
				}
			}
		}
	}

	void OnCharacterDeath(BaseCharacter c, bool isOwned)
	{
		if (useHardcodedCounts)
		{
			if (isOwned)
			{
				ownedRemaining = Mathf.Max(ownedRemaining - 1, 0);
				if (ownedRemaining == 0)
				{
					ShowOverlay("End of prototype\nAll units lost");
				}
			}
			else
			{
				enemyRemaining = Mathf.Max(enemyRemaining - 1, 0);
				if (enemyRemaining == 0)
				{
					ShowOverlay("End of prototype\nAll enemies defeated");
				}
			}
			return;
		}
		
		// Non-hardcoded: track from alive sets
		if (isOwned)
		{
			ownedAlive.Remove(c);
			if (ownedAlive.Count == 0)
			{
				ShowOverlay("End of prototype\nAll units lost");
			}
		}
		else
		{
			enemyAlive.Remove(c);
			if (enemyAlive.Count == 0)
			{
				ShowOverlay("End of prototype\nAll enemies defeated");
			}
		}
	}

	void EnsureOverlay()
	{
		if (overlayCanvas != null && endText != null && resetButton != null && background != null) return;
		// Try to find an existing prefab in the scene
		TryAcquireOverlayFromScene();
		if (overlayCanvas != null)
		{
			// Make sure button resets level in case prefab has no wiring
			if (resetButton != null)
			{
				resetButton.onClick.AddListener(ResetLevel);
			}
			return;
		}
		// Optionally create a simple overlay if none was found
		if (!createOverlayIfMissing)
		{
			Debug.LogWarning("GameOverManager: No GameOverUI prefab found in scene and creation is disabled.");
			return;
		}
		// Create a simple overlay Canvas and children
		GameObject canvasGO = new GameObject("GameOverOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		canvasGO.transform.SetParent(transform, false);
		overlayCanvas = canvasGO.GetComponent<Canvas>();
		overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
		var scaler = canvasGO.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920, 1080);

		// Background
		GameObject bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
		bgGO.transform.SetParent(canvasGO.transform, false);
		background = bgGO.GetComponent<Image>();
		background.color = new Color(0f, 0f, 0f, 0.7f);
		var bgRT = bgGO.GetComponent<RectTransform>();
		bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;

		// Title text
		GameObject textGO = new GameObject("EndText", typeof(RectTransform), typeof(Text));
		textGO.transform.SetParent(bgGO.transform, false);
		endText = textGO.GetComponent<Text>();
		endText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		endText.fontSize = 48; endText.alignment = TextAnchor.MiddleCenter; endText.color = Color.white;
		var textRT = textGO.GetComponent<RectTransform>();
		textRT.anchorMin = new Vector2(0.5f, 0.6f); textRT.anchorMax = new Vector2(0.5f, 0.6f); textRT.pivot = new Vector2(0.5f, 0.5f);
		textRT.sizeDelta = new Vector2(800, 200);

		// Reset button
		GameObject btnGO = new GameObject("ResetButton", typeof(RectTransform), typeof(Image), typeof(Button));
		btnGO.transform.SetParent(bgGO.transform, false);
		resetButton = btnGO.GetComponent<Button>();
		var btnImg = btnGO.GetComponent<Image>(); btnImg.color = new Color(0.2f, 0.6f, 0.2f, 0.9f);
		var btnRT = btnGO.GetComponent<RectTransform>();
		btnRT.anchorMin = new Vector2(0.5f, 0.4f); btnRT.anchorMax = new Vector2(0.5f, 0.4f); btnRT.pivot = new Vector2(0.5f, 0.5f);
		btnRT.sizeDelta = new Vector2(240, 64);
		// Button label
		GameObject btnTxtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
		btnTxtGO.transform.SetParent(btnGO.transform, false);
		var btnTxt = btnTxtGO.GetComponent<Text>(); btnTxt.text = "Reset Level"; btnTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		btnTxt.fontSize = 24; btnTxt.alignment = TextAnchor.MiddleCenter; btnTxt.color = Color.white;
		var btnTxtRT = btnTxtGO.GetComponent<RectTransform>(); btnTxtRT.anchorMin = Vector2.zero; btnTxtRT.anchorMax = Vector2.one; btnTxtRT.offsetMin = Vector2.zero; btnTxtRT.offsetMax = Vector2.zero;

		resetButton.onClick.AddListener(ResetLevel);
	}

	void TryAcquireOverlayFromScene()
	{
		// Find by name first
		GameObject root = GameObject.Find(overlayRootName);
		if (root == null)
		{
			// Fallback: look for any ResetLevelButton and take its canvas
			var reset = Object.FindFirstObjectByType<ResetLevelButton>();
			if (reset != null) root = reset.gameObject;
		}
		if (root == null) return;
		overlayCanvas = root.GetComponentInParent<Canvas>();
		if (overlayCanvas == null) overlayCanvas = root.GetComponent<Canvas>();
		// Find parts by name if not assigned
		if (background == null)
		{
			var bgTr = FindChildRecursive(overlayCanvas.transform, "Background");
			if (bgTr != null) background = bgTr.GetComponent<Image>();
		}
		if (endText == null)
		{
			var titleTr = FindChildRecursive(overlayCanvas.transform, "Title");
			if (titleTr != null) endText = titleTr.GetComponent<Text>();
		}
		if (resetButton == null)
		{
			var btnTr = FindChildRecursive(overlayCanvas.transform, "ResetButton");
			if (btnTr != null) resetButton = btnTr.GetComponent<Button>();
		}
	}

	Transform FindChildRecursive(Transform parent, string name)
	{
		for (int i = 0; i < parent.childCount; i++)
		{
			var child = parent.GetChild(i);
			if (child.name == name) return child;
			var deep = FindChildRecursive(child, name);
			if (deep != null) return deep;
		}
		return null;
	}

	void ShowOverlay(string message)
	{
		if (gameOverShown) return;
		gameOverShown = true;
		if (overlayCanvas == null) EnsureOverlay();
		if (overlayCanvas != null) overlayCanvas.gameObject.SetActive(true);
		if (endText != null) endText.text = message;
	}

	void HideOverlay()
	{
		if (overlayCanvas != null) overlayCanvas.gameObject.SetActive(false);
	}

	public void ResetLevel()
	{
		Time.timeScale = 1f;
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}
}


