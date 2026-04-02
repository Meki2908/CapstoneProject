using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

/// <summary>
/// Editor tool: "Tools → Setup Refinement Sprite Asset"
/// Generates star/arrow icons, creates TMP Sprite Asset, auto-assigns.
/// </summary>
public class RefinementSpriteSetup : Editor
{
    private const int ICON_SIZE = 64;
    private const int SHEET_WIDTH = ICON_SIZE * 3;

    [MenuItem("Tools/Setup Refinement Sprite Asset")]
    public static void SetupSpriteAsset()
    {
        // ── Step 1: Generate spritesheet ──
        Texture2D sheet = new Texture2D(SHEET_WIDTH, ICON_SIZE, TextureFormat.RGBA32, false);
        Color[] clear = new Color[SHEET_WIDTH * ICON_SIZE];
        for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
        sheet.SetPixels(clear);

        DrawStar(sheet, 0, new Color(1f, 0.84f, 0f), new Color(1f, 0.65f, 0f));
        DrawStar(sheet, ICON_SIZE, new Color(0.35f, 0.35f, 0.4f), new Color(0.25f, 0.25f, 0.3f));
        DrawArrow(sheet, ICON_SIZE * 2, new Color(0.9f, 0.9f, 0.95f));
        sheet.Apply();

        string dir = "Assets/ASSETS/UI";
        if (!AssetDatabase.IsValidFolder("Assets/ASSETS"))
            AssetDatabase.CreateFolder("Assets", "ASSETS");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/ASSETS", "UI");

        string pngPath = $"{dir}/RefinementIcons.png";
        byte[] pngBytes = sheet.EncodeToPNG();
        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", pngPath));
        File.WriteAllBytes(fullPath, pngBytes);
        DestroyImmediate(sheet);
        AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);
        Debug.Log($"[RefinementSpriteSetup] Generated {pngPath} ({pngBytes.Length} bytes)");

        // ── Step 2: Slice texture ──
        TextureImporter importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
        if (importer == null) { Debug.LogError("Failed to get TextureImporter!"); return; }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.isReadable = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        string[] spriteNames = { "star", "star_empty", "arrow" };
        var spritesheet = new List<SpriteMetaData>();
        for (int i = 0; i < 3; i++)
        {
            spritesheet.Add(new SpriteMetaData
            {
                name = spriteNames[i],
                rect = new Rect(i * ICON_SIZE, 0, ICON_SIZE, ICON_SIZE),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            });
        }
        importer.spritesheet = spritesheet.ToArray();
        importer.SaveAndReimport();

        // ── Step 3: Create TMP Sprite Asset ──
        string assetPath = $"{dir}/RefinementIcons SpriteAsset.asset";

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
        if (texture == null) { Debug.LogError("Failed to load texture!"); return; }

        var existing = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(assetPath);
        if (existing != null) AssetDatabase.DeleteAsset(assetPath);

        // Load sprites
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(pngPath);
        var sprites = new Dictionary<string, Sprite>();
        foreach (var obj in allAssets)
        {
            if (obj is Sprite s)
            {
                sprites[s.name] = s;
                Debug.Log($"[RefinementSpriteSetup] Found sprite: '{s.name}' rect={s.rect}");
            }
        }

        if (sprites.Count == 0)
        {
            Debug.LogError("[RefinementSpriteSetup] No sprites found after slicing!");
            return;
        }

        // Build glyph and character tables
        var glyphs = new List<TMP_SpriteGlyph>();
        var characters = new List<TMP_SpriteCharacter>();

        for (int i = 0; i < spriteNames.Length; i++)
        {
            if (!sprites.ContainsKey(spriteNames[i]))
            {
                Debug.LogWarning($"[RefinementSpriteSetup] Missing sprite: {spriteNames[i]}");
                continue;
            }

            Sprite sp = sprites[spriteNames[i]];
            Rect r = sp.rect;

            var glyph = new TMP_SpriteGlyph();
            glyph.index = (uint)i;
            glyph.sprite = sp;
            glyph.metrics = new UnityEngine.TextCore.GlyphMetrics(
                r.width, r.height, 0, r.height * 0.75f, r.width);
            glyph.glyphRect = new UnityEngine.TextCore.GlyphRect(
                (int)r.x, (int)r.y, (int)r.width, (int)r.height);
            glyph.scale = 1.0f;
            glyphs.Add(glyph);

            var character = new TMP_SpriteCharacter(0, glyph);
            character.name = spriteNames[i];
            character.scale = 1.0f;
            characters.Add(character);
        }

        // Create asset
        TMP_SpriteAsset spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        spriteAsset.spriteSheet = texture;
        spriteAsset.name = "RefinementIcons SpriteAsset";

        // Add glyphs and characters via getter lists (setter is read-only, but getter returns mutable list)
        foreach (var g in glyphs)
            spriteAsset.spriteGlyphTable.Add(g);
        foreach (var c in characters)
            spriteAsset.spriteCharacterTable.Add(c);

        Debug.Log($"[RefinementSpriteSetup] Tables: {spriteAsset.spriteGlyphTable.Count} glyphs, {spriteAsset.spriteCharacterTable.Count} chars");

        AssetDatabase.CreateAsset(spriteAsset, assetPath);

        // Update lookup tables
        spriteAsset.UpdateLookupTables();
        EditorUtility.SetDirty(spriteAsset);
        AssetDatabase.SaveAssets();

