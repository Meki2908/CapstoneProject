using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class MinimapCameraFollow : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform player;
    public Vector3 offset = new Vector3(0, 50, 0);

    [Header("Rotation Settings")]
    [Tooltip("Kéo Main Camera vào đây để minimap xoay theo hướng nhìn của camera. Để trống sẽ xoay theo player body.")]
    public Transform cameraTransform;

    [Header("UI Settings")]
    public RectTransform playerIcon;

    [Header("Minimap Toggle")]
    [Tooltip("Panel/Canvas cha chứa toàn bộ minimap UI. Để trống sẽ tự động tìm.")]
    public GameObject minimapUIRoot;

    [Header("Brightness (Night Override)")]
    [Tooltip("TẮT mặc định. Bật sẽ đổi RenderSettings.ambient mỗi lần render minimap — rất tốn trên URP. Nên dùng Volume riêng layer minimap.")]
    public bool brightenMinimap = false;
    [Range(0.5f, 3f)] public float minimapAmbientIntensity = 1.5f;

    private Camera _minimapCamera;
    private Color _savedAmbientColor;
    private float _savedAmbientIntensity;
    private AmbientMode _savedAmbientMode;
    private bool _brightnessHooksRegistered;

    void Start()
    {
        _minimapCamera = GetComponent<Camera>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("[MinimapCameraFollow] Đã tự động tìm thấy Player!");
            }
            else
            {
                Debug.LogWarning("[MinimapCameraFollow] Không tìm thấy GameObject với tag 'Player'. Hãy gán thủ công trong Inspector!");
            }
        }

        // Tự động tìm Main Camera nếu chưa gán
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            Debug.Log("[MinimapCameraFollow] Tự động dùng Main Camera để xoay minimap.");
        }

        // Auto-find minimap UI root nếu chưa gán
        if (minimapUIRoot == null)
        {
            FindMinimapUIRoot();
        }

        // Apply trạng thái minimap từ settings
        ApplyMinimapToggle();

        // Subscribe để cập nhật khi user đổi settings
        GameSettings.OnSettingsChanged += OnSettingsChanged;

        RegisterBrightnessHooksIfNeeded();
    }

    void OnDestroy()
    {
        GameSettings.OnSettingsChanged -= OnSettingsChanged;
        UnregisterBrightnessHooks();
    }

    void RegisterBrightnessHooksIfNeeded()
    {
        if (!brightenMinimap || _brightnessHooksRegistered) return;
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        RenderPipelineManager.endCameraRendering += OnEndCamera;
        _brightnessHooksRegistered = true;
    }

    void UnregisterBrightnessHooks()
    {
        if (!_brightnessHooksRegistered) return;
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        RenderPipelineManager.endCameraRendering -= OnEndCamera;
        _brightnessHooksRegistered = false;
    }

    void LateUpdate()
    {
        if (_minimapCamera != null && !_minimapCamera.enabled)
            return;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            else return;
        }

        // Camera minimap theo sát player
        transform.position = player.position + offset;

        // Xoay theo camera (hoặc player nếu không có camera)
        float yAngle = (cameraTransform != null) ? cameraTransform.eulerAngles.y : player.eulerAngles.y;
        transform.rotation = Quaternion.Euler(90f, yAngle, 0f);

        // Icon player luôn hướng lên trên UI (không xoay theo map)
        if (playerIcon != null)
            playerIcon.localRotation = Quaternion.identity;
    }

    // ==================== MINIMAP BRIGHTNESS ====================

    void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
    {
        if (!brightenMinimap || _minimapCamera == null || cam != _minimapCamera) return;

        // Lưu lại ambient hiện tại
        _savedAmbientColor = RenderSettings.ambientLight;
        _savedAmbientIntensity = RenderSettings.ambientIntensity;
        _savedAmbientMode = RenderSettings.ambientMode;

        // Đổi sang sáng cho minimap
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.white;
        RenderSettings.ambientIntensity = minimapAmbientIntensity;
    }

    void OnEndCamera(ScriptableRenderContext ctx, Camera cam)
    {
        if (!brightenMinimap || _minimapCamera == null || cam != _minimapCamera) return;

        // Khôi phục ambient gốc
        RenderSettings.ambientMode = _savedAmbientMode;
        RenderSettings.ambientLight = _savedAmbientColor;
        RenderSettings.ambientIntensity = _savedAmbientIntensity;
    }

    // ==================== MINIMAP TOGGLE ====================

    private void OnSettingsChanged()
    {
        ApplyMinimapToggle();
    }

    /// <summary>
    /// Bật/tắt minimap dựa trên GameSettings.miniMapEnabled
    /// </summary>
    private void ApplyMinimapToggle()
    {
        bool enabled = true;
        if (GameSettings.Instance != null)
            enabled = GameSettings.Instance.miniMapEnabled;

        // Tắt/bật camera minimap
        if (_minimapCamera != null)
            _minimapCamera.enabled = enabled;

        // Tắt/bật UI panel
        if (minimapUIRoot != null)
            minimapUIRoot.SetActive(enabled);
    }

    /// <summary>
    /// Tự động tìm panel/canvas cha chứa minimap UI.
    /// </summary>
    private void FindMinimapUIRoot()
    {
        // Cách 1: Tìm RawImage dùng renderTexture của camera này
        if (_minimapCamera != null && _minimapCamera.targetTexture != null)
        {
            var rawImages = FindObjectsByType<RawImage>(FindObjectsSortMode.None);
            foreach (var img in rawImages)
            {
                if (img.texture == _minimapCamera.targetTexture)
                {
                    Transform parent = img.transform.parent;
                    if (parent != null)
                    {
                        minimapUIRoot = parent.gameObject;
                        Debug.Log($"[MinimapCameraFollow] Auto-found minimap UI root: {minimapUIRoot.name}");
                        return;
                    }
                }
            }
        }

        // Cách 2: Tìm bằng tên
        string[] possibleNames = { "MinimapPanel", "Panel_Minimap", "Minimap", "MinimapUI", "MiniMap" };
        foreach (string name in possibleNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                minimapUIRoot = obj;
                Debug.Log($"[MinimapCameraFollow] Found minimap UI by name: {name}");
                return;
            }
        }

        Debug.LogWarning("[MinimapCameraFollow] Không tìm thấy minimap UI root! Hãy gán thủ công trong Inspector.");
    }
}
