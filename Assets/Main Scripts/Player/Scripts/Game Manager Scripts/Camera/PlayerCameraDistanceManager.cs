using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Single source of truth for Cinemachine camera distance.
/// Owns the effective CameraDistance (user zoom + optional combat clamp) and applies smoothing.
/// </summary>
public sealed class PlayerCameraDistanceManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CinemachineCamera cinemachineCamera;

    [Header("Distance (User Zoom)")]
    [SerializeField, Range(0f, 20f)] private float defaultDistance = 6f;
    [SerializeField, Range(0f, 20f)] private float minimumDistance = 1f;
    [SerializeField, Range(0f, 40f)] private float maximumDistance = 12f;

    [Header("Distance (Combat Clamp)")]
    [SerializeField, Range(0f, 20f)] private float combatMinDistance = 5f;
    [SerializeField, Range(0f, 20f)] private float combatMaxDistance = 9f;

    [Header("Smoothing")]
    [SerializeField, Range(0f, 30f)] private float smoothing = 4f;

    private CinemachinePositionComposer positionComposer;

    private float userTargetDistance;

    private bool combatClampEnabled;

    public float DefaultDistance => defaultDistance;
    public float UserTargetDistance => userTargetDistance;

    private void Awake()
    {
        if (cinemachineCamera == null)
            cinemachineCamera = GetComponentInChildren<CinemachineCamera>();

        ResolveBody();

        // ÉP BUỘC dùng defaultDistance làm gốc (Bỏ qua giá trị rác lưu trong Prefab)
        userTargetDistance = defaultDistance;

        // Snap (Gắn chặt) Camera về đúng vị trí ngay lập tức ở frame đầu tiên 
        // để tránh hiện tượng lúc mới spawn bị "zoom từ từ" vào nhân vật.
        if (positionComposer != null)
        {
            positionComposer.CameraDistance = defaultDistance;
        }
    }

    private void ResolveBody()
    {
        if (cinemachineCamera == null) { positionComposer = null; return; }
        positionComposer = cinemachineCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer;
    }

    public void SetUserTargetDistance(float distance)
    {
        userTargetDistance = Mathf.Clamp(distance, minimumDistance, maximumDistance);
    }

    public void AddUserTargetDelta(float delta)
    {
        SetUserTargetDistance(userTargetDistance + delta);
    }

    /// <summary>
    /// Clamp the effective camera distance while in combat.
    /// The clamp is intersected with [minimumDistance, maximumDistance].
    /// </summary>
    public void SetCombatClamp(bool enabled)
    {
        combatClampEnabled = enabled;
    }

    private void Update()
    {
        if (positionComposer == null)
        {
            // In case Cinemachine components were added/rebuilt at runtime.
            ResolveBody();
            if (positionComposer == null) return;
        }

        float clampMin = minimumDistance;
        float clampMax = maximumDistance;

        if (combatClampEnabled)
        {
            float a = Mathf.Min(combatMinDistance, combatMaxDistance);
            float b = Mathf.Max(combatMinDistance, combatMaxDistance);
            clampMin = Mathf.Max(clampMin, a);
            clampMax = Mathf.Min(clampMax, b);
        }

        float target = Mathf.Clamp(userTargetDistance, clampMin, clampMax);
        float current = positionComposer.CameraDistance;

        if (Mathf.Approximately(current, target))
            return;

        float t = smoothing <= 0f ? 1f : (smoothing * Time.deltaTime);
        positionComposer.CameraDistance = Mathf.Lerp(current, target, t);
    }
}

