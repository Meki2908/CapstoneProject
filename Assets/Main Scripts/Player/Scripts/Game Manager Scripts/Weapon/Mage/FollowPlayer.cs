using UnityEngine;

/// <summary>
/// VFX follow player (e.g. shield). Target must be set by the spawner — no global Find.
/// </summary>
public class FollowPlayer : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    [Tooltip("Lerp factor per frame toward target. Use 1 for instant snap (recommended for shields). 0.1 trails far behind when the player moves.")]
    [Range(0.01f, 1f)]
    public float followSpeed = 1f;
    [Tooltip("Offset from target")]
    public Vector3 offset = Vector3.zero;

    [Header("Debug")]
    [SerializeField] bool debugFollow = false;

    bool _loggedBind;

    private void Start()
    {
        if (target == null)
            Debug.LogWarning($"[FollowPlayer] Target is null on {gameObject.name}. Spawner must assign target immediately after Instantiate.");
        else if (debugFollow)
            Debug.Log($"[FollowPlayer] Start target={target.name} followSpeed={followSpeed:F2}", this);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + target.TransformVector(offset);
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed);

        if (debugFollow && !_loggedBind)
        {
            _loggedBind = true;
            float d = Vector3.Distance(transform.position, targetPos);
            Debug.Log($"[FollowPlayer] first LateUpdate distToTarget={d:F2} followSpeed={followSpeed:F2} target={target.name}", this);
        }
    }
}
