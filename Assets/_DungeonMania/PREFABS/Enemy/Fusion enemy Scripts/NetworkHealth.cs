using Fusion;
using UnityEngine;

public class NetworkHealth : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnHPChanged))]
    public int CurrentHP { get; set; }

    public int maxHP = 100;

    // Hàm này tự chạy khi Dummy được đẻ ra trên mạng
    public override void Spawned()
    {
        // HasStateAuthority nghĩa là: "Tôi là Server/Host, tôi có quyền quyết định"
        if (HasStateAuthority)
        {
            CurrentHP = maxHP;
        }
    }

    // Lưỡi kiếm sẽ gọi hàm này khi chém trúng
    public void TakeDamage(int damage)
    {
        // CHỈ SERVER/HOST MỚI CÓ QUYỀN TRỪ MÁU (Chống hack)
        if (HasStateAuthority)
        {
            CurrentHP -= damage;
            if (CurrentHP <= 0)
            {
                CurrentHP = 0;
                Die();
            }
        }
    }

    private void Die()
    {
        Debug.Log("<color=red>Mục tiêu đã bị tiêu diệt!</color>");
        Runner.Despawn(Object);
    }

    // 👉 CÚ PHÁP FUSION 2: Hàm bỏ chữ static và không dùng Changed<T> nữa
    public void OnHPChanged()
    {
        // Bạn có thể lấy thẳng biến CurrentHP hiện tại ra xài luôn!
        Debug.Log($"<color=orange>Quái bị mất máu! Máu còn: {CurrentHP}</color>");
    }
}