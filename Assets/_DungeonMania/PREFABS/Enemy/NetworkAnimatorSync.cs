using UnityEngine;

/// <summary>
/// Animator local — đồng bộ remote qua Fusion sẽ thay thế sau.
/// </summary>
[DefaultExecutionOrder(200)]
public class NetworkAnimatorSync : MonoBehaviour
{
    static readonly int H_Speed = Animator.StringToHash("speed");
    static readonly int H_AttackSpeed = Animator.StringToHash("attackSpeed");
    static readonly int H_IsCrouch = Animator.StringToHash("isCrouch");

    Animator _animator;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>(true);
    }

    public void SendTrigger(string triggerName)
    {
        if (_animator == null) return;
        _animator.SetTrigger(Animator.StringToHash(triggerName));
    }

    public void SendTriggerByHash(int hash)
    {
        if (_animator == null) return;
        _animator.SetTrigger(hash);
    }
}
