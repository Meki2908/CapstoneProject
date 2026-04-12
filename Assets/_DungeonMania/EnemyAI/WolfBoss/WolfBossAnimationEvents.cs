using UnityEngine;

[RequireComponent(typeof(Animator))]
public class WolfBossAnimationEvents : MonoBehaviour
{
    private WolfBossAI _bossAI;
    private BossPawDamage _pawDamage;

    private void Awake()
    {
        // WolfBossAI nằm trên Root (parent của Wolfboss_A)
        _bossAI = GetComponentInParent<WolfBossAI>();
        _pawDamage = GetComponent<BossPawDamage>();

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
}
