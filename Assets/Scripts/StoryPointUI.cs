using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// StoryPointUI handles UI elements for StoryPoint narrative events
/// </summary>
public class StoryPointUI : MonoBehaviour
{
    [Header("UI References")]
    public Canvas canvas;
    public Image fadeImage;
    public TextMeshProUGUI dialogueText;
    public Image dialogueBackground;
    
    [Header("Dialogue Settings")]
    public Color dialogueBackgroundColor = new Color(0f, 0f, 0f, 0.8f);
    public Color dialogueTextColor = Color.white;
    public int dialogueFontSize = 24;
    
    private bool isInitialized = false;
    
    void Awake()
    {
        if (!isInitialized)
        {
            CreateUI();
        }
    }
    
    void CreateUI()
    {
        // Create canvas if needed
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("StoryPointCanvas");
            canvasObj.transform.SetParent(transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create fade image
        if (fadeImage == null)
        {
            GameObject fadeObj = new GameObject("FadeImage");
            fadeObj.transform.SetParent(canvas.transform, false);
            fadeImage = fadeObj.AddComponent<Image>();
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
            
            RectTransform fadeRect = fadeImage.GetComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.sizeDelta = Vector2.zero;
            
            fadeImage.raycastTarget = false;
        }
        
        // Create dialogue background
        if (dialogueBackground == null)
        {
            GameObject bgObj = new GameObject("DialogueBackground");
            bgObj.transform.SetParent(canvas.transform, false);
            dialogueBackground = bgObj.AddComponent<Image>();
            dialogueBackground.color = dialogueBackgroundColor;
            
            RectTransform bgRect = dialogueBackground.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.1f, 0.1f);
            bgRect.anchorMax = new Vector2(0.9f, 0.3f);
            bgRect.sizeDelta = Vector2.zero;
            
            dialogueBackground.gameObject.SetActive(false);
        }
        
        // Create dialogue text
        if (dialogueText == null)
        {
            GameObject textObj = new GameObject("DialogueText");
            textObj.transform.SetParent(dialogueBackground.transform, false);
            dialogueText = textObj.AddComponent<TextMeshProUGUI>();
            dialogueText.color = dialogueTextColor;
            dialogueText.fontSize = dialogueFontSize;
            dialogueText.alignment = TextAlignmentOptions.Center;
            dialogueText.textWrappingMode = TextWrappingModes.Normal;
            
            RectTransform textRect = dialogueText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.05f);
            textRect.anchorMax = new Vector2(0.95f, 0.95f);
            textRect.sizeDelta = Vector2.zero;
        }
        
        isInitialized = true;
    }
    
    public IEnumerator FadeOut(float duration, Color targetColor)
    {
        if (fadeImage == null) yield break;
        
        float elapsed = 0f;
        Color startColor = fadeImage.color;
        targetColor.a = 1f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        fadeImage.color = targetColor;
    }
    
    public IEnumerator FadeIn(float duration)
    {
        if (fadeImage == null) yield break;
        
        float elapsed = 0f;
        Color startColor = fadeImage.color;
        Color targetColor = startColor;
        targetColor.a = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        fadeImage.color = targetColor;
    }
    
    public IEnumerator ShowDialogue(string[] lines, float displayTime)
    {
        if (dialogueBackground == null || dialogueText == null) yield break;
        
        dialogueBackground.gameObject.SetActive(true);
        
        foreach (string line in lines)
        {
            dialogueText.text = line;
            yield return new WaitForSeconds(displayTime);
        }
        
        dialogueBackground.gameObject.SetActive(false);
        dialogueText.text = "";
    }
}

