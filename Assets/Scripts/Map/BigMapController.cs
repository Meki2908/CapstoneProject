using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Mirror của QuestJournalUI — áp dụng cho Big Map.
/// Canvas luôn bật. Chỉ rootPanel (panel con) bị ẩn/hiện.
///
/// SETUP (giống QuestJournalUI):
///   1. Canvas BigMap → LUÔN BẬT trong Hierarchy
///   2. Gắn script này lên Canvas (hoặc bất kỳ GameObject nào luôn active)
///   3. rootPanel = Panel con bên trong Canvas (kéo vào Inspector)
///   4. Ấn M trong Game → bật/tắt rootPanel + cursor
/// </summary>
public class BigMapController : MonoBehaviour
{
    [Header("Phím toggle")]
    public KeyCode toggleKey = KeyCode.M;

    [Header("Panel bên trong Canvas")]
    public GameObject rootPanel;   // Kéo Panel con vào đây — giống rootPanel của QuestJournalUI

    [Header("UI ẩn khi map mở (tuỳ chọn)")]
    public GameObject[] hideWhenOpen;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("[BigMap] rootPanel CHƯA ĐƯỢC GÁN! Kéo Panel con vào Inspector slot 'Root Panel'.");
        }
    }

    void Update()
    {
        if (IsTogglePressedThisFrame())
            ToggleMap();
    }

    // ─── Toggle (giống ToggleJournal) ────────────────────────────────────────

    public void ToggleMap()
    {
        if (rootPanel == null) return;

        bool isOpen = !rootPanel.activeSelf;
        rootPanel.SetActive(isOpen);

        if (isOpen)
        {
            SetOtherUI(false);
            Cursor.visible   = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            SetOtherUI(true);
            Cursor.visible   = false;
            Cursor.lockState = CursorLockMode.Locked;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void OpenMap()
    {
        if (rootPanel == null) return;
        rootPanel.SetActive(true);
        SetOtherUI(false);
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseMap()
    {
        if (rootPanel == null) return;
        rootPanel.SetActive(false);
        SetOtherUI(true);
        Cursor.visible   = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public bool IsOpen => rootPanel != null && rootPanel.activeSelf;

    // ─── Helpers ──────────────────────────────────────────────────────────────

    void SetOtherUI(bool active)
    {
        if (hideWhenOpen == null) return;
        foreach (var ui in hideWhenOpen)
            if (ui != null) ui.SetActive(active);
    }

    bool IsTogglePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.mKey.wasPressedThisFrame)
            return true;
#endif
        return Input.GetKeyDown(toggleKey);
    }
}
