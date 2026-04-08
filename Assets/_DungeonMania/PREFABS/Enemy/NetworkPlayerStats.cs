using UnityEngine;

/// <summary>
/// Giữ API cho <see cref="PlayerHealth"/> — HP sync qua Fusion sẽ thay thế sau.
/// </summary>
[DefaultExecutionOrder(210)]
public class NetworkPlayerStats : MonoBehaviour
{
    public event System.Action<float, float> OnRemoteHealthChanged;

    public void UpdateHP(float currentHP, float maxHP, bool isAlive)
    {
        OnRemoteHealthChanged?.Invoke(currentHP, maxHP);
    }
}
