using UnityEditor;
using UnityEngine;

// Editor utility to inspect Monster03 GameObjects, renderers and materials.
// Menu: Tools/Debug/Monster03 -> Run checks or apply quick fixes.
public static class Monster03Debugger
{
    [MenuItem("Tools/Debug/Monster03/Report Status")]
    public static void ReportStatus()
    {
        var all = GameObject.FindObjectsOfType<GameObject>();
        int found = 0;
        foreach (var go in all)
        {
            if (go.name.Contains("Monster03"))
            {
                found++;
                Debug.Log($"--- Monster03 object: {GetPath(go)} ---");
                Debug.Log($"ActiveSelf: {go.activeSelf}, ActiveInHierarchy: {go.activeInHierarchy}, Layer: {LayerMask.LayerToName(go.layer)}");
                var mr = go.GetComponent<MeshRenderer>();
                var smr = go.GetComponent<SkinnedMeshRenderer>();
                if (mr != null)
                {
                    Debug.Log($"MeshRenderer enabled: {mr.enabled}, shadowCastingMode: {mr.shadowCastingMode}, receiveShadows: {mr.receiveShadows}");
                    ReportMaterials(mr.sharedMaterials);
                }
                if (smr != null)
                {
                    Debug.Log($"SkinnedMeshRenderer enabled: {smr.enabled}, shadowCastingMode: {smr.shadowCastingMode}, receiveShadows: {smr.receiveShadows}");
                    ReportMaterials(smr.sharedMaterials);
                }
                var trans = go.transform;
                Debug.Log($"Position: {trans.position}, Rotation: {trans.eulerAngles}, Scale: {trans.localScale}");
            }
        }
        if (found == 0) Debug.LogWarning("No GameObjects with name containing 'Monster03' found in the scene.");
    }

    [MenuItem("Tools/Debug/Monster03/Force Reassign URP Lit")]
    public static void ForceReassignURPLit()
    {
        var gos = GameObject.FindObjectsOfType<GameObject>();
        int reassigned = 0;
        for (int i = 0; i < gos.Length; i++)
        {
            var go = gos[i];
            if (!go.name.Contains("Monster03")) continue;
            var mr = go.GetComponent<MeshRenderer>();
            var smr = go.GetComponent<SkinnedMeshRenderer>();
            if (mr != null) reassigned += ReplaceMaterials(mr.sharedMaterials);
            if (smr != null) reassigned += ReplaceMaterials(smr.sharedMaterials);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Monster03Debugger] Reassigned {reassigned} material slots to URP/Lit fallback.");
    }

    static int ReplaceMaterials(Material[] mats)
    {
        int count = 0;
        for (int i = 0; i < mats.Length; i++)
        {
            var mat = mats[i];
            if (mat == null) continue;
            Shader shader = null;
            try { shader = mat.shader; } catch { shader = null; }
            if (shader == null || shader.name == "Hidden/InternalErrorShader")
            {
                Shader newShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (newShader != null)
                {
                    Undo.RecordObject(mat, "Replace missing shader");
                    mat.shader = newShader;
                    EditorUtility.SetDirty(mat);
                    count++;
                }
            }
        }
        return count;
    }

    static void ReportMaterials(Material[] mats)
    {
        if (mats == null || mats.Length == 0) { Debug.Log("No materials assigned."); return; }
        for (int i = 0; i < mats.Length; i++)
        {
            var mat = mats[i];
            if (mat == null) { Debug.Log($"Material slot {i}: null"); continue; }
            string shaderName = "null";
            try { shaderName = mat.shader != null ? mat.shader.name : "null"; } catch { shaderName = "error"; }
            Debug.Log($"Material slot {i}: {AssetDatabase.GetAssetPath(mat)} | Shader: {shaderName} | renderQueue: {mat.renderQueue}");
            // Common properties to check
            if (mat.HasProperty("_Surface")) Debug.Log($"  _Surface: {mat.GetFloat("_Surface")}");
            if (mat.HasProperty("_AlphaClip")) Debug.Log($"  _AlphaClip: {mat.GetFloat("_AlphaClip")}");
            if (mat.HasProperty("_Color")) Debug.Log($"  _Color: {mat.GetColor("_Color")}");
        }
    }

    static string GetPath(GameObject go)
    {
        string path = go.name;
        var t = go.transform;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}


