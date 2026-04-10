using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý mở khóa độ khó dungeon.
/// Bấm Easy → unlock Normal, bấm Normal → unlock Hard.
/// Lưu bằng PlayerPrefs.
/// 
/// Setup:
///   1. Gắn script lên Panel chọn độ khó
///   2. Kéo 3 buttons vào Inspector
///   3. Đặt dungeonID riêng cho mỗi dungeon
/// </summary>
public class DungeonDifficultyManager : MonoBehaviour
{
    [Header("=== Dungeon ID ===")]
    public string dungeonID = "SaMac";

    [Header("=== Buttons ===")]
    public Button easyButton;
    public Button normalButton;
    public Button hardButton;

    // PlayerPrefs: 0=chưa mở, 1=mở Normal, 2=mở Hard
    string PrefKey => $"Dungeon_{dungeonID}_MaxCleared";

    public enum Difficulty { Easy = 1, Normal = 2, Hard = 3 }

    public static DungeonDifficultyManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // Gắn sự kiện onClick
        if (easyButton != null)
            easyButton.onClick.AddListener(OnClickEasy);
        if (normalButton != null)
            normalButton.onClick.AddListener(OnClickNormal);

        RefreshButtons();
    }

    void OnEnable()
    {
        StartCoroutine(RefreshNextFrame());
    }

    System.Collections.IEnumerator RefreshNextFrame()
    {
        yield return null;
        RefreshButtons();
    }

    void OnClickEasy()
    {
        // Bấm Easy → unlock Normal
        Unlock(1);
    }

    void OnClickNormal()
    {
        // Bấm Normal → unlock Hard
        Unlock(2);
    }

    void Unlock(int level)
    {
        int current = PlayerPrefs.GetInt(PrefKey, 0);
        if (level > current)
        {
            PlayerPrefs.SetInt(PrefKey, level);
            PlayerPrefs.Save();
            Debug.Log($"[Difficulty] {dungeonID}: Unlocked level {level}");
        }
        RefreshButtons();
    }

    public void RefreshButtons()
    {
        int maxCleared = PlayerPrefs.GetInt(PrefKey, 0);

        // Easy: luôn mở
        SetButtonState(easyButton, true);

        // Normal: mở khi maxCleared >= 1
        SetButtonState(normalButton, maxCleared >= 1);

        // Hard: mở khi maxCleared >= 2
        SetButtonState(hardButton, maxCleared >= 2);

        Debug.Log($"[Difficulty] {dungeonID}: maxCleared={maxCleared}, Normal={maxCleared >= 1}, Hard={maxCleared >= 2}");
    }

    void SetButtonState(Button btn, bool unlocked)
    {
        if (btn == null) return;

        btn.interactable = unlocked;

        // Nếu có ButtonStateController → dùng nó để hiện/ẩn text đúng cách
        var bsc = btn.GetComponent<ButtonStateController>();
        if (bsc != null)
        {
            if (unlocked) bsc.SetEnable();
            else bsc.SetDisable();
        }
    }

    /// <summary>
    /// Gọi từ WaveSpawner khi win (nếu cần)
    /// </summary>
    public void OnDungeonWin(Difficulty difficulty)
    {
        Unlock((int)difficulty);
    }

    /// <summary>
    /// Reset tiến trình (debug)
    /// </summary>
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(PrefKey);
        PlayerPrefs.Save();
        RefreshButtons();
        Debug.Log($"[Difficulty] {dungeonID}: Reset!");
    }
}
