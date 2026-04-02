using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Editor tool: "Tools → Add Borders to Refinement Slots"
/// Generates a border frame sprite and applies it to equipment slots,
/// material slot, and fusion icon slots in the Refinement panel.
/// </summary>
public class RefinementBorderSetup : Editor
{
    private const int SIZE = 128;
    private const int BORDER = 4;

    [MenuItem("Tools/Add Borders to Refinement Slots")]
    public static void Setup()
    {
        // ── Step 1: Generate border sprite ──
        Texture2D borderTex = GenerateBorderTexture(SIZE, BORDER,
            new Color(0.7f, 0.55f, 0.2f, 0.9f),  // gold border
            new Color(0.1f, 0.1f, 0.12f, 0.6f));   // dark fill

        Texture2D borderEmptyTex = GenerateBorderTexture(SIZE, BORDER,
            new Color(0.4f, 0.4f, 0.45f, 0.7f),   // gray border
            new Color(0.12f, 0.12f, 0.15f, 0.4f)); // darker fill

        string dir = "Assets/ASSETS/UI";
        if (!AssetDatabase.IsValidFolder("Assets/ASSETS"))
            AssetDatabase.CreateFolder("Assets", "ASSETS");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/ASSETS", "UI");

        SaveAndImportSprite(borderTex, $"{dir}/SlotBorder_Gold.png");
        SaveAndImportSprite(borderEmptyTex, $"{dir}/SlotBorder_Gray.png");
        DestroyImmediate(borderTex);
        DestroyImmediate(borderEmptyTex);

        // Load sprites
        Sprite goldSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{dir}/SlotBorder_Gold.png");
        Sprite graySprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{dir}/SlotBorder_Gray.png");

        if (goldSprite == null || graySprite == null)
        {
            Debug.LogError("[RefinementBorderSetup] Failed to load generated sprites!");
            return;
        }

        // ── Step 2: Find BlacksmithUI ──
        BlacksmithUI bsUI = null;
        Transform root = null;

        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
        {
            root = prefabStage.prefabContentsRoot.transform;
            bsUI = root.GetComponentInChildren<BlacksmithUI>(true);
        }
        if (bsUI == null)
        {
            bsUI = Object.FindFirstObjectByType<BlacksmithUI>(FindObjectsInactive.Include);
            if (bsUI != null) root = bsUI.transform;
        }

        if (bsUI == null)
        {
            EditorUtility.DisplayDialog("Error", "BlacksmithUI not found!\nOpen Canvas_Blacksmith in Prefab Mode.", "OK");
            return;
        }

        int count = 0;

        // ── Step 3: Apply borders to equipment slots ──
        var so = new SerializedObject(bsUI);
        var slotBtnsProp = so.FindProperty("refineEquipSlotButtons");
        if (slotBtnsProp != null)
        {
            for (int i = 0; i < slotBtnsProp.arraySize; i++)
            {
                Button btn = slotBtnsProp.GetArrayElementAtIndex(i).objectReferenceValue as Button;
                if (btn != null)
                {
                    ApplyBorder(btn.gameObject, graySprite);
                    count++;
                }
            }
        }

        // ── Step 4: Apply to star images ──
        var starsProp = so.FindProperty("refineStarImages");
        if (starsProp != null)
        {
            for (int i = 0; i < starsProp.arraySize; i++)
            {
                Image img = starsProp.GetArrayElementAtIndex(i).objectReferenceValue as Image;
                if (img != null)
                {
                    img.sprite = goldSprite;
                    EditorUtility.SetDirty(img);
                    count++;
                }
            }
        }

        // ── Step 5: Apply to material/fusion slots by name ──
        if (root != null)
        {
            string[] slotNames = { "RefineMaterial", "FusionSourceIcon", "FusionResultIcon" };
            foreach (string name in slotNames)
            {
                Transform t = FindChildRecursive(root, name);
                if (t != null)
                {
                    ApplyBorder(t.gameObject, graySprite);
                    count++;
                }
            }
        }

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        Debug.Log($"[RefinementBorderSetup] Applied borders to {count} elements!");
        EditorUtility.DisplayDialog("Success!",
            $"Border sprites created and applied!\n\n" +
            $"Sprites: {dir}/SlotBorder_Gold.png, SlotBorder_Gray.png\n" +
            $"Applied to: {count} elements\n\n" +
            "Apply Prefab Overrides to save.",
            "OK");
    }

    static void ApplyBorder(GameObject go, Sprite borderSprite)
    {
        // Add border as child Image overlay
        Transform existing = go.transform.Find("SlotBorder");
        if (existing != null) DestroyImmediate(existing.gameObject);

        GameObject borderGO = new GameObject("SlotBorder", typeof(RectTransform), typeof(Image));
        borderGO.transform.SetParent(go.transform, false);

        // Stretch to fill parent
        var rt = borderGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = borderGO.GetComponent<Image>();
        img.sprite = borderSprite;
        img.type = Image.Type.Sliced;
        img.raycastTarget = false;

        // Move to front
        borderGO.transform.SetAsLastSibling();
        EditorUtility.SetDirty(go);
    }

    static Texture2D GenerateBorderTexture(int size, int border, Color borderColor, Color fillColor)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < border || x >= size - border || y < border || y >= size - border;

                // Corner rounding (4px radius)
                int cornerDist = 4;
                bool isCorner = false;
                if (x < cornerDist && y < cornerDist)
                    isCorner = (cornerDist - x) * (cornerDist - x) + (cornerDist - y) * (cornerDist - y) > cornerDist * cornerDist;
                else if (x >= size - cornerDist && y < cornerDist)
                    isCorner = (x - (size - cornerDist - 1)) * (x - (size - cornerDist - 1)) + (cornerDist - y) * (cornerDist - y) > cornerDist * cornerDist;
                else if (x < cornerDist && y >= size - cornerDist)
                    isCorner = (cornerDist - x) * (cornerDist - x) + (y - (size - cornerDist - 1)) * (y - (size - cornerDist - 1)) > cornerDist * cornerDist;
                else if (x >= size - cornerDist && y >= size - cornerDist)
                    isCorner = (x - (size - cornerDist - 1)) * (x - (size - cornerDist - 1)) + (y - (size - cornerDist - 1)) * (y - (size - cornerDist - 1)) > cornerDist * cornerDist;

                if (isCorner)
                    tex.SetPixel(x, y, Color.clear);
                else if (isBorder)
                    tex.SetPixel(x, y, borderColor);
                else
                    tex.SetPixel(x, y, fillColor);
            }
        }

        tex.Apply();
        return tex;
    }

    static void SaveAndImportSprite(Texture2D tex, string path)
    {
        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spriteBorder = new Vector4(6, 6, 6, 6); // 9-slice borders
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
    }

    static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
