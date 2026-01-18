using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool that attempts to fix materials after converting a project to URP.
/// - Scans selected materials or all materials in the project.
/// - Replaces missing/legacy shaders with URP equivalents (Lit or Unlit).
/// - Copies common properties: _MainTex -> _BaseMap, _Color -> _BaseColor where possible.
/// </summary>
public static class FixMaterialURP
{
	public const string MenuRoot = "Tools/URP/Fix Converted Materials";

	[MenuItem(MenuRoot + "/Selected Materials", false, 100)]
	public static void FixSelectedMaterialsMenu()
	{
		var selectedGuids = Selection.assetGUIDs;
		var materialPaths = new List<string>();
		foreach (var guid in selectedGuids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			if (path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
				materialPaths.Add(path);
		}

		if (materialPaths.Count == 0)
		{
			EditorUtility.DisplayDialog("Fix URP Materials", "No materials selected. Select one or more .mat assets in the Project window.", "OK");
			return;
		}

		ProcessMaterials(materialPaths.ToArray());
	}

	[MenuItem(MenuRoot + "/All Materials in Project", false, 110)]
	public static void FixAllMaterialsMenu()
	{
		var guids = AssetDatabase.FindAssets("t:Material");
		var paths = new string[guids.Length];
		for (int i = 0; i < guids.Length; i++)
			paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);

		if (paths.Length == 0)
		{
			EditorUtility.DisplayDialog("Fix URP Materials", "No material assets found in the project.", "OK");
			return;
		}

		ProcessMaterials(paths);
	}

	private static void ProcessMaterials(string[] materialPaths)
	{
		int fixedCount = 0;
		int skippedCount = 0;
		try
		{
			for (int i = 0; i < materialPaths.Length; i++)
			{
				var path = materialPaths[i];
				EditorUtility.DisplayProgressBar("Fixing Materials (URP)", $"Processing {path} ({i+1}/{materialPaths.Length})", (float)i / materialPaths.Length);

				var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
				if (mat == null)
				{
					skippedCount++;
					continue;
				}

				string currentShaderName = mat.shader != null ? mat.shader.name : "Missing";

				// Decide target shader
				string targetShaderName = null;
				if (string.IsNullOrEmpty(currentShaderName) || currentShaderName.IndexOf("Mobile/Unlit", StringComparison.OrdinalIgnoreCase) >= 0
					|| currentShaderName.IndexOf("Unlit", StringComparison.OrdinalIgnoreCase) >= 0 && currentShaderName.IndexOf("Universal", StringComparison.OrdinalIgnoreCase) < 0)
				{
					targetShaderName = "Universal Render Pipeline/Unlit";
				}
				else if (currentShaderName.IndexOf("Legacy Shaders", StringComparison.OrdinalIgnoreCase) >= 0
					|| currentShaderName.IndexOf("Standard", StringComparison.OrdinalIgnoreCase) >= 0
					|| currentShaderName.IndexOf("Mobile/Diffuse", StringComparison.OrdinalIgnoreCase) >= 0
					|| currentShaderName == "Missing")
				{
					targetShaderName = "Universal Render Pipeline/Lit";
				}
				else
				{
					// If shader already looks URP, skip
					if (currentShaderName.StartsWith("Universal Render Pipeline/", StringComparison.OrdinalIgnoreCase))
					{
						skippedCount++;
						continue;
					}
					// fallback to Lit
					targetShaderName = "Universal Render Pipeline/Lit";
				}

				var targetShader = Shader.Find(targetShaderName);
				if (targetShader == null)
				{
					Debug.LogWarning($"Target shader not found: {targetShaderName}. Skipping material: {path}");
					skippedCount++;
					continue;
				}

				// Record undo for asset
				Undo.RegisterCompleteObjectUndo(mat, "Fix URP Material");

				// Preserve common properties
				// Main texture: Standard/_MainTex -> URP/_BaseMap
				if (mat.HasProperty("_MainTex"))
				{
					var mainTex = mat.GetTexture("_MainTex");
					// We'll set later after shader change if target supports it
					// Keep in temp vars via local dictionary
					mat.SetTexture("_MainTex", mainTex); // ensure kept (no-op)
				}

				// Keep color if exists
				if (mat.HasProperty("_Color"))
				{
					var color = mat.GetColor("_Color");
					mat.SetColor("_Color", color); // keep value accessible
				}

				// Change shader
				mat.shader = targetShader;

				// Map properties from common legacy names to URP names
				// _MainTex -> _BaseMap, _Color -> _BaseColor
				if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
				{
					var tex = mat.GetTexture("_MainTex");
					if (mat.HasProperty("_BaseMap"))
						mat.SetTexture("_BaseMap", tex);
					else if (mat.HasProperty("_BaseMap")) // redundant safety
						mat.SetTexture("_BaseMap", tex);
				}
				// Try to map color
				if (mat.HasProperty("_Color"))
				{
					var col = mat.GetColor("_Color");
					if (mat.HasProperty("_BaseColor"))
						mat.SetColor("_BaseColor", col);
				}

				// Attempt to set appropriate keywords / modes for Unlit vs Lit
				if (targetShaderName.EndsWith("/Unlit", StringComparison.OrdinalIgnoreCase))
				{
					// URP Unlit uses _BaseMap/_BaseColor
					// Nothing extra to enable by default
				}
				else if (targetShaderName.EndsWith("/Lit", StringComparison.OrdinalIgnoreCase))
				{
					// Ensure metallic/smoothness fallbacks if present in old shader
					if (mat.HasProperty("_Metallic"))
					{
						float metallic = mat.GetFloat("_Metallic");
						if (mat.HasProperty("_Metallic"))
							mat.SetFloat("_Metallic", metallic);
					}
				}

				EditorUtility.SetDirty(mat);
				fixedCount++;
			}
		}
		finally
		{
			EditorUtility.ClearProgressBar();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		EditorUtility.DisplayDialog("Fix URP Materials", $"Finished.\nFixed: {fixedCount}\nSkipped: {skippedCount}", "OK");
		Debug.Log($"FixMaterialURP: Finished. Fixed={fixedCount}, Skipped={skippedCount}");
	}
}





