using UnityEngine;
using UnityEditor;

/// <summary>
/// Utility script to clean up missing script references
/// </summary>
public class CleanupMissingScripts : MonoBehaviour
{
    [MenuItem("Tools/Cleanup Missing Scripts")]
    public static void CleanupMissingScriptReferences()
    {
        // Find all GameObjects in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int cleanedCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            // Get all components on this object
            Component[] components = obj.GetComponents<Component>();
            
            for (int i = components.Length - 1; i >= 0; i--)
            {
                if (components[i] == null)
                {
                    // This is a missing script reference
                    Debug.Log($"Found missing script on: {obj.name}");
                    cleanedCount++;
                    
                    // Remove the missing script component
                    DestroyImmediate(components[i]);
                }
            }
        }
        
        Debug.Log($"Cleaned up {cleanedCount} missing script references");
        
        // Mark the scene as dirty so changes are saved
        EditorUtility.SetDirty(FindObjectOfType<GameObject>());
    }
}
