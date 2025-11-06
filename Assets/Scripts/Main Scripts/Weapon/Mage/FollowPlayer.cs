using UnityEngine;

/// <summary>
/// Component để VFX follow player movement (như khiên)
/// </summary>
public class FollowPlayer : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("Target to follow (auto find player if null)")]
    public Transform target;
    [Tooltip("Follow speed (0 = instant, 1 = smooth)")]
    public float followSpeed = 0.1f;
    [Tooltip("Offset from target")]
    public Vector3 offset = Vector3.zero;

    private void Start()
    {
        if (target == null)
        {
            // Auto find player
            var player = FindObjectOfType<Character>();
            target = player?.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + target.TransformVector(offset);
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed);

        // Optional: rotate to face same direction as player
        // transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, followSpeed);
    }
}