        Debug.Log($"[RefinementSpriteSetup] Created: {assetPath} ({glyphs.Count} glyphs, {characters.Count} chars)");

        // ── Step 4: Set as default or fallback ──
        TMP_Settings settings = TMP_Settings.instance;
        if (settings != null)
        {
            var settingsSO = new SerializedObject(settings);
            var defaultProp = settingsSO.FindProperty("m_defaultSpriteAsset");
            if (defaultProp != null)
            {
                if (defaultProp.objectReferenceValue == null)
                {
                    defaultProp.objectReferenceValue = spriteAsset;
                    settingsSO.ApplyModifiedProperties();
                    Debug.Log("[RefinementSpriteSetup] Set as default TMP Sprite Asset");
                }
                else
                {
                    var defaultAsset = defaultProp.objectReferenceValue as TMP_SpriteAsset;
                    if (defaultAsset != null && defaultAsset != spriteAsset)
                    {
                        if (defaultAsset.fallbackSpriteAssets == null)
                            defaultAsset.fallbackSpriteAssets = new List<TMP_SpriteAsset>();
                        if (!defaultAsset.fallbackSpriteAssets.Contains(spriteAsset))
                        {
                            defaultAsset.fallbackSpriteAssets.Add(spriteAsset);
                            EditorUtility.SetDirty(defaultAsset);
                            Debug.Log("[RefinementSpriteSetup] Added as fallback sprite asset");
                        }
                    }
                }
            }
        }

        // ── Step 5: Assign to BlacksmithUI ──
        BlacksmithUI bsUI = null;
        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
            bsUI = prefabStage.prefabContentsRoot.GetComponentInChildren<BlacksmithUI>(true);
        if (bsUI == null)
            bsUI = Object.FindFirstObjectByType<BlacksmithUI>(FindObjectsInactive.Include);

        if (bsUI != null)
        {
            var so = new SerializedObject(bsUI);
            string[] fields = { "refineLevelText", "refineStatsText", "refineSuccessText",
                                "fusionInfoText", "refineEquipNameText", "resultText" };
            foreach (var f in fields)
            {
                var prop = so.FindProperty(f);
                if (prop != null && prop.objectReferenceValue is TMP_Text tmp)
                {
                    tmp.spriteAsset = spriteAsset;
                    EditorUtility.SetDirty(tmp);
                    Debug.Log($"[RefinementSpriteSetup] Assigned to: {f}");
                }
            }
            so.ApplyModifiedProperties();
        }

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Success!",
            $"Sprite Asset: {assetPath}\n" +
            $"Sprites: {glyphs.Count}/3 (star, star_empty, arrow)\n\n" +
            (bsUI != null ? "Auto-assigned to TMP components!" : "Open Canvas_Blacksmith to auto-assign."),
            "OK");
    }

    // ════════════════════════════════════════════════════════════
    // DRAWING
    // ════════════════════════════════════════════════════════════

    static void DrawStar(Texture2D tex, int offsetX, Color fill, Color edge)
    {
        int cx = offsetX + ICON_SIZE / 2;
        int cy = ICON_SIZE / 2;
        float outerR = ICON_SIZE * 0.42f;
        float innerR = ICON_SIZE * 0.18f;

        Vector2[] verts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float angle = -Mathf.PI / 2f + i * Mathf.PI / 5f;
            float r = (i % 2 == 0) ? outerR : innerR;
            verts[i] = new Vector2(cx + r * Mathf.Cos(angle), cy + r * Mathf.Sin(angle));
        }

        for (int y = 0; y < ICON_SIZE; y++)
            for (int x = offsetX; x < offsetX + ICON_SIZE; x++)
                if (PointInPolygon(new Vector2(x, y), verts))
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float t = Mathf.Clamp01((d - innerR) / (outerR - innerR));
                    tex.SetPixel(x, y, Color.Lerp(fill, edge, t * 0.5f));
                }
    }

    static void DrawArrow(Texture2D tex, int offsetX, Color color)
    {
        int cx = offsetX + ICON_SIZE / 2;
        int cy = ICON_SIZE / 2;
        DrawChevron(tex, cx - 10, cy, 14, color);
        DrawChevron(tex, cx + 6, cy, 14, color);
    }

    static void DrawChevron(Texture2D tex, int cx, int cy, int size, Color color)
    {
        for (int i = -size; i <= size; i++)
        {
            int x = cx + Mathf.Abs(i) / 2;
            int y = cy + i;
            if (y < 0 || y >= ICON_SIZE) continue;
            for (int t = 0; t < 4; t++)
            {
                int px = x + t;
                if (px >= 0 && px < SHEET_WIDTH)
                    tex.SetPixel(px, y, color);
            }
        }
    }

    static FieldInfo FindField(System.Type type, string name)
    {
        while (type != null)
        {
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field != null) return field;
            type = type.BaseType;
        }
        return null;
    }

    static List<FieldInfo> GetAllFields(System.Type type)
    {
        var fields = new List<FieldInfo>();
        while (type != null)
        {
            fields.AddRange(type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance));
            type = type.BaseType;
        }
        return fields;
    }

    static bool PointInPolygon(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        int j = poly.Length - 1;
        for (int i = 0; i < poly.Length; j = i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
        }
        return inside;
    }
}
