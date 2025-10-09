using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// StoryPoint creates narrative moments with dialogue, camera control, spawning, and objectives
/// </summary>
public class StoryPoint : MonoBehaviour
{
    [System.Serializable]
    public class SpawnGroup
    {
        public string groupName = "Spawn Group";
        public GameObject unitPrefab;
        public int count = 1;
        public string unitTag = "Enemy"; // "Enemy" or "Friendly"
        
        [Header("Spawn Location")]
        public bool useSpecificPoints = false;
        public Transform[] spawnPoints; // Specific spawn locations
        public Transform spawnCenter; // If not using specific points, spawn around this
        public float spawnRadius = 5f; // Radius for random spawning
        public float spawnSpread = 1f; // Random offset for each unit
    }
    
    [System.Serializable]
    public class SpawnScenario
    {
        public string scenarioName = "Scenario";
        public List<SpawnGroup> spawnGroups = new List<SpawnGroup>();
    }
    
    [Header("Trigger Settings")]
    public bool triggerOnce = true;
    public float triggerRadius = 5f;
    public string triggerTag = "Allied"; // What can trigger this story point
    private bool hasTriggered = false;
    
    [Header("Camera Control")]
    public bool focusCamera = false;
    public Transform cameraFocusPoint;
    public Camera targetCamera; // Manual camera assignment
    public float cameraTransitionDuration = 1f;
    public float cameraFocusDuration = 3f;
    
    [Header("Screen Effects")]
    public bool useFadeIn = false;
    public bool useFadeOut = false;
    public float fadeDuration = 1f;
    public Color fadeColor = Color.black;
    
    [Header("Dialogue")]
    public bool showDialogue = false;
    public string[] dialogueLines;
    public float dialogueDisplayTime = 3f;
    
    [Header("Unit Spawning")]
    public List<SpawnScenario> spawnScenarios = new List<SpawnScenario>();
    
    [Header("Objectives")]
    public bool trackEnemies = false; // Track when all spawned enemies are defeated
    public bool convertFriendliesToAllied = false; // Convert friendly units to allied when objective complete
    public GameObject alliedUnitPrefab; // Prefab to replace friendly units with (if null, modifies in place)
    
    [Header("Events")]
    public UnityEvent onTrigger;
    public UnityEvent onDialogueComplete;
    public UnityEvent onAllEnemiesDefeated;
    
    [Header("Debug")]
    public bool showStoryPointDebug = false;
    
    private StoryPointUI storyUI;
    private bool isPlayingSequence = false;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private List<GameObject> spawnedFriendlies = new List<GameObject>();
    private bool objectiveCompleted = false;
    
    void Start()
    {
        // Find or create UI
        storyUI = FindFirstObjectByType<StoryPointUI>();
        if (storyUI == null && (showDialogue || useFadeIn || useFadeOut))
        {
            GameObject uiObj = new GameObject("StoryPointUI");
            storyUI = uiObj.AddComponent<StoryPointUI>();
        }
        
        // Get camera if not assigned
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }
    
