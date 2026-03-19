using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Quản lý chuyển scene dùng chung cho tất cả scene.
/// Lần đầu: tìm Panel_GUILoading gốc trong scene → reparent vào manager (DontDestroyOnLoad).
/// Panel tồn tại vĩnh viễn qua mọi scene → không cần tìm lại, không flash.
/// Nếu không tìm thấy panel gốc → tự tạo fallback bằng code.
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
        Debug.Log("[SceneTransition] Tạo Instance mới (không có player prefab)");
        return Instance;
    }

    [Header("=== Scene Names ===")]
    [SerializeField] private string mainMapScene = "Map_Chinh";
    [SerializeField] private string dungeonSaMacScene = "MapSaMac";
    [SerializeField] private string uiGameScene = "UI_Game";

    [Header("=== Settings ===")]
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private bool restoreCursorAfterLoad = false;

    [Header("=== Loading Panel (kéo thả từ scene) ===")]
    [Tooltip("Panel loading — nếu để trống sẽ tự tìm Panel_GUILoading")]
    [SerializeField] private GameObject loadingPanel;
    [Tooltip("Slider progress bar")]
    [SerializeField] private Slider loadingBar;
    [Tooltip("Text hiển thị tip ngẫu nhiên")]
    [SerializeField] private TextMeshProUGUI tipText;

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



    // Internal references
    private CanvasGroup fadeCanvasGroup;
    private bool panelAdopted = false;

    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // CRITICAL: Tách khỏi player prefab hierarchy để DontDestroyOnLoad hoạt động
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            // CHỈ xóa component thừa — KHÔNG xóa gameObject (player prefab)!
            Destroy(this);
            return;
        }
    }

    private void Start()
    {
        if (!panelAdopted)
        {
            AdoptOriginalPanel();
        }
    }

    // ===== ADOPT PANEL GỐC =====

    /// <summary>
    /// Tìm Panel_GUILoading gốc trong scene → reparent vào manager → DontDestroyOnLoad.
    /// Chỉ chạy 1 lần. Nếu không tìm thấy → tạo fallback bằng code.
    /// </summary>
    private void AdoptOriginalPanel()
    {
        // Nếu user đã kéo thả panel vào Inspector → dùng luôn, không cần tìm
        if (loadingPanel != null)
        {
            SetupAdoptedPanel(loadingPanel);
            return;
        }

        string[] panelNames = { "Panel_GUILoading", "Panel_Loading", "LoadingPanel" };

        GameObject foundPanel = null;

        // Cách 1: Tìm trong Canvas_Menu (ưu tiên — panel thường nằm ở đây)
        GameObject canvasMenu = GameObject.Find("Canvas_Menu");
        if (canvasMenu != null)
        {
            foreach (string name in panelNames)
            {
                Transform t = FindDeep(canvasMenu.transform, name);
                if (t != null)
                {
                    foundPanel = t.gameObject;
                    Debug.Log($"[SceneTransition] Found original panel '{name}' in Canvas_Menu");
                    break;
                }
            }
        }

        // Cách 2: Tìm toàn scene (active)
        if (foundPanel == null)
        {
            foreach (string name in panelNames)
            {
                GameObject go = GameObject.Find(name);
                if (go != null)
                {
                    foundPanel = go;
                    Debug.Log($"[SceneTransition] Found original panel '{name}' in scene");
                    break;
                }
            }
        }

        // Cách 3: Tìm cả inactive objects
        if (foundPanel == null)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (string name in panelNames)
                {
                    Transform t = FindDeep(root.transform, name);
                    if (t != null)
                    {
                        foundPanel = t.gameObject;
                        Debug.Log($"[SceneTransition] Found original panel '{name}' (inactive) in {root.name}");
                        break;
                    }
                }
                if (foundPanel != null) break;
            }
        }

        if (foundPanel != null)
        {
            // === ADOPT: Reparent panel trực tiếp vào manager ===
            // Không tạo Canvas mới — thêm Canvas component lên chính panel
            foundPanel.transform.SetParent(transform, false);

            // Thêm Canvas lên panel (để render độc lập, không cần Canvas cha)
            Canvas panelCanvas = foundPanel.GetComponent<Canvas>();
            if (panelCanvas == null)
                panelCanvas = foundPanel.AddComponent<Canvas>();
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 999;

            // Thêm CanvasScaler + GraphicRaycaster nếu chưa có
            if (foundPanel.GetComponent<CanvasScaler>() == null)
            {
                var scaler = foundPanel.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            if (foundPanel.GetComponent<GraphicRaycaster>() == null)
                foundPanel.AddComponent<GraphicRaycaster>();

            // Đảm bảo RectTransform stretch toàn màn hình
            RectTransform rt = foundPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            loadingPanel = foundPanel;

            // Lấy components (chỉ tìm nếu chưa set từ Inspector)
            if (loadingBar == null)
                loadingBar = loadingPanel.GetComponentInChildren<Slider>(true);

            if (tipText == null)
            {
                Transform textT = FindDeep(loadingPanel.transform, "Text_Loading");
                if (textT != null)
                    tipText = textT.GetComponent<TextMeshProUGUI>();
                if (tipText == null)
                    tipText = loadingPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            fadeCanvasGroup = loadingPanel.GetComponent<CanvasGroup>();
            if (fadeCanvasGroup == null)
                fadeCanvasGroup = loadingPanel.AddComponent<CanvasGroup>();

            loadingPanel.SetActive(false);
            panelAdopted = true;

            Debug.Log($"[SceneTransition] ✅ Adopted panel '{foundPanel.name}' → DontDestroyOnLoad (không tạo Canvas mới)");
        }
        else
        {
            // === FALLBACK: Tạo panel bằng code ===
            Debug.LogWarning("[SceneTransition] Panel gốc không tìm thấy → tạo fallback panel bằng code");
            CreateFallbackPanel();
            panelAdopted = true;
        }
    }

    // ===== FALLBACK PANEL (code-generated) =====

    private void CreateFallbackPanel()
    {
        GameObject canvasGO = new GameObject("LoadingCanvas_Fallback");
        canvasGO.transform.SetParent(transform);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Panel background đen
        loadingPanel = new GameObject("Panel_Loading_Fallback");
        loadingPanel.transform.SetParent(canvasGO.transform, false);

        var panelRect = loadingPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var bgImage = loadingPanel.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 1f);

        fadeCanvasGroup = loadingPanel.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;

        // Slider
        CreateFallbackSlider(loadingPanel.transform);

        // Text
        CreateFallbackText(loadingPanel.transform);

        loadingPanel.SetActive(false);
    }

    private void CreateFallbackSlider(Transform parent)
    {
        GameObject sliderGO = new GameObject("Slider_Loading");
        sliderGO.transform.SetParent(parent, false);

        var sliderRect = sliderGO.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.2f, 0.4f);
        sliderRect.anchorMax = new Vector2(0.8f, 0.45f);
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        loadingBar = sliderGO.AddComponent<Slider>();
        loadingBar.minValue = 0f;
        loadingBar.maxValue = 1f;
        loadingBar.value = 0f;

        // Background
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(sliderGO.transform, false);
        var bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgGO.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        // Fill
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillArea.transform, false);
        var fillRect = fillGO.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImage = fillGO.AddComponent<Image>();
        fillImage.color = new Color(0.9f, 0.7f, 0.2f, 1f);

        loadingBar.fillRect = fillRect;
        loadingBar.targetGraphic = fillImage;
        loadingBar.interactable = false;
        loadingBar.handleRect = null;
    }

    private void CreateFallbackText(Transform parent)
    {
        GameObject textGO = new GameObject("Text_Loading");
        textGO.transform.SetParent(parent, false);

        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.2f, 0.46f);
        textRect.anchorMax = new Vector2(0.8f, 0.56f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        tipText = textGO.AddComponent<TextMeshProUGUI>();
        tipText.text = "Loading...";
        tipText.fontSize = 24;
        tipText.color = Color.white;
        tipText.alignment = TextAlignmentOptions.Center;
    }

    /// <summary>
    /// Setup panel đã được kéo vào Inspector: reparent + lấy components (không tạo Canvas mới)
    /// </summary>
    private void SetupAdoptedPanel(GameObject panel)
    {
        // Reparent trực tiếp vào manager
        panel.transform.SetParent(transform, false);

        // Thêm Canvas lên chính panel
        Canvas panelCanvas = panel.GetComponent<Canvas>();
        if (panelCanvas == null)
            panelCanvas = panel.AddComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = 999;

        if (panel.GetComponent<CanvasScaler>() == null)
        {
            var scaler = panel.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }
        if (panel.GetComponent<GraphicRaycaster>() == null)
            panel.AddComponent<GraphicRaycaster>();

        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        if (loadingBar == null)
            loadingBar = panel.GetComponentInChildren<Slider>(true);
        if (tipText == null)
            tipText = panel.GetComponentInChildren<TextMeshProUGUI>(true);

        fadeCanvasGroup = panel.GetComponent<CanvasGroup>();
        if (fadeCanvasGroup == null)
            fadeCanvasGroup = panel.AddComponent<CanvasGroup>();

        panel.SetActive(false);
        panelAdopted = true;

        Debug.Log($"[SceneTransition] ✅ Setup panel '{panel.name}' from Inspector (không tạo Canvas mới)");
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

        // Đảm bảo panel đã được adopt (trường hợp Start() chưa chạy)
        if (!panelAdopted) AdoptOriginalPanel();

        StartCoroutine(TransitionRoutine(sceneName, message ?? "Loading..."));
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

    private IEnumerator TransitionRoutine(string sceneName, string message)
    {
        isTransitioning = true;

        // Đảm bảo time scale = 1 (có thể bị pause)
        Time.timeScale = 1f;

        // Chọn tip ngẫu nhiên thay vì dùng message mặc định
        string tip = GetRandomTip();

        // Hiện loading panel với tip
        ShowLoadingPanel(tip);
        UpdateProgress(0f, tip);

        // Fade in loading screen
        yield return StartCoroutine(FadeIn());

        // Bắt đầu load scene async
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad == null)
        {
            Debug.LogError($"[SceneTransition] Không thể load scene: {sceneName}. Kiểm tra Build Settings!");
            isTransitioning = false;
            HideLoadingPanel();
            yield break;
        }
        asyncLoad.allowSceneActivation = false;

        // Slider chạy theo dữ liệu thật từ Unity AsyncOperation
        // asyncLoad.progress: 0 → 0.9 khi allowSceneActivation=false (Unity giới hạn)
        // Scale: progress / 0.9 = 0% → 100% thật

        while (asyncLoad.progress < 0.9f)
        {
            // Dữ liệu thật 100% — không lerp, không timer giả
            float realProgress = asyncLoad.progress / 0.9f;
            UpdateProgress(realProgress, tip);
            yield return null;
        }

        // Scene data đã load xong → hiển thị 100%
        UpdateProgress(1f, tip);

        // Giữ hiển thị 100% 0.3s để user thấy rõ đã xong
        yield return new WaitForSecondsRealtime(0.3f);

        // Kích hoạt scene mới
        asyncLoad.allowSceneActivation = true;

        // Chờ scene thực sự activate xong
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Chờ 2 frame để scene mới setup (Awake/Start chạy)
        yield return null;
        yield return null;

        // Panel đã DontDestroyOnLoad → vẫn còn nguyên → FadeOut trực tiếp
        Debug.Log($"[SceneTransition] Scene '{sceneName}' loaded → FadeOut...");

        yield return StartCoroutine(FadeOut());

        HideLoadingPanel();

        if (restoreCursorAfterLoad)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        isTransitioning = false;
        Debug.Log($"[SceneTransition] Chuyển scene hoàn tất: {sceneName}");
    }

    // ===== LOADING PANEL =====

    private void ShowLoadingPanel(string message)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = 0f;
        }
        if (tipText != null)
            tipText.text = message;
    }

    private void HideLoadingPanel()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    private void UpdateProgress(float progress, string tip = "Loading...")
    {
        if (loadingBar != null)
            loadingBar.value = progress;
        if (tipText != null)
            tipText.text = tip;
    }

    // ===== FADE =====

    private IEnumerator FadeIn()
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.alpha = 0f;
        while (fadeCanvasGroup.alpha < 1f)
        {
            fadeCanvasGroup.alpha += Time.unscaledDeltaTime * fadeSpeed;
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.alpha = 1f;
        while (fadeCanvasGroup.alpha > 0f)
        {
            fadeCanvasGroup.alpha -= Time.unscaledDeltaTime * fadeSpeed;
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
    }

    // ===== CLEANUP =====

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
