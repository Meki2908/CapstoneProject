using UnityEngine;

[RequireComponent(typeof(Animator))]
public class WolfBossAnimationEvents : MonoBehaviour
{
    private WolfBossAI _bossAI;
    private BossPawDamage _pawDamage;
    private WolfBossVFXEvents _vfxEvents;

    private void Awake()
    {
        // WolfBossAI nằm trên Root (parent của Wolfboss_A)
        _bossAI = GetComponentInParent<WolfBossAI>();
        _pawDamage = GetComponent<BossPawDamage>();
        _vfxEvents = GetComponentInParent<WolfBossVFXEvents>();

        if (_bossAI == null)
            Debug.LogError("[WolfBossAnimationEvents] Không tìm thấy WolfBossAI trên parent! " +
                           "Script này cần nằm trên child của Root có WolfBossAI.");

        if (_pawDamage == null)
            Debug.LogWarning("[WolfBossAnimationEvents] Không tìm thấy BossPawDamage! " +
                             "Hãy gắn BossPawDamage lún cùng GameObject này.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Movement Lock/Unlock (cho FSM)

    /// <summary>
    /// Khoá di chuyển Boss khi bắt đầu thực hiện đòn đánh.
    /// Đặt Animation Event vào frame BẮT ĐẦU của pha tấn công.
    /// </summary>
    public void LockMovement()
    {
        _bossAI?.LockMovement();
    }

    /// <summary>
    /// Mở lại di chuyển sau khi animation kết thúc.
    /// Đặt Animation Event vào frame CUỐI của animation attack.
    /// </summary>
    public void UnlockMovement()
    {
        _bossAI?.UnlockMovement();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Paw Damage (qua BossPawDamage)

    /// <summary>
    /// Bật cửa sổ gây damage tại frame HIT của đòn đánh.
    /// Đặt Animation Event vào frame BẮT ĐẦU quét của chi trước.
    /// </summary>
    public void BeginPawDamage()
    {
        _pawDamage?.BeginPawDamage();
    }

    /// <summary>
    /// Tắt cửa sổ gây damage sau khi quét xong.
    /// Đặt Animation Event vào frame KẾT THÚC quét của chi trước.
    /// </summary>
    public void EndPawDamage()
    {
        _pawDamage?.EndPawDamage();
    }

    /// <summary>
    /// Frame sinh ra hình ảnh chém vào không khí (VFX).
    /// Đặt ở một frame bất kì trong lúc quẹt (nhập int: 0 cho tay trái, 1 cho tay phải).
    /// </summary>
    public void SpawnClawVFX(int pawIndex)
    {
        _pawDamage?.SpawnClawVFX(pawIndex);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  New Pooled VFX Events (via WolfBossVFXEvents)

    /// <summary>Spawn VFX cho kỹ năng Nổi Giận / Ultimate.</summary>
    public void SpawnUltimateVFX() => _vfxEvents?.SpawnUltimateVFX();

    /// <summary>Spawn VFX cho đòn Special Attack (sa).</summary>
    public void SpawnSpecialVFX() => _vfxEvents?.SpawnSpecialVFX();

    /// <summary>Spawn VFX cho Normal Attack (thường gán cùng frame với paw damage).</summary>
    public void SpawnNormalAttackVFX(int pawIndex) => _vfxEvents?.SpawnNormalAttackVFX(pawIndex);

    // ─────────────────────────────────────────────────────────────────────────
    //  Fang Spawning (via WolfBossAI)

    /// <summary>
    /// Triệu hồi 2 Fang tại frame chỉ định trong clip "ulti".
    /// Đặt Animation Event vào đúng frame xuất hiện của Fang trong animation.
    /// </summary>
    public void SpawnFangs() => _bossAI?.SpawnFangsFromAE();

    /// <summary>
    /// Bật Phase 2 Visuals (Eye Trails, Aura Smoke, v.v.) tại frame chỉ định.
    /// Thường đặt trong clip "roar" tại đỉnh của động tác gầm thét,
    /// hoặc đầu clip "ulti" trước khi Fang xuất hiện.
    /// </summary>
    public void ShowPhase2Visuals() => _bossAI?.ShowPhase2VisualsFromAE();
}
