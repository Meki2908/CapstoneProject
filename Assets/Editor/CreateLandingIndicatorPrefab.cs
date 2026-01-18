using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to create a ready-to-use LandingIndicator prefab.
/// It creates a flat red cylinder (no collider), assigns a material, adds the LandingIndicator script,
/// and saves the prefab to Assets/Prefabs/LandingIndicator.prefab.
/// Use: Tools/Create/Landing Indicator Prefab
/// </summary>
public static class CreateLandingIndicatorPrefab
{
	[MenuItem("Tools/Create/Landing Indicator Prefab")]
	public static void CreatePrefab()
	{
		// Target folder inside Boss_Golem Prefabs
		string prefabFolder = "Assets/ASSETS/Dungeon_SaMac/Asset_Enemy_SaMac/Boss_Golem/Prefabs";
		EnsureFolderExists(prefabFolder);

		// Create a flat cylinder to act as a circular indicator (works in 3D scenes)
		GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		indicator.name = "LandingIndicator";
		// Make it very thin so it appears as a flat disc
		indicator.transform.localScale = new Vector3(1f, 0.01f, 1f);

		// Remove collider (we don't need it)
		var col = indicator.GetComponent<Collider>();
		if (col != null) Object.DestroyImmediate(col);

		// Create a pale red, mostly-transparent material so the indicator can fade smoothly
		string matPath = prefabFolder + "/LandingIndicator_Mat.mat";
		Material mat = new Material(Shader.Find("Standard"));
		// pale red (light) with slight alpha
		mat.color = new Color(1f, 0.6f, 0.6f, 0.9f);
		// Configure Standard shader to use transparent blending so alpha changes are visible
		mat.SetFloat("_Mode", 3f); // 3 == Transparent mode for Standard shader
		mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		mat.SetInt("_ZWrite", 0);
		mat.DisableKeyword("_ALPHATEST_ON");
		mat.EnableKeyword("_ALPHABLEND_ON");
		mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
		mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
		AssetDatabase.CreateAsset(mat, matPath);

		// Assign material
		var rend = indicator.GetComponent<Renderer>();
		if (rend != null)
		{
			rend.sharedMaterial = mat;
		}

		// Add landing indicator behaviour
		indicator.AddComponent<LandingIndicator>();

		// Save as prefab
		string prefabPath = prefabFolder + "/LandingIndicator.prefab";
		PrefabUtility.SaveAsPrefabAsset(indicator, prefabPath);
		
		// --- Create additional assets shown in inspector ---

		// 1) Orbiting orb material + prefab
		string orbMatPath = prefabFolder + "/OrbitingOrb_Mat.mat";
		Material orbMat = new Material(Shader.Find("Standard"));
		orbMat.color = new Color(1f, 0.5f, 0.5f, 1f);
		AssetDatabase.CreateAsset(orbMat, orbMatPath);

		string orbPrefabPath = prefabFolder + "/OrbitingOrb.prefab";
		GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		orb.name = "OrbitingOrb";
		orb.transform.localScale = Vector3.one * 0.2f;
		var orbCol = orb.GetComponent<Collider>();
		if (orbCol != null) Object.DestroyImmediate(orbCol);
		var orbRend = orb.GetComponent<Renderer>();
		if (orbRend != null) orbRend.sharedMaterial = orbMat;
		PrefabUtility.SaveAsPrefabAsset(orb, orbPrefabPath);
		Object.DestroyImmediate(orb);

		// 2) Model VFX prefab (simple ParticleSystem)
		string modelVfxPath = prefabFolder + "/ModelVFX.prefab";
		GameObject modelVfx = new GameObject("ModelVFX");
		var ps = modelVfx.AddComponent<ParticleSystem>();
		var main = ps.main;
		main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0.2f, 1f));
		main.startLifetime = 1.2f;
		main.startSize = 0.6f;
		PrefabUtility.SaveAsPrefabAsset(modelVfx, modelVfxPath);
		Object.DestroyImmediate(modelVfx);

		// 3) Line projectile prefab (thin cylinder)
		string lineProjPath = prefabFolder + "/LineProjectile.prefab";
		GameObject lineProj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		lineProj.name = "LineProjectile";
		lineProj.transform.localScale = new Vector3(0.08f, 0.5f, 0.08f); // thin and long
		var lpCol = lineProj.GetComponent<Collider>();
		if (lpCol != null) Object.DestroyImmediate(lpCol);
		// simple white material
		string lineMatPath = prefabFolder + "/LineProjectile_Mat.mat";
		Material lineMat = new Material(Shader.Find("Standard"));
		lineMat.color = Color.white;
		AssetDatabase.CreateAsset(lineMat, lineMatPath);
		var lpRend = lineProj.GetComponent<Renderer>();
		if (lpRend != null) lpRend.sharedMaterial = lineMat;
		PrefabUtility.SaveAsPrefabAsset(lineProj, lineProjPath);
		Object.DestroyImmediate(lineProj);

		// Helper to ensure nested folders exist
		void EnsureFolderExists(string folderPath)
		{
			if (AssetDatabase.IsValidFolder(folderPath)) return;
			string[] parts = folderPath.Split('/');
			if (parts.Length == 0) return;
			string current = parts[0];
			for (int i = 1; i < parts.Length; i++)
			{
				string next = current + "/" + parts[i];
				if (!AssetDatabase.IsValidFolder(next))
				{
					AssetDatabase.CreateFolder(current, parts[i]);
				}
				current = next;
			}
		}

		// Assign created assets to any GolemAI components found in prefabs inside the target folder.
		void AssignToGolemPrefabs(string targetFolder, string landingPrefabAssetPath, string orbitPrefabAssetPath, string modelVfxAssetPath, string lineProjAssetPath, string orbitMatPath)
		{
			GameObject landingPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(landingPrefabAssetPath);
			GameObject orbitPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(orbitPrefabAssetPath);
			GameObject modelVfxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelVfxAssetPath);
			GameObject lineProjAsset = AssetDatabase.LoadAssetAtPath<GameObject>(lineProjAssetPath);
			Material orbitMatAsset = AssetDatabase.LoadAssetAtPath<Material>(orbitMatPath);

			if (landingPrefabAsset == null && orbitPrefabAsset == null && modelVfxAsset == null && lineProjAsset == null && orbitMatAsset == null) return;

			string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { targetFolder });
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				// Skip our created prefabs
				if (path == landingPrefabAssetPath || path == orbitPrefabAssetPath || path == modelVfxAssetPath || path == lineProjAssetPath) continue;

				GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
				if (prefabRoot == null) continue;

				var golemAI = prefabRoot.GetComponentInChildren<GolemAI>(true);
				if (golemAI != null)
				{
					if (landingPrefabAsset != null) golemAI.landingIndicatorPrefab = landingPrefabAsset;
					if (orbitPrefabAsset != null) golemAI.orbitingOrbPrefab = orbitPrefabAsset;
					if (modelVfxAsset != null) golemAI.modelVFXPrefab = modelVfxAsset;
					if (lineProjAsset != null) golemAI.lineProjectilePrefab = lineProjAsset;
					if (orbitMatAsset != null) golemAI.orbitingOrbMaterial = orbitMatAsset;

					EditorUtility.SetDirty(prefabRoot);
					PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
					Debug.Log($"Assigned created assets to {path}");
				}

				PrefabUtility.UnloadPrefabContents(prefabRoot);
			}
		}

		// Assign to golem prefabs
		AssignToGolemPrefabs(prefabFolder, prefabPath, orbPrefabPath, modelVfxPath, lineProjPath, orbMatPath);

		// Cleanup temp object
		Object.DestroyImmediate(indicator);

		AssetDatabase.SaveAssets();
		Debug.Log($"Landing Indicator prefab created at {prefabPath} (material: {matPath})");
	}
}


