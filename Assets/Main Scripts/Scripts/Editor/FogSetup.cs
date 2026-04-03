using UnityEngine;
using UnityEditor;

public class FogSetup
{
    // ── RenderSettings fog (distance haze 2D) ──────────────────────────
    [MenuItem("Tools/Fog/Add Simple Fog (Distance Haze)")]
    public static void CreateFog()
    {
        if (GameObject.Find("SimpleFog") != null)
        {
            Debug.LogWarning("[SimpleFog] Đã tồn tại trong scene.");
            Selection.activeGameObject = GameObject.Find("SimpleFog");
            return;
        }

        GameObject go = new GameObject("SimpleFog");
        SimpleFogController fog = go.AddComponent<SimpleFogController>();

        fog.enableFog      = true;
        fog.fogMode        = FogMode.Linear;
        fog.fogColor       = new Color(0.48f, 0.50f, 0.54f, 1f);
        fog.startDistance  = 30f;
        fog.endDistance    = 120f;
        fog.density        = 0.015f;

        Undo.RegisterCreatedObjectUndo(go, "Add Simple Fog");
        Selection.activeGameObject = go;
        Debug.Log("<color=cyan>[SimpleFog]</color> Đã tạo!");
    }

    // ── Volumetric 3D Particle Fog ──────────────────────────────────────
    [MenuItem("Tools/Fog/Add Volumetric Fog 3D (Particle)")]
    public static void CreateVolumetricFog()
    {
        if (GameObject.Find("VolumetricFog") != null)
        {
            Debug.LogWarning("[VolumetricFog] Đã tồn tại trong scene.");
            Selection.activeGameObject = GameObject.Find("VolumetricFog");
            return;
        }

        GameObject go = new GameObject("VolumetricFog");
        go.AddComponent<ParticleSystem>(); // RequireComponent cần có trước
        VolumetricFog vfog = go.AddComponent<VolumetricFog>();

        // Preset mặc định
        vfog.areaRadius   = 60f;
        vfog.areaHeight   = 12f;
        vfog.maxParticles = 200;
        vfog.particleSize = 30f;
        vfog.emissionRate = 40f;
        vfog.maxAlpha     = 0.07f;
        vfog.fogColor     = new Color(0.80f, 0.82f, 0.85f, 1f);
        vfog.driftSpeed   = 0.3f;
        vfog.followCamera = true;

        Undo.RegisterCreatedObjectUndo(go, "Add Volumetric Fog 3D");
        Selection.activeGameObject = go;
        Debug.Log("<color=cyan>[VolumetricFog]</color> Đã tạo! Bấm Play để xem sương mù 3D.");
    }
}
