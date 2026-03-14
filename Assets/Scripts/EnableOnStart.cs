using UnityEngine;

/// <summary>
/// Gắn script này vào Canvas bị tắt trong Editor.
/// Khi game chạy, nó sẽ tự bật Canvas lên.
/// </summary>
public class EnableOnStart : MonoBehaviour
{
    void Awake()
    {
        gameObject.SetActive(true);
    }
}
