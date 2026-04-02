using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Editor tool: "Tools → Add Star Row to Refinement Panel"
/// Safely adds 7 star Image objects into the existing RefinementTabPanel
/// WITHOUT destroying or modifying any existing elements.
/// </summary>
public class RefinementStarInjector : Editor
{
    [MenuItem("Tools/Add Star Row to Refinement Panel")]
    public static void AddStarRow()
    {
        // Find BlacksmithUI (Prefab Mode or Scene)
        BlacksmithUI blacksmithUI = null;
        Transform root = null;

        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            root = prefabStage.prefabContentsRoot.transform;
            blacksmithUI = root.GetComponent<BlacksmithUI>();
            if (blacksmithUI == null)
                blacksmithUI = root.GetComponentInChildren<BlacksmithUI>(true);
        }
        if (blacksmithUI == null)
        {
            blacksmithUI = Object.FindFirstObjectByType<BlacksmithUI>(FindObjectsInactive.Include);
            if (blacksmithUI != null) root = blacksmithUI.transform;
        }

        if (blacksmithUI == null || root == null)
        {
            EditorUtility.DisplayDialog("Error", "BlacksmithUI not found!\nOpen Canvas_Blacksmith in Prefab Mode first.", "OK");
            return;
        }

        // Find RefinementTabPanel
        Transform refinementPanel = FindChild(root, "RefinementTabPanel");
        if (refinementPanel == null)
        {
            EditorUtility.DisplayDialog("Error", "RefinementTabPanel not found!\nRun 'Add Refinement Tab' first.", "OK");
            return;
        }

        // Check if StarRow already exists
        Transform existingStarRow = refinementPanel.Find("RefineStarRow");
        if (existingStarRow != null)
        {
            if (!EditorUtility.DisplayDialog("Already Exists",
                "RefineStarRow already exists. Delete and recreate?", "Yes", "Cancel"))
                return;
            DestroyImmediate(existingStarRow.gameObject);
        }

        // Find RefineLevelText to insert StarRow after it
        Transform levelText = refinementPanel.Find("RefineLevelText");
        int insertIndex = -1;
        if (levelText != null)
            insertIndex = levelText.GetSiblingIndex() + 1;

        // ── Create StarRow ──
        GameObject starRow = new GameObject("RefineStarRow", typeof(RectTransform));
        starRow.transform.SetParent(refinementPanel, false);

        // Position after RefineLevelText
        if (insertIndex >= 0)
            starRow.transform.SetSiblingIndex(insertIndex);

        // Layout
        var hlg = starRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        var rowLE = starRow.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 50;

        // ── Create 7 Star Images ──
        Image[] starImages = new Image[7];
        Color gray = new Color(0.3f, 0.3f, 0.35f);

        // Try to find star sprite from generated spritesheet
        Sprite starSprite = null;
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/ASSETS/UI/RefinementIcons.png");
        if (allAssets != null)
        {
            foreach (var obj in allAssets)
            {
                if (obj is Sprite s && s.name == "star")
                {
                    starSprite = s;
                    break;
                }
            }
        }

        for (int i = 0; i < 7; i++)
        {
            GameObject starGO = new GameObject($"Star_{i}", typeof(RectTransform), typeof(Image));
            starGO.transform.SetParent(starRow.transform, false);

            var le = starGO.AddComponent<LayoutElement>();
            le.preferredWidth = 40;
            le.preferredHeight = 40;

            starImages[i] = starGO.GetComponent<Image>();
            starImages[i].color = gray;

            if (starSprite != null)
                starImages[i].sprite = starSprite;
        }

        // ── Wire to BlacksmithUI ──
        var so = new SerializedObject(blacksmithUI);
        var starsProp = so.FindProperty("refineStarImages");
        if (starsProp != null)
        {
            starsProp.arraySize = 7;
            for (int i = 0; i < 7; i++)
                starsProp.GetArrayElementAtIndex(i).objectReferenceValue = starImages[i];
            so.ApplyModifiedProperties();
            Debug.Log("[RefinementStarInjector] Wired 7 star images to BlacksmithUI.refineStarImages");
        }
        else
        {
            Debug.LogWarning("[RefinementStarInjector] refineStarImages field not found! Assign manually in Inspector.");
        }

        EditorUtility.SetDirty(blacksmithUI);
        EditorUtility.SetDirty(blacksmithUI.gameObject);

        Debug.Log("[RefinementStarInjector] Added RefineStarRow with 7 star images!");
        EditorUtility.DisplayDialog("Success!",
            "RefineStarRow added!\n\n" +
            "7 star Images created (Star_0 ~ Star_6)\n" +
            (starSprite != null ? "Star sprite auto-assigned." : "Assign a star sprite to each Image in Inspector.") +
            "\n\nApply Prefab Overrides to save.",
            "OK");
    }

    static Transform FindChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
