using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Assign new URP shaders to materials in EnemyNew prefab
/// </summary>
public class AssignURPShaders : EditorWindow
{
    private GameObject enemyNewPrefab;
    private int fixedCount = 0;
    private List<string> logs = new List<string>();
    private Vector2 scrollPos;

    // Shader name mappings - cu sang moi
    private Dictionary<string, string> shaderMapping = new Dictionary<string, string>()
    {
        { "EGA/Particles/Add_CenterGlow", "EGA/Particles/Add_CenterGlow" },
        { "EGA/Particles/Add_DistortTexture", "EGA/Particles/Add_DistortTexture" },
        { "EGA/Particles/Blend_CenterGlow", "EGA/Particles/Blend_CenterGlow" },
        { "EGA/Particles/Blend_DistortTexture", "EGA/Particles/Add_DistortTexture" },
        { "EGA/Particles/Add_Trail", "EGA/Particles/Add_CenterGlow" },
        { "EGA/Particles/Blend_Electricity", "EGA/Particles/Add_CenterGlow" },
        { "EGA/Particles/Distortion", "EGA/Particles/Add_DistortTexture" },
        { "EGA/Particles/Blend_LitGlow", "EGA/Particles/Blend_CenterGlow" },
        { "EGA/Particles/Lit_CenterGlow", "EGA/Particles/Blend_CenterGlow" },
    };

    [MenuItem("DungeonMania/Assign URP Shaders")]
    public static void ShowWindow()
    {
        GetWindow<AssignURPShaders>("Assign URP Shaders");
    }

    private void OnGUI()
    {
        GUILayout.Label("Assign New URP Shaders to Materials", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        enemyNewPrefab = (GameObject)EditorGUILayout.ObjectField("EnemyNew Prefab:", enemyNewPrefab, typeof(GameObject), false);
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Script nay se thay doi shader cua tat ca material\n" +
            "trong EnemyNew prefab sang phien ban URP moi tao",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Assign URP Shaders", GUILayout.Height(40)))
        {
            AssignShaders();
        }

        if (fixedCount > 0)
        {
            EditorGUILayout.LabelField($"Da fix: {fixedCount} materials");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            foreach (var l in logs) EditorGUILayout.LabelField(l, EditorStyles.miniLabel);
            EditorGUILayout.EndScrollView();
        }
    }

    private void AssignShaders()
    {
        fixedCount = 0;
        logs.Clear();

        // Tim prefab
        if (enemyNewPrefab == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab EnemyNew", new[] { "Assets/_DungeonMania" });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                enemyNewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        if (enemyNewPrefab == null)
        {
            EditorUtility.DisplayDialog("Loi", "Khong tim thay prefab!", "OK");
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(enemyNewPrefab);
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);

        if (prefabInstance == null)
        {
            EditorUtility.DisplayDialog("Loi", "Khong load duoc prefab!", "OK");
            return;
        }

        try
        {
            // Fix all renderers
            Renderer[] renderers = prefabInstance.GetComponentsInChildren<Renderer>(true);
            
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                Material[] mats = renderer.sharedMaterials;
                if (mats == null) continue;

                bool modified = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    Material mat = mats[i];
                    if (mat == null) continue;

                    string oldShader = mat.shader != null ? mat.shader.name : "";
                    
                    // Kiem tra co trong mapping
                    if (shaderMapping.TryGetValue(oldShader, out string newShaderName))
                    {
                        Shader newShader = Shader.Find(newShaderName);
                        
                        if (newShader != null)
                        {
                            // Thay doi shader
                            mat.shader = newShader;
                            mats[i] = mat;
                            modified = true;
                            
                            logs.Add($"{GetPath(renderer.gameObject)}: {oldShader} -> {newShaderName}");
                            fixedCount++;
                        }
                        else
                        {
                            logs.Add($"ERROR: Khong tim thay shader {newShaderName}");
                        }
                    }
                }

                if (modified)
                {
                    renderer.sharedMaterials = mats;
                }
            }

            // Save
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
            
            logs.Insert(0, $"Hoan tat! Da fix {fixedCount} materials");
            Debug.Log($"[AssignURP] Da fix {fixedCount} materials");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabInstance);
        }

        AssetDatabase.Refresh();
    }

    private string GetPath(GameObject obj)
    {
        string path = obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }
        return path;
    }
}
