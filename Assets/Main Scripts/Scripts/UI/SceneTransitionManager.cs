using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Quản lý chuyển scene dùng chung cho tất cả scene.
/// CHỈ bật/tắt Panel_GUILoading trong Canvas_Menu gốc.
/// Không reparent, không tạo object mới, không di chuyển panel.
/// Mỗi scene mới → tìm lại panel trong scene đó.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    /// <summary>
    /// Đảm bảo Instance tồn tại — tự tạo nếu chưa có (dùng cho UI_Game không có player)
    /// </summary>
    public static SceneTransitionManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        var go = new GameObject("[SceneTransitionManager]");
        Instance = go.AddComponent<SceneTransitionManager>();
        Debug.Log("[SceneTransition] Tạo Instance mới");
        return Instance;
    }

    [Header("=== Scene Names ===")]
    [SerializeField] private string mainMapScene = "Map_Chinh";
    [SerializeField] private string dungeonSaMacScene = "MapSaMac";
    [SerializeField] private string uiGameScene = "UI_Game";

    [Header("=== Settings ===")]
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private bool restoreCursorAfterLoad = false;

    // Random loading tips — displayed randomly during scene transitions
    private static readonly string[] loadingTips = new string[]
    {
        // Gameplay hints
        "Tip: Press Q when your Ultimate is ready to unleash a devastating attack!",
        "Tip: Dash at the right moment to gain invincibility frames — dodge any damage!",
        "Tip: Each weapon type has its own skill set. Try Sword, Axe, and Mage!",
        "Tip: Gems socketed into weapons boost your power. Rarer gems = stronger effects!",
        "Tip: Sprint + Jump lets you leap higher than a normal jump!",
        "Tip: Crouch to avoid some enemy attacks.",
        "Tip: Sheathe your weapon when not fighting to move faster.",

        // Combat & weapon
        "Thunder Sword — fast attacks, chain combos, ultimate summons lightning!",
        "War Axe — heavy damage, slow but every hit counts!",
        "Mage Staff — ranged attacks, ultimate summons a firestorm!",
        "Tip: Each weapon has 4 skills: E, R, F and Q Ultimate. Master them all!",
        "Tip: Skill cooldowns are shown on icons. Manage your timing to win!",
        "Tip: Better equipment increases your movement speed.",

        // Dungeon tips
        "Desert Dungeon — where only the bravest warriors dare to enter!",
        "Tip: Each dungeon wave is stronger than the last. Be prepared!",
        "Tip: The final dungeon boss can drop Legendary items!",
        "Tip: Defeat enemies in dungeons to earn EXP and rare loot.",
        "Tip: If you fail a dungeon, you can retry or return to the map.",
        "Beware of the Lich — they wield dangerous dark magic!",
        "Stone Ogre has thick armor — find its weakness!",
        "Ancient Golem — slow but one hit can take you down!",
        "Minotaur — guardian of the labyrinth. Dodge its charge attack!",
        "Ifrit — lord of fire. Don't get surrounded!",

        // Lore & atmosphere
        "Where am I? I have to find a way out...",
        "The darkness is spreading... The light is fading...",
        "Legends tell of warriors who once conquered these lands...",
        "This desert hides secrets from an ancient era...",
        "The clash of swords echoes through the dark dungeon...",
        "Every enemy defeated brings valuable experience.",
        "The road ahead is full of danger. Be ready to fight!",
        "The strongest warriors never give up...",
        "Fire and ice, blade and spell — which weapon is your destiny?",
        "Explore every corner — treasure hides where you least expect it!",
    };

    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Tách khỏi player prefab hierarchy để DontDestroyOnLoad hoạt động
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }
    }

    // ===== TÌM PANEL TRONG SCENE HIỆN TẠI =====

    /// <summary>
    /// Tìm Panel_GUILoading trong scene hiện tại (không di chuyển, không thay đổi gì).
    /// Trả về null nếu không tìm thấy.
    /// </summary>
    private GameObject FindLoadingPanel()
    {
        string[] panelNames = { "Panel_GUILoading", "Panel_Loading", "LoadingPanel" };

        // Cách 1: Tìm trong Canvas_Menu (ưu tiên)
        GameObject canvasMenu = GameObject.Find("Canvas_Menu");
        if (canvasMenu != null)
        {
            foreach (string name in panelNames)
            {
                Transform t = FindDeep(canvasMenu.transform, name);
                if (t != null) return t.gameObject;
            }
        }

        // Cách 2: Tìm toàn scene (active)
        foreach (string name in panelNames)
        {
            GameObject go = GameObject.Find(name);
            if (go != null) return go;
        }

        // Cách 3: Tìm cả inactive objects
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (string name in panelNames)
            {
                Transform t = FindDeep(root.transform, name);
                if (t != null) return t.gameObject;
            }
        }

        return null;
    }

    /// <summary>
    /// Lấy components UI từ panel (Slider, Text, CanvasGroup) — không thêm component mới.
    /// </summary>
    private void GetPanelComponents(GameObject panel, out Slider slider, out TextMeshProUGUI text, out CanvasGroup canvasGroup)
    {
        slider = panel.GetComponentInChildren<Slider>(true);

        // Ưu tiên tìm Text_Loading
        Transform textT = FindDeep(panel.transform, "Text_Loading");
        text = textT != null ? textT.GetComponent<TextMeshProUGUI>() : null;
        if (text == null)
            text = panel.GetComponentInChildren<TextMeshProUGUI>(true);

        canvasGroup = panel.GetComponent<CanvasGroup>();
    }

    // ===== HELPER =====

    private Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // ===== PUBLIC API =====

    public void GoToScene(string sceneName, string message = null)
    {
        if (isTransitioning)
        {
            Debug.LogWarning($"[SceneTransition] Đang chuyển scene rồi, bỏ qua yêu cầu: {sceneName}");
            return;
        }

        StartCoroutine(TransitionRoutine(sceneName));
    }

    public void GoToMainMap(string message = "Đang quay về bản đồ...")
    {
        GoToScene(mainMapScene, message);
    }

    public void GoToDungeonSaMac(string message = "Đang vào dungeon...")
    {
        GoToScene(dungeonSaMacScene, message);
    }

    public void GoToUIGame(string message = "Đang tải menu...")
    {
        GoToScene(uiGameScene, message);
    }

    public bool IsTransitioning => isTransitioning;

    // ===== TRANSITION LOGIC =====

    private string GetRandomTip()
    {
        return loadingTips[Random.Range(0, loadingTips.Length)];
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        isTransitioning = true;

        // Đảm bảo time scale = 1 (có thể bị pause)
        Time.timeScale = 1f;

        string tip = GetRandomTip();

        // === BƯỚC 1: Tìm và bật panel NGAY LẬP TỨC trong scene HIỆN TẠI ===
        GameObject panel = FindLoadingPanel();
        Slider slider = null;
        TextMeshProUGUI text = null;
        CanvasGroup cg = null;

        if (panel != null)
        {
            GetPanelComponents(panel, out slider, out text, out cg);

            // Hiện panel NGAY (alpha=1) — không fade in để tránh bị đơ che mất
            if (cg != null) cg.alpha = 1f;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling(); // Luôn render trên cùng

            // Set UI ban đầu
            if (slider != null) slider.value = 0f;
            if (text != null) text.text = tip;

            // Chờ vài frame để Unity render panel lên màn hình trước khi load
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();
        }

        // === BƯỚC 2: Load scene async ===
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad == null)
        {
            Debug.LogError($"[SceneTransition] Không thể load scene: {sceneName}. Kiểm tra Build Settings!");
            isTransitioning = false;
            if (panel != null) panel.SetActive(false);
            yield break;
        }
        asyncLoad.allowSceneActivation = false;

        // Progress bar chạy theo dữ liệu thật
        while (asyncLoad.progress < 0.9f)
        {
            float realProgress = asyncLoad.progress / 0.9f;
            if (slider != null) slider.value = realProgress;
            yield return null;
        }

        // Hiển thị 100%
        if (slider != null) slider.value = 1f;
        yield return new WaitForSecondsRealtime(0.3f);

        // === BƯỚC 3: Kích hoạt scene mới (scene cũ bị destroy) ===
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Chờ 2 frame để scene mới setup (Awake/Start)
        yield return null;
        yield return null;

        // === BƯỚC 4: Tìm panel trong scene MỚI và fade out ===
        panel = FindLoadingPanel();
        if (panel != null)
        {
            GetPanelComponents(panel, out slider, out text, out cg);

            // Bật panel lên để fade out
            panel.SetActive(true);
            panel.transform.SetAsLastSibling(); // Luôn render trên cùng
            if (cg != null) cg.alpha = 1f;

            Debug.Log($"[SceneTransition] Scene '{sceneName}' loaded → FadeOut...");

            // Fade out
            if (cg != null)
            {
                while (cg.alpha > 0f)
                {
                    cg.alpha -= Time.unscaledDeltaTime * fadeSpeed;
                    yield return null;
                }
                cg.alpha = 0f;
            }

            // Tắt panel
            panel.SetActive(false);
        }

        if (restoreCursorAfterLoad)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        isTransitioning = false;
        Debug.Log($"[SceneTransition] Chuyển scene hoàn tất: {sceneName}");
    }

    // ===== CLEANUP =====

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
