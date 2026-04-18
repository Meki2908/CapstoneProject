using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý phân trang cho TeleportCanvas trong Canvas_MapChinh.
///
/// === SETUP ===
/// 1. Gắn script này lên TeleportCanvas (hoặc panel cha của các page).
/// 2. Kéo từng page panel vào mảng "Pages" (đúng thứ tự 1, 2, 3...).
/// 3. Kéo Btn_Next và Btn_Prev (tuỳ chọn) vào đây.
/// 4. Gán text hiển thị trang nếu muốn (ví dụ: "1 / 3").
///
/// === CẤU TRÚC PAGES GỢI Ý ===
/// Page 0 — Page_Dungeon   (các cổng vào dungeon, DamLay/SaMac/Demon)
/// Page 1 — Page_WorldBosses (nút tele đến Vargr v.v.)
/// </summary>
public class TeleportPageController : MonoBehaviour
{
    [Header("=== Pages ===")]
    [Tooltip("Kéo mỗi panel trang vào đây theo đúng thứ tự (page 0, 1, 2...).")]
    public GameObject[] pages;

    [Header("=== Navigation Buttons ===")]
    [Tooltip("Nút tiến sang trang kế tiếp (Next).")]
    public Button btnNext;
    [Tooltip("Nút quay lại trang trước (Prev). Có thể để trống nếu không dùng.")]
    public Button btnPrev;

    [Header("=== Page Indicator (tuỳ chọn) ===")]
    [Tooltip("Text hiển thị số trang hiện tại, ví dụ: '1 / 2'. Để trống nếu không cần.")]
    public TextMeshProUGUI pageIndicatorText;

    [Header("=== Trang mặc định ===")]
    [Tooltip("Index trang sẽ hiển thị khi mở TeleportCanvas (0 = trang đầu tiên).")]
    [Range(0, 10)]
    public int defaultPageIndex = 0;

    // ── Runtime ──────────────────────────────────────────────────────────────

    private int _currentPage = 0;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (btnNext != null) btnNext.onClick.AddListener(NextPage);
        if (btnPrev != null) btnPrev.onClick.AddListener(PrevPage);
    }

    private void OnEnable()
    {
        // Mỗi lần canvas mở, quay về trang mặc định
        ShowPage(defaultPageIndex);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Chuyển sang trang tiếp theo. Nếu đang ở trang cuối thì wrap về trang đầu.
    /// </summary>
    public void NextPage()
    {
        if (pages == null || pages.Length == 0) return;
        int next = (_currentPage + 1) % pages.Length;
        ShowPage(next);
    }

    /// <summary>
    /// Quay lại trang trước. Nếu đang ở trang đầu thì wrap về trang cuối.
    /// </summary>
    public void PrevPage()
    {
        if (pages == null || pages.Length == 0) return;
        int prev = (_currentPage - 1 + pages.Length) % pages.Length;
        ShowPage(prev);
    }

    /// <summary>
    /// Chuyển đến trang theo index cụ thể.
    /// </summary>
    public void ShowPage(int index)
    {
        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning("[TeleportPageController] Chưa gán pages! Kéo các page panel vào mảng Pages.");
            return;
        }

        index = Mathf.Clamp(index, 0, pages.Length - 1);
        _currentPage = index;

        // Bật trang đang chọn, tắt các trang còn lại
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
                pages[i].SetActive(i == _currentPage);
        }

        UpdateIndicator();
        UpdateNavButtons();

        Debug.Log($"[TeleportPageController] Trang {_currentPage + 1}/{pages.Length}: {(pages[_currentPage] != null ? pages[_currentPage].name : "null")}");
    }

    // ── Private Helpers ────────────────────────────────────────────────────────

    private void UpdateIndicator()
    {
        if (pageIndicatorText == null || pages == null) return;
        pageIndicatorText.text = $"{_currentPage + 1} / {pages.Length}";
    }

    private void UpdateNavButtons()
    {
        if (pages == null || pages.Length <= 1)
        {
            // Chỉ 1 trang: ẩn cả 2 nút
            if (btnNext != null) btnNext.gameObject.SetActive(false);
            if (btnPrev != null) btnPrev.gameObject.SetActive(false);
            return;
        }

        // Hiện nút khi có nhiều hơn 1 trang
        if (btnNext != null) btnNext.gameObject.SetActive(true);
        if (btnPrev != null) btnPrev.gameObject.SetActive(true);
    }

    // ── Debug ─────────────────────────────────────────────────────────────────

    private void OnValidate()
    {
        // Đảm bảo defaultPageIndex không vượt quá số trang
        if (pages != null && pages.Length > 0)
            defaultPageIndex = Mathf.Clamp(defaultPageIndex, 0, pages.Length - 1);
    }
}
