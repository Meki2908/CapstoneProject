using UnityEngine;

/// <summary>
/// Removes duplicate AudioListeners from the scene to fix the "There are 2 audio listeners" warning.
/// Add this script to any GameObject in the scene (e.g., on the Main Camera).
/// </summary>
public class AudioListenerFixer : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("If true, will disable all AudioListeners except the first one found")]
    public bool fixDuplicateListeners = true;

    private void Awake()
    {
        if (fixDuplicateListeners)
        {
            FixDuplicateAudioListeners();
        }
    }

    private void FixDuplicateAudioListeners()
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        
        if (listeners.Length > 1)
        {
            Debug.LogWarning($"[AudioListenerFixer] Found {listeners.Length} AudioListeners in scene. Disabling duplicates.");
            
            // Keep only the first one enabled
            for (int i = 1; i < listeners.Length; i++)
            {
                listeners[i].enabled = false;
                Debug.Log($"[AudioListenerFixer] Disabled AudioListener on: {listeners[i].gameObject.name}");
            }
        }
    }
}
