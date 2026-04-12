using UnityEngine;

public class BossPawDamage : MonoBehaviour
{
    [Header("=== Damage Settings ===")]
    [Tooltip("Sát thương mỗi lần đòn quét trúng player.")]
    public float damagePerHit = 20f;
    [Tooltip("Cooldown giữa 2 lần gây damage trong cùng 1 hit window (tránh spam).")]
    public float damageCooldown = 0.3f;
    [Tooltip("Tag của Player object.")]
    public string playerTag = "Player";

    [Header("=== Paw Raycast ===")]
    [Tooltip("Transform xương chi trước TRÁI (Bip01 L Hand hoặc Bip01 L Forearm).")]
    public Transform pawLeft;
    [Tooltip("Transform xương chi trước PHẢI (Bip01 R Hand hoặc Bip01 R Forearm).")]
    public Transform pawRight;
    [Tooltip("Bán kính sphere overlap để detect player (units). Tăng nếu đòn hay hụt.")]
    public float pawRadius = 0.6f;
    [Tooltip("Layer mask của Player.")]
    public LayerMask playerLayerMask = ~0;

    [Header("=== Debug ===")]
    public bool showGizmos = true;

    // State
    private bool _damageWindowOpen = false;
    private float _lastDamageTime   = -99f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Animation Event API (gọi từ WolfBossAnimationEvents)

    /// <summary>Bật cửa sổ gây damage (frame bắt đầu hit).</summary>
    public void BeginPawDamage()
    {
        _damageWindowOpen = true;
    }

    /// <summary>Tắt cửa sổ gây damage (frame kết thúc hit).</summary>
    public void EndPawDamage()
    {
        _damageWindowOpen = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Runtime

    private void Update()
    {
        if (!_damageWindowOpen) return;
        if (Time.time - _lastDamageTime < damageCooldown) return;

        CheckPaw(pawLeft);
        CheckPaw(pawRight);
    }

    private void CheckPaw(Transform paw)
    {
        if (paw == null) return;

        // Sphere overlap tại vị trí paw bone
        Collider[] hits = Physics.OverlapSphere(paw.position, pawRadius, playerLayerMask);
        foreach (var hit in hits)
        {
            // Tìm PlayerHealth trực tiếp — không phụ thuộc vào Tag
            var ph = hit.GetComponentInParent<PlayerHealth>();
            if (ph == null) ph = hit.GetComponent<PlayerHealth>();
            if (ph == null) continue;

            ph.TakeDamage(damagePerHit, paw.position);
            _lastDamageTime = Time.time;
            _damageWindowOpen = false; // chỉ damage 1 lần mỗi window

            Debug.Log($"[BossPawDamage] {paw.name} hit Player for {damagePerHit} dmg");
            return; // chỉ damage 1 player mỗi frame
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = _damageWindowOpen ? Color.red : Color.yellow;
        if (pawLeft  != null) Gizmos.DrawWireSphere(pawLeft.position,  pawRadius);
        if (pawRight != null) Gizmos.DrawWireSphere(pawRight.position, pawRadius);
    }
#endif
}
