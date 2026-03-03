using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Editor utility to add a trigger Hurtbox child and EnemyDeathController to selected enemy prefabs.
/// Usage: select prefab assets in Project window, then Tools -> Enemy Setup -> Add Hurtbox & Death Controller to Selected Prefabs
/// </summary>
public class EnemyPrefabSetupUtility
{
    const string hurtboxName = "Hurtbox";

    [MenuItem("Tools/Enemy Setup/Add Hurtbox & Death Controller to Selected Prefabs")]
    public static void AddHurtboxAndDeathControllerToSelected()
    {
        var guids = Selection.assetGUIDs;
        if (guids == null || guids.Length == 0)
        {
            Debug.LogWarning("[EnemyPrefabSetupUtility] No assets selected. Select enemy prefab assets in Project window.");
            return;
        }

        int processed = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            // Work on prefab contents
            GameObject instance = PrefabUtility.InstantiatePrefab(go) as GameObject;
            if (instance == null) continue;

            // Add EnemyDeathController if missing
            if (instance.GetComponent<EnemyDeathController>() == null)
            {
                instance.AddComponent<EnemyDeathController>();
            }

            // Add Hurtbox child if missing
            var existing = instance.transform.Cast<Transform>().FirstOrDefault(t => t.name == hurtboxName);
            if (existing == null)
            {
                var hb = new GameObject(hurtboxName);
                hb.transform.SetParent(instance.transform, false);
                hb.transform.localPosition = Vector3.zero;
                var sc = hb.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 1.0f;

                // Try to set layer "EnemyHurtbox" if exists
                int layerIdx = LayerMask.NameToLayer("EnemyHurtbox");
                if (layerIdx >= 0)
                    hb.layer = layerIdx;
                else
                    Debug.LogWarning("[EnemyPrefabSetupUtility] Layer 'EnemyHurtbox' not found. Create it and re-run to assign layer automatically.");
            }

            // Apply changes back to prefab
            PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.UserAction);
            GameObject.DestroyImmediate(instance);
            processed++;
        }

        Debug.Log($"[EnemyPrefabSetupUtility] Processed {processed} prefabs.");
    }
}


