using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CreateRTSUI
{
	[MenuItem("Tools/RTS UI/Create UI Prefabs")] 
	public static void CreateUIPrefabs()
	{
		// Ensure folders
		string uiFolder = "Assets/Prefabs/UI";
		if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
		if (!AssetDatabase.IsValidFolder(uiFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

		// Create UnitButton prefab
		GameObject unitButtonGO = new GameObject("UnitButton", typeof(RectTransform), typeof(Image), typeof(Button));
		var unitBtnRT = unitButtonGO.GetComponent<RectTransform>();
		unitBtnRT.sizeDelta = new Vector2(81, 94);
		unitBtnRT.anchorMin = new Vector2(0, 1);
		unitBtnRT.anchorMax = new Vector2(0, 1);
		unitBtnRT.pivot = new Vector2(0, 1);
		var btnImg = unitButtonGO.GetComponent<Image>();
		btnImg.color = new Color(0.12f, 0.12f, 0.12f, 0.8f);
		// Text child
		GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
		txtGO.transform.SetParent(unitButtonGO.transform, false);
		var txtRT = txtGO.GetComponent<RectTransform>();
		txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one; txtRT.offsetMin = new Vector2(6, 6); txtRT.offsetMax = new Vector2(-6, -6);
		var txt = txtGO.GetComponent<Text>();
		txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		txt.fontSize = 18; txt.alignment = TextAnchor.MiddleLeft; txt.color = Color.white; txt.text = "Unit\nLvl 1";
		// Save UnitButton prefab
		string unitButtonPath = uiFolder + "/UnitButton.prefab";
		PrefabUtility.SaveAsPrefabAsset(unitButtonGO, unitButtonPath);
		Object unitButtonPrefab = AssetDatabase.LoadAssetAtPath<Object>(unitButtonPath);
		Object.DestroyImmediate(unitButtonGO);

		// Create RTS UI prefab (Canvas + manager)
		GameObject canvasGO = new GameObject("RTS_UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		var canvas = canvasGO.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		var scaler = canvasGO.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920, 1080);
		scaler.matchWidthOrHeight = 0.5f;

		// UI Manager
		var manager = canvasGO.AddComponent<RTSUnitUIManager>();

		// Left roster panel (Scroll View style)
		GameObject leftPanel = new GameObject("LeftRoster", typeof(RectTransform), typeof(Image), typeof(Mask));
		leftPanel.transform.SetParent(canvasGO.transform, false);
		var leftRT = leftPanel.GetComponent<RectTransform>();
		leftRT.anchorMin = new Vector2(0, 0); leftRT.anchorMax = new Vector2(0, 1);
		leftRT.pivot = new Vector2(0, 1);
		leftRT.sizeDelta = new Vector2(220, 0);
		leftRT.anchoredPosition = new Vector2(10, -10);
		leftPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.3f);
		leftPanel.GetComponent<Mask>().showMaskGraphic = false;
		// Content for roster
		GameObject content = new GameObject("Content", typeof(RectTransform));
		content.transform.SetParent(leftPanel.transform, false);
		var contentRT = content.GetComponent<RectTransform>();
		contentRT.anchorMin = new Vector2(0, 1);
		contentRT.anchorMax = new Vector2(0, 1);
		contentRT.pivot = new Vector2(0, 1);
		contentRT.sizeDelta = new Vector2(220, 1000);
		contentRT.anchoredPosition = Vector2.zero;
		// Optional: add ScrollRect
		var scroll = leftPanel.AddComponent<ScrollRect>();
		var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
		viewport.transform.SetParent(leftPanel.transform, false);
		var viewportRT = viewport.GetComponent<RectTransform>();
		viewportRT.anchorMin = Vector2.zero; viewportRT.anchorMax = Vector2.one; viewportRT.offsetMin = Vector2.zero; viewportRT.offsetMax = Vector2.zero;
		viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);
		viewport.GetComponent<Mask>().showMaskGraphic = false;
		content.transform.SetParent(viewport.transform, false);
		scroll.viewport = viewportRT;
		scroll.content = contentRT;
		scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Elastic;

		// Selected panel
		GameObject selectedPanel = new GameObject("SelectedPanel", typeof(RectTransform), typeof(Image));
		selectedPanel.transform.SetParent(canvasGO.transform, false);
		var selRT = selectedPanel.GetComponent<RectTransform>();
		selRT.anchorMin = new Vector2(0, 0); selRT.anchorMax = new Vector2(0, 0);
		selRT.pivot = new Vector2(0, 0);
		selRT.sizeDelta = new Vector2(260, 120);
		selRT.anchoredPosition = new Vector2(10, 10);
		selectedPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.3f);
		// Name
		GameObject nameTextGO = new GameObject("NameText", typeof(RectTransform), typeof(Text));
		nameTextGO.transform.SetParent(selectedPanel.transform, false);
		var nameRT = nameTextGO.GetComponent<RectTransform>();
		nameRT.anchorMin = new Vector2(0, 1); nameRT.anchorMax = new Vector2(1, 1); nameRT.pivot = new Vector2(0, 1);
		nameRT.anchoredPosition = new Vector2(10, -10); nameRT.sizeDelta = new Vector2(-20, 32);
		var nameText = nameTextGO.GetComponent<Text>();
		nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		nameText.fontSize = 22; nameText.alignment = TextAnchor.UpperLeft; nameText.color = Color.white; nameText.text = "Selected";
		// Stats
		GameObject statsTextGO = new GameObject("StatsText", typeof(RectTransform), typeof(Text));
		statsTextGO.transform.SetParent(selectedPanel.transform, false);
		var statsRT = statsTextGO.GetComponent<RectTransform>();
		statsRT.anchorMin = new Vector2(0, 0); statsRT.anchorMax = new Vector2(1, 1); statsRT.pivot = new Vector2(0, 1);
		statsRT.offsetMin = new Vector2(10, 10); statsRT.offsetMax = new Vector2(-10, -44);
		var statsText = statsTextGO.GetComponent<Text>();
		statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		statsText.fontSize = 18; statsText.alignment = TextAnchor.UpperLeft; statsText.color = Color.white; statsText.text = "Lvl 1\nHP 100/100";

		// Wire manager references
		manager.leftGridRoot = contentRT;
		manager.selectedPanelRoot = selRT;
		manager.selectedNameText = nameText;
		manager.selectedStatsText = statsText;
		manager.unitButtonPrefab = unitButtonPrefab as GameObject;

		// Save RTS UI prefab
		string rtsUIPath = uiFolder + "/RTS_UI.prefab";
		PrefabUtility.SaveAsPrefabAsset(canvasGO, rtsUIPath);
		Object.DestroyImmediate(canvasGO);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		EditorUtility.DisplayDialog("RTS UI", "Created:\n- " + rtsUIPath + "\n- " + unitButtonPath, "OK");
	}

	[MenuItem("Tools/RTS UI/Create Game Over UI Prefab")]
	public static void CreateGameOverUIPrefab()
	{
		string uiFolder = "Assets/Prefabs/UI";
		if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
		if (!AssetDatabase.IsValidFolder(uiFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

		// Canvas root
		GameObject canvasGO = new GameObject("GameOverUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		var canvas = canvasGO.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		var scaler = canvasGO.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920, 1080);
		scaler.matchWidthOrHeight = 0.5f;

		// Dim background
		GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
		bg.transform.SetParent(canvasGO.transform, false);
		var bgImg = bg.GetComponent<Image>();
		bgImg.color = new Color(0f, 0f, 0f, 0.7f);
		var bgRT = (RectTransform)bg.transform;
		bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;

		// Title
		GameObject title = new GameObject("Title", typeof(RectTransform), typeof(Text));
		title.transform.SetParent(bg.transform, false);
		var titleTxt = title.GetComponent<Text>();
		titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		titleTxt.text = "End of prototype";
		titleTxt.fontSize = 56; titleTxt.alignment = TextAnchor.MiddleCenter; titleTxt.color = Color.white;
		var titleRT = (RectTransform)title.transform;
		titleRT.anchorMin = new Vector2(0.5f, 0.65f); titleRT.anchorMax = new Vector2(0.5f, 0.65f);
		titleRT.pivot = new Vector2(0.5f, 0.5f); titleRT.sizeDelta = new Vector2(900, 140);

		// Reset button
		GameObject btn = new GameObject("ResetButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(ResetLevelButton));
		btn.transform.SetParent(bg.transform, false);
		var btnRT = (RectTransform)btn.transform;
		btnRT.anchorMin = new Vector2(0.5f, 0.45f); btnRT.anchorMax = new Vector2(0.5f, 0.45f);
		btnRT.pivot = new Vector2(0.5f, 0.5f); btnRT.sizeDelta = new Vector2(260, 68);
		var btnImg = btn.GetComponent<Image>(); btnImg.color = new Color(0.2f, 0.6f, 0.2f, 0.9f);
		var button = btn.GetComponent<Button>();
		button.onClick.AddListener(() => btn.GetComponent<ResetLevelButton>().OnClickReset());
		// Label
		GameObject btnLabel = new GameObject("Text", typeof(RectTransform), typeof(Text));
		btnLabel.transform.SetParent(btn.transform, false);
		var lbl = btnLabel.GetComponent<Text>();
		lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		lbl.text = "Reset Level";
		lbl.fontSize = 28; lbl.alignment = TextAnchor.MiddleCenter; lbl.color = Color.white;
		var lblRT = (RectTransform)btnLabel.transform; lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one; lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;

		// Save prefab
		string path = uiFolder + "/GameOverUI.prefab";
		PrefabUtility.SaveAsPrefabAsset(canvasGO, path);
		Object.DestroyImmediate(canvasGO);
		AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
		EditorUtility.DisplayDialog("RTS UI", "Created:\n- " + path, "OK");
	}
}


