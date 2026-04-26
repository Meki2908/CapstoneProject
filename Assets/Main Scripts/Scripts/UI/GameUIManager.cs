using UnityEngine;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Health UI")]
    [Tooltip("Text dùng để hiển thị HP hiện tại và HP tối đa. Kéo thả từ Canvas chung vào đây.")]
    [SerializeField] private TextMeshProUGUI healthText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Nếu bạn đặt UI này nằm ngoài root scene, có thể cần DontDestroyOnLoad tùy ý đồ thiết kế.
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Hàm này được gọi bởi PlayerHealth khi phát hiện lượng HP bị thay đổi.
    /// CHÚ Ý: Chỉ gọi nếu Player đó có HasInputAuthority.
    /// </summary>
    public void UpdateHP(float currentHP, float maxHP)
    {
        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHP)}/{Mathf.CeilToInt(maxHP)}";
        }
        else
        {
            Debug.LogWarning("[GameUIManager] Cập nhật máu thất bại vì chưa gán healthText vào Inspector!");
        }
    }

    // Tương lai bạn có thể thêm các hàm public void UpdateInventory(Item item), UpdateMana(), v.v. tại đây.
}
