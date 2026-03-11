using UnityEngine;

/// <summary>
/// ScriptableObject chứa dữ liệu của một nhiệm vụ.
/// Tạo từ menu: Assets → Create → Quest → QuestData
/// </summary>
[CreateAssetMenu(fileName = "QuestData", menuName = "Quest/QuestData", order = 0)]
public class QuestData : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    public int questID;             // Phải khớp với số 1..5 trong PlayerPrefs QUEST_N
    public string questTitle;
    [TextArea(2, 4)]
    public string questDescription;

    [Header("Các bước nhiệm vụ")]
    public QuestStep[] steps;       // Các bước tuần tự của nhiệm vụ

    [Header("Phần thưởng")]
    public int rewardGold;
    public int rewardExp;
}

/// <summary>
/// Một bước trong nhiệm vụ (VD: "Gặp Leona" → "Đến cổng dungeon")
/// </summary>
[System.Serializable]
public class QuestStep
{
    public string stepTitle;        // Tiêu đề bước
    [TextArea(1, 3)]
    public string instruction;      // Hướng dẫn hiển thị cho người chơi

    [Header("Objective Marker")]
    public string targetTag;        // Tag của GameObject cần đến/tương tác (VD: "NPC_Leona", "DungeonGate")
    public string targetSceneName;  // Tên scene nếu cần (rỗng nếu cùng scene)
}
