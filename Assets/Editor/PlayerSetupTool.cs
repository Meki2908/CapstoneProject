using UnityEngine;
using UnityEditor;
using Unity.FantasyKingdom; // For QualitySettingsController
using UnityEngine.UI;

public class PlayerSetupTool : EditorWindow
{
    private GameObject playerRoot;

    [MenuItem("Tools/Player Setup Tool")]
    public static void ShowWindow()
    {
        GetWindow<PlayerSetupTool>("Player Setup Tool");
    }

    void OnGUI()
    {
        GUILayout.Label("Player Prefab Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        playerRoot = (GameObject)EditorGUILayout.ObjectField("Player Root (Prefab/Instance)", playerRoot, typeof(GameObject), true);

        if (playerRoot == null)
        {
            GUILayout.Label("Please drag the 'PlayerRoot+UI' object here.", EditorStyles.helpBox);
            return;
        }

        GUILayout.Space(20);

        if (GUILayout.Button("Setup All References"))
        {
            SetupReferences();
        }

        GUILayout.Space(10);
        
        if (GUILayout.Button("Check Input System Settings"))
        {
            FixInputSystem();
        }
    }

    void SetupReferences()
    {
        if (playerRoot == null) return;

        int fixCount = 0;

        // 1. Setup Minimap Camera
        MinimapCameraFollow minimapCam = playerRoot.GetComponentInChildren<MinimapCameraFollow>(true);
        if (minimapCam != null)
        {
            // Find "Player" child or tag
            Transform playerT = FindChildRecursively(playerRoot.transform, "Player");
            if (playerT != null)
            {
                Undo.RecordObject(minimapCam, "Setup Minimap Player");
                minimapCam.player = playerT;
                Debug.Log($"Failed to find 'Player' child. Assigned {playerT.name} to MinimapCameraFollow.");
                fixCount++;
            }
            else
            {
                Debug.LogWarning("Could not find a child named 'Player' inside the root.");
            }
        }
        else
        {
            // Try to find in scene if not in prefab hierarchy (though user said prefab)
            minimapCam = FindFirstObjectByType<MinimapCameraFollow>();
            if (minimapCam != null)
            {
                 Transform playerT = FindChildRecursively(playerRoot.transform, "Player");
                 if (playerT != null)
                 {
                    Undo.RecordObject(minimapCam, "Setup Minimap Player");
                    minimapCam.player = playerT;
                    fixCount++;
                 }
            }
        }

        // 2. Setup Equipment/Gem Slot Icons
        var equipmentSlots = playerRoot.GetComponentsInChildren<EquipmentSlotDropZone>(true);
        foreach (var slot in equipmentSlots)
        {
            if (slot.slotImage == null)
            {
                // Logic: Find an Image component on this object or a child named "Icon"
                Image img = slot.GetComponent<Image>();
                if (img == null)
                {
                    Transform iconTr = FindChildRecursively(slot.transform, "Icon");
                    if (iconTr != null) img = iconTr.GetComponent<Image>();
                }
                
                if (img != null)
                {
                    Undo.RecordObject(slot, "Assign Slot Image");
                    // Directly assign the image
                    slot.slotImage = img;
                    fixCount++;
                    Debug.Log($"Assigned image to Equipment Slot: {slot.name}");
                }
            }
        }

        var gemSlots = playerRoot.GetComponentsInChildren<GemSlotDropZone>(true);
        foreach (var slot in gemSlots)
        {
             // Similar logic for GemSlotDropZone
             // Using serializedobject to be safe if field access fails
            SerializedObject so = new SerializedObject(slot);
            SerializedProperty sp = so.FindProperty("slotImage");
            if (sp != null && sp.objectReferenceValue == null)
            {
                 Image img = slot.GetComponent<Image>();
                 if (img == null)
                 {
                    Transform iconTr = FindChildRecursively(slot.transform, "Icon");
                    if (iconTr != null) img = iconTr.GetComponent<Image>();
                 }

                 if (img != null)
                 {
                     sp.objectReferenceValue = img;
                     so.ApplyModifiedProperties();
                     fixCount++;
                     Debug.Log($"Assigned image to Gem Slot: {slot.name}");
                 }
            }
        }
        
        // 3. Setup QualitySettingsController
        QualitySettingsController qualityCtrl = playerRoot.GetComponentInChildren<QualitySettingsController>(true);
        if (qualityCtrl != null)
        {
            SerializedObject so = new SerializedObject(qualityCtrl);
            SerializedProperty sp = so.FindProperty("QualitySettingsButton");
            if (sp != null && sp.objectReferenceValue == null)
            {
                // Find a GameObject named "QualitySettingsButton" or similar
                Transform btn = FindChildRecursively(playerRoot.transform, "QualitySettingsButton");
                if (btn == null) btn = FindChildRecursively(playerRoot.transform, "Btn_Quality");

                if (btn != null)
                {
                     sp.objectReferenceValue = btn.gameObject;
                     so.ApplyModifiedProperties();
                     fixCount++;
                     Debug.Log("Assigned QualitySettingsButton");
                }
            }
        }

        Debug.Log($"Setup completed! Fixed {fixCount} references.");
        EditorUtility.DisplayDialog("Setup Complete", $"Fixed {fixCount} references.\n\nDon't forget to check Input System settings if you haven't.", "OK");
    }

    void FixInputSystem()
    {
        // Open Project Settings to Input
        SettingsService.OpenProjectSettings("Project/Player");
        EditorUtility.DisplayDialog("Input System", 
            "Please explicitly check:\n\nPlayer > Other Settings > Active Input Handling\n\nSet it to 'Both' or 'Input System Package (New)'.\n\nThe tool cannot change this setting automatically as it requires a restart.", "OK");
    }

    Transform FindChildRecursively(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindChildRecursively(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
