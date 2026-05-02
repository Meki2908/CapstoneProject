using UnityEngine;

public class SkillLock : MonoBehaviour
{
    public bool isPerformingSkill { get; private set; }

    private Animator _animator;
    private PlayerHealth _playerHealth;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        // Ưu tiên tìm PlayerHealth ở Root ngoài cùng (chứa Character) thay vì tìm ở cục Model con
        _playerHealth = GetComponentInParent<PlayerHealth>();
    }

    // Bắt đầu skill: Bật cờ khóa skill và khiên bất tử
    public void BeginSkillRootMotion(Animator animator, bool enableRootMotion = true)
    {
        isPerformingSkill = true;
        if (_playerHealth != null) _playerHealth.SetInvulnerable(true);
    }

    // Kết thúc skill: Tắt cờ và khiên
    public void EndSkillRootMotion(Animator animator)
    {
        isPerformingSkill = false;
        if (_playerHealth != null) _playerHealth.SetInvulnerable(false);
    }

    // Animation Event: AE bật cờ
    public void AE_LockCCAndApplyRootMotion()
    {
        isPerformingSkill = true;
        if (_playerHealth != null) _playerHealth.SetInvulnerable(true);
    }

    // Animation Event: AE tắt cờ
    public void AE_UnlockCCAndDisableRootMotion()
    {
        isPerformingSkill = false;
        if (_playerHealth != null) _playerHealth.SetInvulnerable(false);
    }

    // =========================================================================
    // LEGACY STUBS - BẢO HIỂM ANIMATION EVENTS
    // Các hàm dưới đây được giữ lại ở dạng RỖNG. Tuyệt đối không xóa chúng!
    // Tránh việc file FBX Animation của bạn đang gọi tên Event này mà không thấy, 
    // Unity sẽ báo lỗi đỏ lòm.
    // =========================================================================

    public void ApplyRootMotion() => AE_LockCCAndApplyRootMotion();
    
    public void DisableRootMotion() => AE_UnlockCCAndDisableRootMotion();

    public void LockPosition() 
    { 
        // Đã gỡ bỏ: Fusion FixedUpdateNetwork ở Character.cs sẽ tự lo việc đứng im
    }

    public void UnlockPosition() 
    { 
        // Đã gỡ bỏ
    }

    public void MaintainPosition() 
    { 
        // Đã gỡ bỏ vòng lặp Update ghì CharacterController
    }
}