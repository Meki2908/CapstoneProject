using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

/// <summary>
/// Áp dụng Render Distance bằng Camera.layerCullDistances.
///
/// Chỉ cull các layer CÓ THỂ cull an toàn: Enemy, VFX, Shield, EnemyHurtbox.
/// KHÔNG cull Default/Ground/Player/Water — tránh clip terrain và mesh quan trọng.
///
/// Đây là setting chất lượng hình ảnh (nhìn xa/gần) — KHÔNG phải setting tăng FPS chính.
/// Các setting thực sự ảnh hưởng FPS: Shadow Quality, Graphics Quality (LOD, MSAA, texture).
/// </summary>
public class RenderDistanceController : MonoBehaviour
{
    public static RenderDistanceController Instance { get; private set; }

    // Cull distance cho mỗi mức (chỉ áp dụng cho Enemy + VFX layers)
    // Index:  0     1     2     3      4      5
    // Label:  4x    8x    12x   16x    20x    24x
    private static readonly float[] enemyCullDist = {  80f, 120f, 180f, 250f, 350f, 0f };
    private static readonly float[] vfxCullDist   = {  50f,  80f, 120f, 180f, 250f, 0f };
    // 0f = không cull (render vô hạn) — 24x = max quality

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }
    }

    private void Start()
    {
        ApplyRenderDistance();
        GameSettings.OnSettingsChanged += ApplyRenderDistance;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        GameSettings.OnSettingsChanged -= ApplyRenderDistance;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyRenderDistance();
    }

    private void ApplyRenderDistance()
    {
        if (GameSettings.Instance == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        int index = Mathf.Clamp(
            GameSettings.Instance.renderDistanceIndex,
            0, enemyCullDist.Length - 1
        );

        float enemy = enemyCullDist[index];
        float vfx   = vfxCullDist[index];

        // 32 layers — mặc định 0 = không cull
        float[] distances = new float[32];

        // Chỉ cull layers an toàn — KHÔNG cull Default(0), Ground(3), Water(4), Player(8)
        distances[6]  = enemy;  // Enemy
        distances[7]  = enemy;  // Shield
        distances[9]  = enemy;  // EnemyHurtbox
        distances[13] = vfx;    // VFX

        cam.layerCullDistances = distances;
        // layerCullSpherical chỉ hỗ trợ built-in renderer, không dùng với URP
        if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null)
            cam.layerCullSpherical = true;

        int label = GameSettings.renderDistanceOptions[index];
        Debug.Log($"[RenderDistance] Applied: {label}x → Enemy={enemy}, VFX={vfx}");
    }

    public static RenderDistanceController EnsureInstance()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("[RenderDistanceController]");
        Instance = go.AddComponent<RenderDistanceController>();
        return Instance;
    }
}
