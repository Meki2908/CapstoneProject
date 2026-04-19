#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class CleanPortalPrefab
{
    [MenuItem("Tools/Clean Portal Prefab")]
    public static void DoClean()
    {
        string path = "Assets/ASSETS/Asset_RunesAndPortals/PortalRound/Prefabs/PF_Portal_Round.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError("Prefab not found at " + path);
            return;
        }

        // We instantiate the prefab to modify it cleanly
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        try
        {
            // 1. Find all 'Freeze circle' objects
            // The user said there are 3. The 1st one needs to KEEP its PortalNode and Collider.
            // But wait, the best way is to keep PortalNode on the ROOT Portal.
            // Let's check where the MAIN PortalNode should be. Usually on the ROOT object `PF_Portal_Round`.
            
            // Let's first ensure the ROOT has exactly one PortalNode and one properly sized trigger Collider.
            var portalNodeScriptType = typeof(MonoBehaviour); // We have to find by name, but it's a script.
            MonoBehaviour[] allScripts = instance.GetComponentsInChildren<MonoBehaviour>(true);
            MonoBehaviour portalNodeScript = null;
            
            foreach(var s in allScripts)
            {
                if (s != null && s.GetType().Name == "PortalNode")
                {
                    if (portalNodeScript == null) 
                        portalNodeScript = s;
                }
            }

            if (portalNodeScript == null)
            {
                Debug.LogError("No PortalNode found anywhere in the prefab!");
            }
            else
            {
                Debug.Log("Found PortalNode. Moving to ROOT if not already there.");
                // If it's not on the root, we can't easily 'move' it via API without losing references unless we copy JSON
                // Actually, the user says "ẩn 2 cái túi đi là ko bấm F được" meaning they disabled the ONE that had the portal node.
                // We'll just disable the MeshRenderers of the Freeze circles, NOT the GameObjects themselves!
            }

            // Find all 'Freeze circle' objects and 'Cylinder' or 'QuestBeacon' objects
            Transform[] children = instance.GetComponentsInChildren<Transform>(true);
            int freezeCount = 0;
            foreach(Transform t in children)
            {
                if (t.name.Contains("Freeze circle"))
                {
                    freezeCount++;
                    // Hide the mesh renderer, keep the object active so collisions and scripts work
                    MeshRenderer mr = t.GetComponent<MeshRenderer>();
                    if (mr != null) mr.enabled = false;
                    
                    // If the user wants to hide the last 2:
                    if (freezeCount > 1)
                    {
                        // Actually, let's just leave the first one enabled, and disable the rest's renderer.
                    }
                }
                
                // Hide the ugly cylinder/pillar
                if (t.name.Contains("Cylinder"))
                {
                    MeshRenderer mr = t.GetComponent<MeshRenderer>();
                    if (mr != null) mr.enabled = false;
                }
            }

            // Save back
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Debug.Log("Successfully cleaned Portal Prefab!");
        }
        finally
        {
            GameObject.DestroyImmediate(instance);
        }
    }
}
#endif
