using UnityEngine;

public static class AnimatorNetworkExtensions
{
    /// <summary>
    /// Đồng bộ Trigger Animation qua mạng (dành cho Fusion 2).
    /// Kích hoạt trigger cục bộ, đồng thời gọi NetworkAnimatorSync để gửi RPC sang server.
    /// </summary>
    public static void SetTriggerNetworked(this Animator animator, string triggerName)
    {
        animator.SetTrigger(triggerName);
        
        // Tìm thủ công ở root (hoặc chính nó)
        var netSync = animator.GetComponentInParent<NetworkAnimatorSync>();
        if (netSync != null)
        {
            netSync.SendTrigger(triggerName);
        }
    }
}
