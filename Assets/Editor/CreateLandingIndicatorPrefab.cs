using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to create a ready-to-use LandingIndicator prefab and related simple assets.
/// Does NOT add any runtime LandingIndicator component so it is safe if that script is absent.
/// Menu: Tools/Create/Landing Indicator Prefab (Boss_Golem)
/// </summary>
public static class CreateLandingIndicatorPrefab
{
	[MenuItem("Tools/Create/Landing Indicator Prefab (Boss_Golem)")]
	public static void CreatePrefab()
	{
		string prefabFolder = "Assets/ASSETS/Dungeon_SaMac/Asset_Enemy_SaMac/Boss_Golem/Prefabs";
		EnsureFolderExists(prefabFolder);

		// Create a flat cylinder to act as a circular indicator
		GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		indicator.name = "LandingIndicator";
		indicator.transform.localScale = new Vector3(1f, 0.01f, 1f);
		var col = indicator.GetComponent<Collider>();
		if (col != null) Object.DestroyImmediate(col);

		// Create pale red transparent material
		string matPath = prefabFolder + "/LandingIndicator_Mat.mat";
		Material mat = new Material(Shader.Find("Standard"));
		mat.color = new Color(1f, 0.6f, 0.6f, 0.9f);
		mat.SetFloat("_Mode", 3f);
		mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		mat.SetInt("_ZWrite", 0);
		mat.DisableKeyword("_ALPHATEST_ON");
		mat.EnableKeyword("_ALPHABLEND_ON");
		mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
		mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
		AssetDatabase.CreateAsset(mat, matPath);

		var rend = indicator.GetComponent<Renderer>();
		if (rend != null) rend.sharedMaterial = mat;

		// Save indicator prefab
		string prefabPath = prefabFolder + "/LandingIndicator.prefab";
		PrefabUtility.SaveAsPrefabAsset(indicator, prefabPath);
		Object.DestroyImmediate(indicator);

		// Create simple orb prefab
		string orbMatPath = prefabFolder + "/OrbitingOrb_Mat.mat";
		Material orbMat = new Material(Shader.Find("Standard")) { color = new Color(1f, 0.5f, 0.5f, 1f) };
		AssetDatabase.CreateAsset(orbMat, orbMatPath);
		string orbPrefabPath = prefabFolder + "/OrbitingOrb.prefab";
		GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		orb.name = "OrbitingOrb";
		orb.transform.localScale = Vector3.one * 0.2f;
		var orbCol = orb.GetComponent<Collider>(); if (orbCol) Object.DestroyImmediate(orbCol);
		var orbRend = orb.GetComponent<Renderer>(); if (orbRend) orbRend.sharedMaterial = orbMat;
		PrefabUtility.SaveAsPrefabAsset(orb, orbPrefabPath);
		Object.DestroyImmediate(orb);

		// Create simple model VFX (ParticleSystem) prefab
		string modelVfxPath = prefabFolder + "/ModelVFX.prefab";
		GameObject modelVfx = new GameObject("ModelVFX");
		var ps = modelVfx.AddComponent<ParticleSystem>();
		var main = ps.main;
		main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0.2f, 1f));
		main.startLifetime = 1.2f;
		main.startSize = 0.6f;
		PrefabUtility.SaveAsPrefabAsset(modelVfx, modelVfxPath);
		Object.DestroyImmediate(modelVfx);

		// Create line projectile prefab
		string lineProjPath = prefabFolder + "/LineProjectile.prefab";
		GameObject lineProj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		lineProj.name = "LineProjectile";
		lineProj.transform.localScale = new Vector3(0.08f, 0.5f, 0.08f);
		var lpCol = lineProj.GetComponent<Collider>(); if (lpCol) Object.DestroyImmediate(lpCol);
		string lineMatPath = prefabFolder + "/LineProjectile_Mat.mat";
		Material lineMat = new Material(Shader.Find("Standard")) { color = Color.white };
		AssetDatabase.CreateAsset(lineMat, lineMatPath);
		var lpRend = lineProj.GetComponent<Renderer>(); if (lpRend) lpRend.sharedMaterial = lineMat;
		PrefabUtility.SaveAsPrefabAsset(lineProj, lineProjPath);
		Object.DestroyImmediate(lineProj);

		AssetDatabase.SaveAssets();
		Debug.Log($"Landing Indicator + helper prefabs created at {prefabFolder}");
	}

	private static void EnsureFolderExists(string folderPath)
	{
		if (AssetDatabase.IsValidFolder(folderPath)) return;
		string[] parts = folderPath.Split('/');
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

}


