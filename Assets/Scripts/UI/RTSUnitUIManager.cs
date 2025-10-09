using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Simple runtime UI: shows a left grid of owned units and a selected unit panel.
/// Hooks into SelectionManager to refresh selected unit. Uses world state each second to refresh the roster.
/// </summary>
public class RTSUnitUIManager : MonoBehaviour
{
	[Header("References")]
	public SelectionManager selectionManager;
	public RectTransform leftGridRoot; // Parent for unit icons
	public RectTransform selectedPanelRoot; // Parent for selected unit details

	[Header("Prefabs")] 
	public GameObject unitButtonPrefab; // Simple button with Image and Text
	public Text selectedNameText;
	public Text selectedStatsText; // e.g. HP, Level

	[Header("Layout")] 
	public int maxColumns = 1;
	public float cellHeight = 60f;
	public float cellSpacing = 8f;

	private readonly List<GameObject> pooledButtons = new List<GameObject>();
	private float rosterRefreshTimer = 0f;
	private const float RosterRefreshInterval = 1f;

	void Awake()
	{
		if (selectionManager == null)
		{
			selectionManager = FindFirstObjectByType<SelectionManager>();
		}
	}

	void Update()
	{
		UpdateSelectedPanel();
		rosterRefreshTimer += Time.deltaTime;
		if (rosterRefreshTimer >= RosterRefreshInterval)
		{
			rosterRefreshTimer = 0f;
			RefreshRosterGrid();
		}
	}

	void UpdateSelectedPanel()
	{
		if (selectedNameText == null || selectedStatsText == null || selectionManager == null) return;
		var selected = selectionManager.GetSelectedPlayers();
		if (selected.Count > 0 && selected[0] != null)
		{
			BaseCharacter c = selected[0].GetComponent<BaseCharacter>();
			if (c != null)
			{
				selectedNameText.text = string.IsNullOrEmpty(c.displayName) ? c.gameObject.name : c.displayName;
				selectedStatsText.text = $"Lvl {c.level}\nHP {Mathf.CeilToInt(c.currentHealth)}/{Mathf.CeilToInt(c.maxHealth)}";
				selectedPanelRoot?.gameObject.SetActive(true);
				return;
			}
		}
		selectedPanelRoot?.gameObject.SetActive(false);
	}

	void RefreshRosterGrid()
	{
		if (leftGridRoot == null) return;
		// Find all owned units (Player + Allied)
		List<BaseCharacter> units = new List<BaseCharacter>();
		CollectUnitsWithTag("Player", units);
		CollectUnitsWithTag("Allied", units);

		EnsureButtonPool(units.Count);
		for (int i = 0; i < pooledButtons.Count; i++)
		{
			bool active = i < units.Count;
			pooledButtons[i].SetActive(active);
			if (!active) continue;
			var u = units[i];
			// Position
			int row = i; // single column for now
			RectTransform rt = (RectTransform)pooledButtons[i].transform;
			rt.anchoredPosition = new Vector2(0f, -row * (cellHeight + cellSpacing));
			// Populate
			var text = pooledButtons[i].GetComponentInChildren<Text>();
			if (text != null)
			{
				string name = string.IsNullOrEmpty(u.displayName) ? u.gameObject.name : u.displayName;
				text.text = $"{name}\nLvl {u.level}";
			}
			var btn = pooledButtons[i].GetComponent<Button>();
			if (btn != null)
			{
				btn.onClick.RemoveAllListeners();
				var psc = u.GetComponent<PlayerSelectionController>();
				if (psc != null)
				{
					btn.onClick.AddListener(() => {
						if (selectionManager == null) return;
						// Select only this unit
						var list = selectionManager.GetSelectedPlayers();
						// Deselect all via SelectionManager method
						var selMgr = selectionManager;
						// emulate single click selection
						selMgr.SendMessage("DeselectAllPlayers", SendMessageOptions.DontRequireReceiver);
						selMgr.SendMessage("SelectPlayer", psc, SendMessageOptions.DontRequireReceiver);
					});
				}
			}
		}
	}

	void EnsureButtonPool(int count)
	{
		while (pooledButtons.Count < count)
		{
			GameObject go = Instantiate(unitButtonPrefab, leftGridRoot);
			RectTransform rt = (RectTransform)go.transform;
			rt.anchorMin = new Vector2(0f, 1f);
			rt.anchorMax = new Vector2(0f, 1f);
			rt.pivot = new Vector2(0f, 1f);
			rt.sizeDelta = new Vector2(leftGridRoot.rect.width, cellHeight);
			pooledButtons.Add(go);
		}
	}

	void CollectUnitsWithTag(string tag, List<BaseCharacter> list)
	{
		var objs = GameObject.FindGameObjectsWithTag(tag);
		for (int i = 0; i < objs.Length; i++)
		{
			var c = objs[i].GetComponent<BaseCharacter>();
			if (c != null && c.IsAlive()) list.Add(c);
		}
	}
}


