using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

public class PlayerUnlitWithShadowsTool : EditorWindow
{
	private GameObject targetRoot;
	private Shader unlitShadowShader;
	private Material unlitShadowMaterial;
	private string shaderPath = "Assets/Shaders/UnlitWithShadows.shader";
	private string materialPath = "Assets/Materials/PlayerUnlitWithShadows.mat";
	private float shadowDarkness = 0.5f;

	[MenuItem("Tools/Player Unlit With Shadows")]
	public static void ShowWindow()
	{
		GetWindow<PlayerUnlitWithShadowsTool>("Player Unlit With Shadows");
	}

	private void OnEnable()
	{
		unlitShadowShader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
		unlitShadowMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
	}

	private void OnGUI()
	{
		GUILayout.Label("Apply Unlit (but keep shadows) to a Player hierarchy", EditorStyles.boldLabel);

		targetRoot = (GameObject)EditorGUILayout.ObjectField("Player Root", targetRoot, typeof(GameObject), true);

		EditorGUILayout.Space();

		if (unlitShadowShader == null) {
			EditorGUILayout.HelpBox("Shader not found at: " + shaderPath + "\nYou can create it with the button below.", MessageType.Info);
		} else {
			EditorGUILayout.ObjectField("Shader", unlitShadowShader, typeof(Shader), false);
		}

		shadowDarkness = EditorGUILayout.Slider("Shadow Darkness", shadowDarkness, 0f, 1f);

		EditorGUILayout.Space();

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Create Shader File")) {
			CreateShaderFile();
		}
		if (GUILayout.Button("Create Material")) {
			CreateMaterial();
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();

		if (GUILayout.Button("Apply to Selected Player (set material + disable probes)")) {
			ApplyToTarget();
		}

		if (GUILayout.Button("Revert materials to original (not implemented)")) {
			EditorUtility.DisplayDialog("Revert", "Revert not implemented. Please backup before running.", "OK");
		}
	}

	private void CreateShaderFile()
	{
		if (System.IO.File.Exists(shaderPath)) {
			if (!EditorUtility.DisplayDialog("Shader exists", "Shader file already exists. Overwrite?", "Yes", "No")) return;
		}

		string content = @"Shader ""Custom/UnlitWithShadows""
{
    Properties {
        _MainTex (""Texture"", 2D) = ""white"" {}
        _Color (""Color"", Color) = (1,1,1,1)
        _ShadowDarkness (""Shadow Darkness"", Range(0,1)) = 0.5
    }
    SubShader {
        Tags { ""RenderType""=""Opaque"" }
        LOD 200

        CGPROGRAM
        #pragma surface surf UnlitWithShadows addshadow fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _ShadowDarkness;

        struct Input {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutput o) {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }

        half4 LightingUnlitWithShadows(SurfaceOutput s, half3 lightDir, half atten)
        {
            half shadowFactor = saturate(atten);
            half mixFactor = lerp(_ShadowDarkness, 1.0, shadowFactor);
            half4 col;
            col.rgb = s.Albedo * mixFactor;
            col.a = 1;
            return col;
        }
        ENDCG
    }
    FallBack ""Diffuse""
}";

		System.IO.Directory.CreateDirectory("Assets/Shaders");
		System.IO.File.WriteAllText(shaderPath, content);
		AssetDatabase.ImportAsset(shaderPath);
		unlitShadowShader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
		EditorUtility.DisplayDialog("Done", "Shader file created at: " + shaderPath, "OK");
	}

	private void CreateMaterial()
	{
		if (unlitShadowShader == null) {
			EditorUtility.DisplayDialog("Missing shader", "Please create the shader first.", "OK");
			return;
		}

		System.IO.Directory.CreateDirectory("Assets/Materials");
		Material mat = new Material(unlitShadowShader);
		mat.SetFloat("_ShadowDarkness", shadowDarkness);
		AssetDatabase.CreateAsset(mat, materialPath);
		AssetDatabase.SaveAssets();
		unlitShadowMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
		EditorUtility.DisplayDialog("Done", "Material created at: " + materialPath, "OK");
	}

	private void ApplyToTarget()
	{
		if (targetRoot == null) {
			targetRoot = Selection.activeGameObject;
			if (targetRoot == null) {
				EditorUtility.DisplayDialog("No target", "Please assign a Player Root or select one in the Hierarchy.", "OK");
				return;
			}
		}

		// Ensure shader and material exist (create material on the fly if needed)
		if (unlitShadowShader == null) {
			if (System.IO.File.Exists(shaderPath)) AssetDatabase.ImportAsset(shaderPath);
			unlitShadowShader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
		}
		if (unlitShadowMaterial == null) {
			if (unlitShadowShader == null) {
				EditorUtility.DisplayDialog("Missing shader", "Shader is missing. Create it first.", "OK");
				return;
			}
			unlitShadowMaterial = new Material(unlitShadowShader);
			unlitShadowMaterial.SetFloat("_ShadowDarkness", shadowDarkness);
			System.IO.Directory.CreateDirectory("Assets/Materials");
			AssetDatabase.CreateAsset(unlitShadowMaterial, materialPath);
			AssetDatabase.SaveAssets();
			unlitShadowMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
		}

		// Apply to all child renderers
		Renderer[] renderers = targetRoot.GetComponentsInChildren<Renderer>(true);
		int count = 0;
		foreach (Renderer r in renderers) {
			// Replace all material slots with our material
			Material[] mats = new Material[r.sharedMaterials.Length];
			for (int i = 0; i < mats.Length; i++) mats[i] = unlitShadowMaterial;
			r.sharedMaterials = mats;

			// Disable probes to prevent environment tinting
			r.lightProbeUsage = LightProbeUsage.Off;
			#if UNITY_2019_1_OR_NEWER
			r.reflectionProbeUsage = ReflectionProbeUsage.Off;
			#endif

			// Ensure shadows are preserved
			r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
			r.receiveShadows = true;

			count++;
		}

		EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
		AssetDatabase.SaveAssets();
		EditorUtility.DisplayDialog("Applied", $"Applied material to {count} renderers under '{targetRoot.name}'.\nLight/Reflection probes disabled, shadows kept.", "OK");
	}
}







