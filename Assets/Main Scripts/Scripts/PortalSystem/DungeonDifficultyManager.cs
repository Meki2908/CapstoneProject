using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý mở khóa độ khó dungeon.
/// Win Easy → unlock Normal, Win Normal → unlock Hard.
/// 
/// Lưu tiến trình bằng PlayerPrefs.
/// Gắn lên Canvas chọn độ khó dungeon.
/// 
/// Setup:
///   1. Gắn script này vào Canvas/Panel chọn độ khó
///   2. Kéo 3 buttons (Easy, Normal, Hard) vào Inspector
///   3. Đặt dungeonID riêng cho mỗi dungeon (VD: "SaMac", "DamLay")
///   4. Gọi OnDungeonWin(difficulty) khi player thắng dungeon
/// </summary>
public class DungeonDifficultyManager : MonoBehaviour
{
    [Header("=== Dungeon ID ===")]
    [Tooltip("Tên riêng cho dungeon này (để lưu riêng tiến trình)")]
    public string dungeonID = "SaMac";

    [Header("=== Buttons ===")]
    public Button easyButton;
    public Button normalButton;
    public Button hardButton;

    [Header("=== Button State Controllers (optional) ===")]
    [Tooltip("Kéo ButtonStateController trên Normal button")]
    public ButtonStateController normalButtonState;
    [Tooltip("Kéo ButtonStateController trên Hard button")]
    public ButtonStateController hardButtonState;

    // PlayerPrefs key: "Dungeon_{dungeonID}_MaxCleared"
    // 0 = chưa clear gì, 1 = cleared Easy, 2 = cleared Normal, 3 = cleared Hard
    string PrefKey => $"Dungeon_{dungeonID}_MaxCleared";

    public enum Difficulty { Easy = 1, Normal = 2, Hard = 3 }

    void Start()
    {
        RefreshButtons();
    }

    void OnEnable()
    {
        RefreshButtons();
    }

    /// <summary>
    /// Cập nhật trạng thái buttons dựa trên tiến trình đã lưu
    /// </summary>
    public void RefreshButtons()
    {
        int maxCleared = PlayerPrefs.GetInt(PrefKey, 0);

        // Easy: luôn mở
        SetButton(easyButton, null, true);

        // Normal: mở khi đã clear Easy (maxCleared >= 1)
        bool normalUnlocked = maxCleared >= 1;
        SetButton(normalButton, normalButtonState, normalUnlocked);

        // Hard: mở khi đã clear Normal (maxCleared >= 2)
        bool hardUnlocked = maxCleared >= 2;
        SetButton(hardButton, hardButtonState, hardUnlocked);

        Debug.Log($"[DifficultyManager] {dungeonID}: maxCleared={maxCleared}, Normal={normalUnlocked}, Hard={hardUnlocked}");
    }

    void SetButton(Button btn, ButtonStateController state, bool unlocked)
    {
        if (btn == null) return;

        if (state != null)
        {
            if (unlocked) state.SetEnable();
            else state.SetDisable();
        }
        else
        {
            btn.interactable = unlocked;
        }
    }

    /// <summary>
    /// Gọi khi player WIN dungeon ở difficulty nào đó.
    /// Sẽ unlock difficulty tiếp theo.
    /// 
    /// Cách gọi:
    ///   DungeonDifficultyManager.Instance.OnDungeonWin(Difficulty.Easy);
    /// Hoặc gọi từ WaveSpawner khi all waves completed.
    /// </summary>
    public void OnDungeonWin(Difficulty difficulty)
    {
        int cleared = (int)difficulty;
        int current = PlayerPrefs.GetInt(PrefKey, 0);

        if (cleared > current)
        {
            PlayerPrefs.SetInt(PrefKey, cleared);
            PlayerPrefs.Save();
            Debug.Log($"[DifficultyManager] {dungeonID}: Cleared {difficulty}! MaxCleared → {cleared}");
        }

        RefreshButtons();
    }

    /// <summary>
    /// Shortcut: gọi từ Button OnClick hoặc WaveSpawner
    /// </summary>
    public void OnWinEasy()   => OnDungeonWin(Difficulty.Easy);
    public void OnWinNormal() => OnDungeonWin(Difficulty.Normal);
    public void OnWinHard()   => OnDungeonWin(Difficulty.Hard);

    /// <summary>
    /// Reset tiến trình (debug)
    /// </summary>
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(PrefKey);
        PlayerPrefs.Save();
        RefreshButtons();
        Debug.Log($"[DifficultyManager] {dungeonID}: Progress reset!");
    }

    // Singleton (optional — nếu muốn gọi từ WaveSpawner)
    public static DungeonDifficultyManager Instance { get; private set; }
    void Awake()
    {
        if (Instance == null) Instance = this;
    }
}
