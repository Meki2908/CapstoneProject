using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Quản lý chuyển scene dùng chung cho tất cả scene
/// Thay thế LoadingScreenManager — dùng Panel_GUILoading trong Canvas_Menu (player prefab)
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("=== Scene Names ===")]
    [SerializeField] private string mainMapScene = "Map_Chinh";
    [SerializeField] private string dungeonSaMacScene = "MapSaMac";
    [SerializeField] private string uiGameScene = "UI_Game";

    [Header("=== Loading Panel (tự tìm nếu null) ===")]
    [Tooltip("Kéo Panel_GUILoading vào đây hoặc để trống — tự tìm")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI loadingText;

    [Header("=== Settings ===")]
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private string defaultLoadingMessage = "Loading...";
    [SerializeField] private bool restoreCursorAfterLoad = false; // Không force cursor

    private CanvasGroup fadeCanvasGroup;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // CRITICAL: Tách khỏi player prefab hierarchy để DontDestroyOnLoad hoạt động
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // Đăng ký event khi scene mới load xong — tìm lại loading panel
            SceneManager.sceneLoaded += OnSceneLoaded;
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
        FindLoadingPanel();
    }

    // ===== PUBLIC API =====

    /// <summary>
    /// Chuyển đến scene bất kỳ có loading screen
    /// </summary>
    public void GoToScene(string sceneName, string message = null)
    {
        if (isTransitioning)
        {
            Debug.LogWarning($"[SceneTransition] Đang chuyển scene rồi, bỏ qua yêu cầu: {sceneName}");
            return;
        }
        StartCoroutine(TransitionRoutine(sceneName, message ?? defaultLoadingMessage));
    }

    /// <summary>
    /// Về Map Chính
    /// </summary>
    public void GoToMainMap(string message = "Đang quay về bản đồ...")
    {
        GoToScene(mainMapScene, message);
    }

    /// <summary>
    /// Vào Dungeon Sa Mạc
    /// </summary>
    public void GoToDungeonSaMac(string message = "Đang vào dungeon...")
    {
        GoToScene(dungeonSaMacScene, message);
    }

    /// <summary>
    /// Về UI Game (Menu)
    /// </summary>
    public void GoToUIGame(string message = "Đang tải menu...")
    {
        GoToScene(uiGameScene, message);
    }

    /// <summary>
    /// Kiểm tra đang chuyển scene không
    /// </summary>
    public bool IsTransitioning => isTransitioning;

    // ===== TRANSITION LOGIC =====

    private IEnumerator TransitionRoutine(string sceneName, string message)
    {
        isTransitioning = true;

        // FIX #1: Đảm bảo time scale = 1 (có thể bị pause)
        Time.timeScale = 1f;

        // FIX #2: Force tìm lại panel (có thể bị null sau scene load)
        ForceRefreshLoadingPanel();

        // Hiện loading panel
        ShowLoadingPanel(message);
        UpdateProgress(0f, message);

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

        // Cập nhật progress THẬT từ AsyncOperation
        while (!asyncLoad.isDone)
        {
            // asyncLoad.progress chạy từ 0 → 0.9 (0.9 = load xong, chờ activate)
            float realProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            UpdateProgress(realProgress, message);

            // Khi load xong (progress >= 0.9) → cho activate
            if (asyncLoad.progress >= 0.9f)
            {
                UpdateProgress(1f, message);
                yield return new WaitForSecondsRealtime(0.3f);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        // Chờ 2 frame để scene mới setup xong
        yield return null;
        yield return null;

        // FIX #3: Tìm lại loading panel trong scene mới
        ForceRefreshLoadingPanel();

        // FIX #18: Phải Show panel mới (alpha=1) trước khi FadeOut
        // Panel cũ đã bị destroy khi scene load, panel mới mặc định ẩn
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = 1f; // Che toàn bộ scene mới
        }

        // Fade out loading screen
        yield return StartCoroutine(FadeOut());

        // Ẩn loading panel
        HideLoadingPanel();

        // FIX #4: Chỉ set cursor nếu cần
        if (restoreCursorAfterLoad)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        isTransitioning = false;
    }

    // ===== LOADING PANEL =====

    /// <summary>
    /// FIX #2: Force tìm lại panel (xóa cache cũ nếu bị destroy)
    /// </summary>
    private void ForceRefreshLoadingPanel()
    {
        // Kiểm tra panel cũ còn tồn tại không (bị destroy khi scene load)
        if (loadingPanel == null || loadingPanel.Equals(null))
        {
            loadingPanel = null;
            loadingBar = null;
            loadingText = null;
            fadeCanvasGroup = null;
        }
        FindLoadingPanel();
    }

    private void FindLoadingPanel()
    {
        if (loadingPanel != null) return;

        // FIX #5: Tìm cả inactive objects bằng FindObjectsOfType
        // Panel_GUILoading mặc định bị ẩn → GameObject.Find không tìm được
        string[] panelNames = { "Panel_GUILoading", "Panel_Loading", "LoadingPanel" };

        // Cách 1: Tìm trong Canvas_Menu (ưu tiên)
        GameObject canvasMenu = GameObject.Find("Canvas_Menu");
        if (canvasMenu != null)
        {
            foreach (string name in panelNames)
            {
                Transform t = FindDeep(canvasMenu.transform, name);
                if (t != null)
                {
                    loadingPanel = t.gameObject;
                    Debug.Log($"[SceneTransition] Found {name} in Canvas_Menu");
                    break;
                }
            }
        }

        // Cách 2: Tìm toàn scene (active objects)
        if (loadingPanel == null)
        {
            foreach (string name in panelNames)
            {
                GameObject go = GameObject.Find(name);
                if (go != null)
                {
                    loadingPanel = go;
                    Debug.Log($"[SceneTransition] Found {name} in scene");
                    break;
                }
            }
        }

        // Cách 3: Tìm inactive objects trong tất cả root objects
        if (loadingPanel == null)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (string name in panelNames)
                {
                    Transform t = FindDeep(root.transform, name);
                    if (t != null)
                    {
                        loadingPanel = t.gameObject;
                        Debug.Log($"[SceneTransition] Found {name} (inactive) in {root.name}");
                        break;
                    }
                }
                if (loadingPanel != null) break;
            }
        }

        // Setup components trong panel
        if (loadingPanel != null)
        {
            if (loadingBar == null)
                loadingBar = loadingPanel.GetComponentInChildren<Slider>(true);

            if (loadingText == null)
            {
                Transform textT = FindDeep(loadingPanel.transform, "Text_Loading");
                if (textT != null)
                    loadingText = textT.GetComponent<TextMeshProUGUI>();
                if (loadingText == null)
                    loadingText = loadingPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            fadeCanvasGroup = loadingPanel.GetComponent<CanvasGroup>();
            if (fadeCanvasGroup == null)
                fadeCanvasGroup = loadingPanel.AddComponent<CanvasGroup>();
        }
        else
        {
            Debug.LogWarning("[SceneTransition] Panel_GUILoading not found! Loading screen sẽ không hiện.");
        }
    }

    private void ShowLoadingPanel(string message)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = 0f;
        }
        if (loadingText != null)
            loadingText.text = message;
    }

    private void HideLoadingPanel()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    private void UpdateProgress(float progress, string message = "Loading...")
    {
        if (loadingBar != null)
            loadingBar.value = progress;
        if (loadingText != null)
            loadingText.text = $"{message} {(int)(progress * 100)}%";
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Scene mới load xong → panel cũ bị destroy → force tìm lại
        loadingPanel = null;
        loadingBar = null;
        loadingText = null;
        fadeCanvasGroup = null;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
            Instance = null;
    }
}
