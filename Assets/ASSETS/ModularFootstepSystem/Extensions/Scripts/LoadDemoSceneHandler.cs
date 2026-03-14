namespace ModularFootstepSystem.Extensions
{
    using UnityEngine;
    
    /// <summary>
    /// Handles decal check on demo scene load.
    /// </summary>
    [ExecuteInEditMode]
    public class LoadDemoSceneHandler : MonoBehaviour
    {
#if UNITY_EDITOR
        private void Awake()
        {
            if (DecalEnabler.IsDecalsEnabled())
            {
                return;
            }

            if (UnityEditor.EditorUtility.DisplayDialog(
                    "Modular Footstep System",
                    "To display footprints correctly, the Decal feature must be enabled. Would you like to enable this feature for the current Render Pipeline Asset?",
                    "Yes",
                    "No"))
            {
                DecalEnabler.EnableDecals();
            }
        }
#endif
    }
}