    void Update()
    {
        // Check objective completion first (always run if tracking)
        if (trackEnemies && !objectiveCompleted && hasTriggered)
        {
            CheckObjectiveCompletion();
        }
        
        // Don't check for triggers if already triggered and set to trigger once
        if (hasTriggered && triggerOnce) return;
        if (isPlayingSequence) return;
        
        // Check for trigger
        Collider[] colliders = Physics.OverlapSphere(transform.position, triggerRadius);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag(triggerTag))
            {
                TriggerStoryPoint();
                break;
            }
        }
    }
    
    void TriggerStoryPoint()
    {
        if (showStoryPointDebug)
        {
            Debug.Log($"[StoryPoint] {gameObject.name} triggered!");
        }
        
        hasTriggered = true;
        onTrigger?.Invoke();
        
        StartCoroutine(PlayStorySequence());
    }
    
    IEnumerator PlayStorySequence()
    {
        isPlayingSequence = true;
        
        if (showStoryPointDebug)
        {
            Debug.Log($"[StoryPoint] Starting story sequence for {gameObject.name}");
        }
        
        // Fade out
        if (useFadeOut && storyUI != null)
        {
            if (showStoryPointDebug) Debug.Log("[StoryPoint] Fading out...");
            yield return StartCoroutine(storyUI.FadeOut(fadeDuration, fadeColor));
        }
        
        // Camera focus
        if (focusCamera && cameraFocusPoint != null && targetCamera != null)
        {
            if (showStoryPointDebug) Debug.Log($"[StoryPoint] Focusing camera on {cameraFocusPoint.name}");
            
            // Temporarily disable camera following
            CameraFollow camFollow = targetCamera.GetComponent<CameraFollow>();
            Transform originalTarget = null;
            if (camFollow != null)
            {
                originalTarget = camFollow.target;
                camFollow.target = null; // Stop following
            }
            
            Vector3 startPos = targetCamera.transform.position;
            Vector3 targetPos = cameraFocusPoint.position;
            
            float elapsed = 0f;
            while (elapsed < cameraTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / cameraTransitionDuration;
                targetCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
            
            yield return new WaitForSeconds(cameraFocusDuration);
            
            // Re-enable camera following
            if (camFollow != null && originalTarget != null)
            {
                camFollow.target = originalTarget;
            }
        }
        
        // Show dialogue
        if (showDialogue && dialogueLines != null && dialogueLines.Length > 0 && storyUI != null)
        {
            if (showStoryPointDebug) Debug.Log($"[StoryPoint] Showing {dialogueLines.Length} dialogue lines");
            yield return StartCoroutine(storyUI.ShowDialogue(dialogueLines, dialogueDisplayTime));
        }
        
        // Spawn units
        if (spawnScenarios.Count > 0)
        {
            if (showStoryPointDebug) Debug.Log($"[StoryPoint] Spawning {spawnScenarios.Count} scenarios");
            SpawnUnits();
        }
        
        // Fade in
        if (useFadeIn && storyUI != null)
        {
            if (showStoryPointDebug) Debug.Log("[StoryPoint] Fading in...");
            yield return StartCoroutine(storyUI.FadeIn(fadeDuration));
        }
        
        onDialogueComplete?.Invoke();
        isPlayingSequence = false;
        
        if (showStoryPointDebug)
        {
            Debug.Log($"[StoryPoint] Story sequence complete for {gameObject.name}");
        }
    }
    
    void SpawnUnits()
    {
        foreach (SpawnScenario scenario in spawnScenarios)
        {
            if (showStoryPointDebug)
            {
                Debug.Log($"[StoryPoint] Spawning scenario: {scenario.scenarioName}");
            }
            
            foreach (SpawnGroup group in scenario.spawnGroups)
            {
                SpawnUnitsInGroup(group);
            }
        }
    }
    
    void SpawnUnitsInGroup(SpawnGroup group)
    {
        if (group.unitPrefab == null)
        {
            Debug.LogWarning($"[StoryPoint] No unit prefab assigned for group {group.groupName}");
            return;
        }
        
        if (showStoryPointDebug)
        {
            Debug.Log($"[StoryPoint] Spawning group: {group.groupName} ({group.count} units)");
        }
        
        for (int i = 0; i < group.count; i++)
        {
            Vector3 spawnPos;
            
            // Determine spawn position
            if (group.useSpecificPoints && group.spawnPoints != null && group.spawnPoints.Length > 0)
            {
                // Use specific spawn points (cycle through them)
                Transform spawnPoint = group.spawnPoints[i % group.spawnPoints.Length];
                spawnPos = spawnPoint.position;
            }
            else if (group.spawnCenter != null)
            {
                // Random position around spawn center
                Vector2 randomCircle = Random.insideUnitCircle * group.spawnRadius;
                spawnPos = group.spawnCenter.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
            }
            else
            {
                // Use this StoryPoint's position
                Vector2 randomCircle = Random.insideUnitCircle * group.spawnRadius;
                spawnPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
            }
            
            // Add spread
            Vector2 spread = Random.insideUnitCircle * group.spawnSpread;
            spawnPos += new Vector3(spread.x, 0f, spread.y);
            
            // Spawn the unit
            GameObject unit = Instantiate(group.unitPrefab, spawnPos, Quaternion.identity);
            unit.tag = group.unitTag;
            
            // Track spawned units for objectives
            if (group.unitTag == "Enemy")
            {
                spawnedEnemies.Add(unit);
                if (showStoryPointDebug)
                {
                    Debug.Log($"[StoryPoint] Spawned enemy at {spawnPos}, tracking for objectives");
                }
            }
            else if (group.unitTag == "Friendly")
            {
                spawnedFriendlies.Add(unit);
                if (showStoryPointDebug)
                {
                    Debug.Log($"[StoryPoint] Spawned friendly at {spawnPos}");
                }
            }
        }
    }
    
    void CheckObjectiveCompletion()
    {
        if (objectiveCompleted) return;
        
        // Remove null (destroyed) enemies from list
        spawnedEnemies.RemoveAll(enemy => enemy == null);
        
        if (showStoryPointDebug && spawnedEnemies.Count > 0)
        {
            Debug.Log($"[StoryPoint] Enemies remaining: {spawnedEnemies.Count}");
        }
        
        // Check if all enemies are defeated
        if (spawnedEnemies.Count == 0 && hasTriggered)
        {
            OnObjectiveCompleted();
        }
    }
    
    void OnObjectiveCompleted()
    {
        if (objectiveCompleted) return;
        
        objectiveCompleted = true;
        
        if (showStoryPointDebug)
        {
            Debug.Log($"[StoryPoint] ✅ All enemies defeated! Objective complete!");
        }
        
        onAllEnemiesDefeated?.Invoke();
        
        // Convert friendlies to allied
        if (convertFriendliesToAllied)
        {
            ConvertFriendliesToAllied();
        }
    }
    
    void ConvertFriendliesToAllied()
    {
        if (showStoryPointDebug)
        {
            Debug.Log($"[StoryPoint] Converting {spawnedFriendlies.Count} friendly units to allied...");
        }
        
        int convertedCount = 0;
        
        // Check if we should replace with prefab or modify in place
        if (alliedUnitPrefab != null)
        {
            // PREFAB REPLACEMENT METHOD (cleaner)
            foreach (GameObject friendly in spawnedFriendlies)
            {
                if (friendly == null) continue;
                
                // Store position and rotation
                Vector3 position = friendly.transform.position;
                Quaternion rotation = friendly.transform.rotation;
                
                // Instantiate allied unit at same position
                GameObject newAllied = Instantiate(alliedUnitPrefab, position, rotation);
                newAllied.tag = "Allied"; // Ensure tag is set
                
                // Destroy old friendly
                Destroy(friendly);
                convertedCount++;
                
                if (showStoryPointDebug)
                {
                    Debug.Log($"[StoryPoint] ✓ Replaced friendly with allied unit prefab at {position}");
                }
            }
        }
        else
        {
            // IN-PLACE MODIFICATION METHOD (fallback if no prefab assigned)
            foreach (GameObject friendly in spawnedFriendlies)
            {
                if (friendly == null) continue;
                
                // Change tag
                friendly.tag = "Allied";
                
                // Update health bar color
                BaseCharacter baseChar = friendly.GetComponent<BaseCharacter>();
                if (baseChar != null && baseChar.healthBarFill != null)
                {
                    baseChar.healthBarFill.color = Color.blue;
                }
                
                // Disable friendly AI
                FriendlyCharacter friendlyChar = friendly.GetComponent<FriendlyCharacter>();
                if (friendlyChar != null)
                {
                    friendlyChar.enabled = false;
                }
                
                // Add player selection
                PlayerSelectionController psc = friendly.GetComponent<PlayerSelectionController>();
                if (psc == null)
                {
                    psc = friendly.AddComponent<PlayerSelectionController>();
                }
                psc.selectionColor = Color.blue;
                
                // Add RTS movement
                RTSMovementController rts = friendly.GetComponent<RTSMovementController>();
                if (rts == null)
                {
                    rts = friendly.AddComponent<RTSMovementController>();
                }
                rts.moveSpeed = 15f;
                rts.showDebugInfo = false;
                
                // Add/enable fireball launcher
                FireballLauncher launcher = friendly.GetComponent<FireballLauncher>();
                if (launcher == null)
                {
                    launcher = friendly.AddComponent<FireballLauncher>();
                }
                
                // Copy settings from FriendlyCharacter if available
                if (friendlyChar != null)
                {
                    launcher.fireballPrefab = friendlyChar.fireballPrefab;
                    launcher.fireballSpawnPoint = friendlyChar.fireballSpawnPoint;
                    launcher.fireballSpeed = friendlyChar.fireballSpeed;
                    launcher.fireballSize = friendlyChar.fireballSize;
                    launcher.fireRate = friendlyChar.fireRate;
                }
                
                launcher.enableDebugLog = false;
                launcher.enabled = true;
                
                convertedCount++;
                
                if (showStoryPointDebug)
                {
                    Debug.Log($"[StoryPoint] ✓ Converted {friendly.name} to allied unit (in-place)");
                }
            }
        }
        
        Debug.Log($"[StoryPoint] Successfully converted {convertedCount} friendly units to allied!");
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw trigger radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        
        // Draw camera focus point
        if (focusCamera && cameraFocusPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, cameraFocusPoint.position);
            Gizmos.DrawWireSphere(cameraFocusPoint.position, 1f);
        }
        
        // Draw spawn points
        foreach (SpawnScenario scenario in spawnScenarios)
        {
            foreach (SpawnGroup group in scenario.spawnGroups)
            {
                if (group.useSpecificPoints && group.spawnPoints != null)
                {
                    Gizmos.color = group.unitTag == "Enemy" ? Color.red : Color.green;
                    foreach (Transform spawnPoint in group.spawnPoints)
                    {
                        if (spawnPoint != null)
                        {
                            Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                        }
                    }
                }
                else if (group.spawnCenter != null)
                {
                    Gizmos.color = group.unitTag == "Enemy" ? new Color(1f, 0.5f, 0.5f, 0.3f) : new Color(0.5f, 1f, 0.5f, 0.3f);
                    Gizmos.DrawWireSphere(group.spawnCenter.position, group.spawnRadius);
                }
            }
        }
    }
}

