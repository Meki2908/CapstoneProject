using UnityEngine;

/// <summary>
/// Gán mặt phẳng cổng cho shader <c>DungeonMania/URP/PortalPlaneClipLit</c> qua MaterialPropertyBlock.
/// Đặt script lên object boss (hoặc bất kỳ) và kéo Transform cổng (mặt phẳng = điểm + pháp tuyến).
/// </summary>
[DisallowMultipleComponent]
public class PortalPlaneClipBinder : MonoBehaviour
{
    [Tooltip("Transform định nghĩa mặt phẳng: position = điểm trên mặt; forward = pháp tuyến (phía 'positive' là nửa không gian hiện mesh khi Invert = false).")]
    [SerializeField] private Transform portalPlane;

    [Tooltip("Nếu bật: dùng -forward làm normal (một số prefab forward hướng vào trong cổng).")]
    [SerializeField] private bool useInverseForwardAsNormal = true;

    [SerializeField] private Renderer[] targetRenderers;

    [Tooltip("Tự gom Renderer trên object này + children nếu mảng trống.")]
    [SerializeField] private bool autoCollectRenderersIfEmpty = true;

    [SerializeField] private bool updateEveryFrame = true;

    private MaterialPropertyBlock _block;

    private static readonly int PortalPointId = Shader.PropertyToID("_PortalPoint");
    private static readonly int PortalNormalId = Shader.PropertyToID("_PortalNormal");
    private static readonly int PortalClipEnabledId = Shader.PropertyToID("_PortalClipEnabled");
    private static readonly int PortalInvertId = Shader.PropertyToID("_PortalInvert");
    private static readonly int PortalSoftnessId = Shader.PropertyToID("_PortalSoftness");

    [SerializeField] private float portalSoftness = 0.02f;
    [SerializeField] private bool portalClipEnabled = true;
    [SerializeField] private bool portalInvert = false;

    /// <summary>Khi bật: <see cref="ApplyNow"/> dùng điểm này thay vì <see cref="portalPlane"/>.position (dùng cho reveal).</summary>
    private bool _useManualPortalPoint;
    private Vector3 _manualPortalPoint;

    public bool PortalInvert => portalInvert;
    public Transform PortalPlaneTransform => portalPlane;
    public bool UseInverseForwardAsNormal => useInverseForwardAsNormal;

    private void Awake()
    {
        _block = new MaterialPropertyBlock();
        if (autoCollectRenderersIfEmpty && (targetRenderers == null || targetRenderers.Length == 0))
            targetRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void OnEnable()
    {
        ApplyNow();
    }

    private void LateUpdate()
    {
        if (updateEveryFrame)
            ApplyNow();
    }

    /// <summary>Đặt điểm trên mặt phẳng clip (world). Dùng khi muốn đẩy “rào” ẩn toàn boss rồi lerp về cổng.</summary>
    public void SetManualPortalPoint(Vector3 worldPoint)
    {
        _manualPortalPoint = worldPoint;
        _useManualPortalPoint = true;
        ApplyNow();
    }

    /// <summary>Bỏ override — lại theo <see cref="portalPlane"/>.</summary>
    public void ClearManualPortalPoint()
    {
        _useManualPortalPoint = false;
        ApplyNow();
    }

    /// <summary>
    /// Hết cutscene / không cần clip nữa: gán MPB tắt clip, rồi tắt component (không chạy LateUpdate).
    /// </summary>
    public void ShutdownPortalEffect(bool disableComponent = true)
    {
        _useManualPortalPoint = false;
        portalClipEnabled = false;
        if (portalPlane != null && targetRenderers != null)
            ApplyNow();
        else
            ApplyClipDisabledOnly();
        if (disableComponent)
            enabled = false;
    }

    private void ApplyClipDisabledOnly()
    {
        if (targetRenderers == null) return;
        foreach (var r in targetRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_block);
            _block.SetFloat(PortalClipEnabledId, 0f);
            r.SetPropertyBlock(_block);
        }
    }

    /// <summary>Gọi khi cổng/boss teleport — cập nhật một lần.</summary>
    public void ApplyNow()
    {
        if (portalPlane == null || targetRenderers == null)
            return;

        Vector3 point = _useManualPortalPoint ? _manualPortalPoint : portalPlane.position;
        Vector3 n = useInverseForwardAsNormal ? -portalPlane.forward : portalPlane.forward;
        n.Normalize();

        foreach (var r in targetRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_block);
            _block.SetVector(PortalPointId, point);
            _block.SetVector(PortalNormalId, new Vector4(n.x, n.y, n.z, 0f));
            _block.SetFloat(PortalClipEnabledId, portalClipEnabled ? 1f : 0f);
            _block.SetFloat(PortalInvertId, portalInvert ? 1f : 0f);
            _block.SetFloat(PortalSoftnessId, portalSoftness);
            r.SetPropertyBlock(_block);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (portalPlane == null) return;
        Gizmos.color = Color.cyan;
        Vector3 p = portalPlane.position;
        Vector3 n = useInverseForwardAsNormal ? -portalPlane.forward : portalPlane.forward;
        Gizmos.DrawLine(p, p + n.normalized * 1.5f);
    }
#endif
}
