using UnityEngine;

public class SkillLock : MonoBehaviour
{
    public bool isPerformingSkill { get; private set; }

    private Animator _animator;
    private CharacterController _controller;
    private Vector3 _skillStartPosition;
    private bool _positionLocked = false;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
    }

    // Bắt đầu skill: set cờ + (tuỳ chọn) bật root motion
    public void BeginSkillRootMotion(Animator animator, bool enableRootMotion = true)
    {
        isPerformingSkill = true;
        var anim = animator != null ? animator : _animator;
        if (anim) anim.applyRootMotion = enableRootMotion;
        // Make player invulnerable for the duration of the skill
        var ph = GetComponent<PlayerHealth>();
        ph?.SetInvulnerable(true);
    }

    // Kết thúc skill: clear cờ + tắt root motion
    public void EndSkillRootMotion(Animator animator)
    {
        isPerformingSkill = false;
        var anim = animator != null ? animator : _animator;
        if (anim) anim.applyRootMotion = false;
        // Remove invulnerability when skill ends
        var ph = GetComponent<PlayerHealth>();
        ph?.SetInvulnerable(false);
    }

    // Animation Event: Lock CC and Apply Root Motion
    public void AE_LockCCAndApplyRootMotion()
    {
        isPerformingSkill = true;
        _animator.applyRootMotion = true;
        LockPosition(); // Lock position to prevent snap back
        // Make player invulnerable for the duration of the skill (animation event path)
        var ph = GetComponent<PlayerHealth>();
        ph?.SetInvulnerable(true);
    }

    // Animation Event: Unlock CC and Disable Root Motion
    public void AE_UnlockCCAndDisableRootMotion()
    {
        isPerformingSkill = false;
        _animator.applyRootMotion = false;
        UnlockPosition(); // Unlock position after skill
        // Remove invulnerability when skill animation ends
        var ph = GetComponent<PlayerHealth>();
        ph?.SetInvulnerable(false);
    }

    // Legacy methods for backward compatibility
    public void ApplyRootMotion()
    {
        AE_LockCCAndApplyRootMotion();
    }

    public void DisableRootMotion()
    {
        AE_UnlockCCAndDisableRootMotion();
    }

    // New methods for position management during skills
    public void LockPosition()
    {
        if (!_positionLocked)
        {
            _skillStartPosition = transform.position;
            _positionLocked = true;
        }
    }

    public void UnlockPosition()
    {
        _positionLocked = false;
    }

    public void MaintainPosition()
    {
        if (_positionLocked && _controller != null)
        {
            // Keep character at the locked position during skill
            Vector3 currentPos = transform.position;
            Vector3 targetPos = _skillStartPosition;

            // Only maintain horizontal position, allow vertical movement
            targetPos.y = currentPos.y;

            if (Vector3.Distance(currentPos, targetPos) > 0.1f)
            {
                Vector3 correction = (targetPos - currentPos) * Time.deltaTime * 10f;
                _controller.Move(correction);
            }
        }
    }

    private void Update()
    {
        if (isPerformingSkill && _positionLocked)
        {
            MaintainPosition();
        }
    }
}