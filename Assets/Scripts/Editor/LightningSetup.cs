using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor helper: tạo GameObject "RedLightningEffect" chứa VFX tia sét đỏ.
/// Đặt GameObject này đúng vị trí hố đen / góc map trong Scene View là xong.
/// Không có Light, không có Skybox — chỉ thuần visual tia sét Additive.
/// </summary>
public class LightningSetup
{
    [MenuItem("Tools/Add Red Lightning VFX")]
    public static void CreateLightningVFX()
    {
        if (GameObject.Find("RedLightningEffect") != null)
        {
            Debug.LogWarning("RedLightningEffect đã tồn tại trong scene. Xóa cái cũ trước khi tạo lại.");
            return;
        }

        GameObject rootObj = new GameObject("RedLightningEffect");

        LightningFlash fx = rootObj.AddComponent<LightningFlash>();

        // --- Sky Tracking ---
        // followCamera = false vì hố đen là object cố định trong world.
        // Chỉ bật true nếu hố đen là texture trong skybox material.
        fx.followCamera = false;
        fx.skyHeight    = 150f; // không dùng khi followCamera = false
        // --- Timing ---
        fx.minTime      = 0.4f;
        fx.maxTime      = 2.0f;
        fx.flashDuration = 0.09f;

        // --- Bolts ---
        fx.boltCount    = 4;
        fx.boltSegments = 18;
        fx.boltJitter   = 7f;
        fx.boltLength   = 90f;   // chiều dài tia từ tâm hố đen xuống
        fx.spawnRadius  = 35f;   // vùng phát xung quanh tâm

        // --- Màu sắc ---
        // Core: gần trắng ở gốc → trông rực sáng với Additive blend
        fx.coreColor = new Color(1f, 0.85f, 0.85f, 1f);
        fx.coreWidth = 0.7f;

        // Glow: đỏ đậm ấm, rộng hơn → tạo hào quang
        fx.glowColor = new Color(1f, 0.08f, 0.04f, 0.65f);
        fx.glowWidth = 3.5f;

        Undo.RegisterCreatedObjectUndo(rootObj, "Create Red Lightning VFX");
        Selection.activeGameObject = rootObj;

        Debug.Log("<color=red>[RedLightningVFX]</color> Đã tạo xong! " +
                  "Di chuyển GameObject 'RedLightningEffect' đến đúng vị trí hố đen trong Scene, rồi bấm Play để xem.");
    }
}
