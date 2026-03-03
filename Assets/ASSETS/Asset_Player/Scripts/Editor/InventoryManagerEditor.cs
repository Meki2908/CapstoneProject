using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Custom editor for InventoryManager to auto-populate items
/// </summary>
[CustomEditor(typeof(InventoryManager))]
public class InventoryManagerEditor : Editor
{
    private string searchPath = "Assets/ScriptableObjects/Items";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        InventoryManager manager = (InventoryManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Auto-Populate Items", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Click the buttons below to automatically find and assign all Item ScriptableObjects from the project.",
            MessageType.Info
        );

        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Auto-Find All Items (for Database)"))
        {
            FindAndAssignItems(manager, true);
        }

        if (GUILayout.Button("Auto-Find All Items (for Test Items)"))
        {
            FindAndAssignItems(manager, false);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Custom Search Path", EditorStyles.boldLabel);
        searchPath = EditorGUILayout.TextField("Search Path", searchPath);

        if (GUILayout.Button("Find Items from Custom Path"))
        {
            FindAndAssignItemsFromPath(manager, searchPath, true);
        }
    }

    /// <summary>
    /// Find all Item ScriptableObjects and assign them
    /// </summary>
    private void FindAndAssignItems(InventoryManager manager, bool assignToDatabase)
    {
        // Find all Item ScriptableObjects in the project
        string[] guids = AssetDatabase.FindAssets("t:Item");

        if (guids.Length == 0)
        {
            Debug.LogWarning("[InventoryManagerEditor] No Item ScriptableObjects found in project!");
            EditorUtility.DisplayDialog("No Items Found", "No Item ScriptableObjects were found in the project.", "OK");
            return;
        }

        Item[] items = new Item[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            items[i] = AssetDatabase.LoadAssetAtPath<Item>(path);
        }

        // Filter out null items and sort by ID
        items = items.Where(item => item != null)
                     .OrderBy(item => item.id)
                     .ToArray();

        // Assign to the appropriate field
        SerializedObject serializedObject = new SerializedObject(manager);
        SerializedProperty property = assignToDatabase 
            ? serializedObject.FindProperty("itemDatabase")
            : serializedObject.FindProperty("testItems");

        if (property != null)
        {
            property.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
            serializedObject.ApplyModifiedProperties();
        }

        string fieldName = assignToDatabase ? "itemDatabase" : "testItems";
        Debug.Log($"[InventoryManagerEditor] Found and assigned {items.Length} items to {fieldName}");
        EditorUtility.DisplayDialog("Success", $"Found and assigned {items.Length} items to {fieldName}!", "OK");
    }

    /// <summary>
    /// Find items from a specific path
    /// </summary>
    private void FindAndAssignItemsFromPath(InventoryManager manager, string path, bool assignToDatabase)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[InventoryManagerEditor] Search path is empty!");
            return;
        }

        // Find all Item ScriptableObjects in the specified path
        string[] guids = AssetDatabase.FindAssets("t:Item", new[] { path });

        if (guids.Length == 0)
        {
            Debug.LogWarning($"[InventoryManagerEditor] No Item ScriptableObjects found in path: {path}");
            EditorUtility.DisplayDialog("No Items Found", $"No Item ScriptableObjects were found in path: {path}", "OK");
            return;
        }

        Item[] items = new Item[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            items[i] = AssetDatabase.LoadAssetAtPath<Item>(assetPath);
        }

        // Filter out null items and sort by ID
        items = items.Where(item => item != null)
                     .OrderBy(item => item.id)
                     .ToArray();

        // Assign to the appropriate field
        SerializedObject serializedObject = new SerializedObject(manager);
        SerializedProperty property = assignToDatabase 
            ? serializedObject.FindProperty("itemDatabase")
            : serializedObject.FindProperty("testItems");

        if (property != null)
        {
            property.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
            serializedObject.ApplyModifiedProperties();
        }

        string fieldName = assignToDatabase ? "itemDatabase" : "testItems";
        Debug.Log($"[InventoryManagerEditor] Found and assigned {items.Length} items from {path} to {fieldName}");
        EditorUtility.DisplayDialog("Success", $"Found and assigned {items.Length} items from {path} to {fieldName}!", "OK");
    }
}

