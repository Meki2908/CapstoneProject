using UnityEngine;

/// <summary>
/// ScriptableObject chứa dữ liệu của một nhiệm vụ.
/// Tạo từ menu: Assets → Create → Quest → QuestData
/// </summary>
[CreateAssetMenu(fileName = "QuestData", menuName = "Quest/QuestData", order = 0)]
public class QuestData : ScriptableObject
{
    [Header("Basic Information")]
    public int questID;             // Must match Quest ID 1..5
    public string questTitle;
    [TextArea(2, 4)]
    public string questDescription;

    [Header("Quest Steps")]
    public QuestStep[] steps;       // Sequential steps for the quest

    [Header("Rewards")]
    public int rewardGold;
    public int rewardExp;
}

/// <summary>
/// Một bước trong nhiệm vụ (VD: "Gặp Leona" → "Đến cổng dungeon")
/// </summary>
[System.Serializable]
public class QuestStep
{
    public string stepTitle;        // Step Title
    [TextArea(1, 3)]
    public string instruction;      // Instruction shown to player

    [Header("Objective Marker")]
    public string targetTag;        // Target GameObject tag (e.g., "NPC_Leona")
    public string targetSceneName;  // Scene name (empty if same scene)
}
