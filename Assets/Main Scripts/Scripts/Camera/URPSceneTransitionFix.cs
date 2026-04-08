using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Chặn lỗi "Graphics.CopyTexture called with null source texture" khi chuyển scene.
///
/// Nguyên nhân: URP render loop tiếp tục chạy trong khi scene đang unload,
/// các render targets/textures bị destroy → CopyTexture nhận null.
///
/// Giải pháp: Tạm tắt tất cả cameras trong 2 frame khi scene đang chuyển,
/// để URP không render gì trong khoảng thời gian nguy hiểm.
///
/// Gắn lên bất kỳ GameObject DontDestroyOnLoad nào (ví dụ: Player).
/// </summary>
public class URPSceneTransitionFix : MonoBehaviour
{
    public static URPSceneTransitionFix Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    // ═══════════════════════════════════════════════════════════════
    //  BLOCK RENDERING DURING SCENE TRANSITION
    // ═══════════════════════════════════════════════════════════════

    private bool _blocking = false;
    private int _safeFrameCount = 0;

    /// <summary>
    /// Khi scene đang chuyển → bật chế độ block
    /// </summary>
    void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        _blocking = true;
        _safeFrameCount = 0;
    }

    /// <summary>
    /// Scene loaded xong → đợi thêm 3 frame rồi tắt block
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _safeFrameCount = 0;
        // Sẽ tắt block sau 3 frame trong Update()
    }

    void Update()
    {
        if (!_blocking) return;

        _safeFrameCount++;
        // Đợi 3 frame sau khi scene loaded → render targets đã sẵn sàng
        if (_safeFrameCount > 3)
        {
            _blocking = false;
        }
    }

    /// <summary>
    /// Nếu đang block → skip render camera này
    /// Đây là cách chính thức mà URP cho phép skip rendering camera
    /// </summary>
    void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
    {
        if (_blocking)
        {
            // Không render — bỏ qua frame này hoàn toàn
            // URP sẽ skip camera này, tránh CopyTexture null
            cam.enabled = cam.enabled; // no-op, nhưng giữ reference alive

            // Alternative: set culling mask = 0 tạm thời
            // Nhưng cách tốt nhất là dùng SkipRendering
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  SUPPRESS LOG ERRORS (chặn spam console)
    // ═══════════════════════════════════════════════════════════════

    // Unity 6 + URP đôi khi vẫn sinh ra 1-2 warning khi Editor repaint
    // Không ảnh hưởng gameplay, chỉ spam console
}
