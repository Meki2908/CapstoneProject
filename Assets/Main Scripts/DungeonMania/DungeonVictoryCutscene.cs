using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Phát video cutscene sau khi thắng boss cuối trong dungeon.
/// Gắn trên cùng GameObject với DungeonWaveManager hoặc bất kỳ đâu trong scene dungeon.
/// 
/// === SETUP ===
/// 1. Kéo file video (.mp4, .webm) vào Assets
/// 2. Gắn script này vào scene dungeon (Demon)
/// 3. Kéo VideoClip vào field "victoryCutsceneClip"
/// 4. Script tự tạo UI overlay khi chạy
/// 
/// Flow: Boss chết → loot delay → VIDEO CUTSCENE → UI thắng + reward
/// </summary>
public class DungeonVictoryCutscene : MonoBehaviour
{
    [Header("=== VIDEO CUTSCENE ===")]
    [Tooltip("Video clip phát sau khi thắng boss cuối. Để trống = bỏ qua.")]
    public VideoClip victoryCutsceneClip;

    [Tooltip("Cho phép skip cutscene bằng nút hoặc phím")]
    public bool allowSkip = true;

    [Tooltip("Label nút Skip")]
    public string skipButtonLabel = "Skip ▶▶";

    [Tooltip("Thời gian fade in/out (giây)")]
    public float fadeDuration = 0.5f;

    [Header("=== AUDIO ===")]
    [Tooltip("Âm lượng video (0-1)")]
    [Range(0f, 1f)]
    public float videoVolume = 1f;

    [Tooltip("Tắt nhạc game khi phát video")]
    public bool muteGameAudioDuringVideo = true;

    [Header("=== ĐIỀU KIỆN HIỆN ===")]
    [Tooltip("Độ khó TỐI THIỂU để cutscene được phát. Mặc định: Hard (chỉ mức Khó mới hiện).")]
    public DungeonDifficulty minimumDifficulty = DungeonDifficulty.Hard;

    // ─── Debug ───
    private void Update()
    {
        // F3 = phát cutscene ngay lập tức (bỏ qua điều kiện difficulty)
        if (!_isPlaying && victoryCutsceneClip != null
            && UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.f3Key.wasPressedThisFrame)
        {
            Debug.Log("[VictoryCutscene] ⚡ F3 — Force play cutscene (bypass difficulty)!");
            _forcePlay = true;
            StartCoroutine(PlayCutsceneAndWait());
        }
    }

    // ─── Runtime ───
    private bool _isPlaying = false;
    private bool _isSkipped = false;
    private bool _forcePlay = false; // F3 bypass difficulty check
    private GameObject _overlayRoot;
    private VideoPlayer _videoPlayer;
    private RawImage _videoDisplay;
    private RenderTexture _renderTexture;
    private CanvasGroup _canvasGroup;

    /// <summary>
    /// Có nên phát cutscene không — kiểm tra cả video clip VÀ độ khó hiện tại.
    /// </summary>
    public bool HasCutscene => victoryCutsceneClip != null && DungeonConfig.SelectedDifficulty >= minimumDifficulty;

    /// <summary>
    /// Đang phát cutscene hay không.
    /// </summary>
    public bool IsPlaying => _isPlaying;

    /// <summary>
    /// Coroutine chính: phát video cutscene, chờ xong (hoặc skip), rồi yield break.
    /// Gọi từ DungeonWaveManager sau khi boss chết và loot delay xong.
    /// </summary>
    public IEnumerator PlayCutsceneAndWait()
    {
        if (victoryCutsceneClip == null)
        {
            Debug.Log("[VictoryCutscene] Không có video clip — bỏ qua cutscene.");
            yield break;
        }

        if (_forcePlay)
        {
            _forcePlay = false; // Reset flag sau khi dùng
            Debug.Log("[VictoryCutscene] Force play — bỏ qua kiểm tra difficulty.");
        }
        else if (DungeonConfig.SelectedDifficulty < minimumDifficulty)
        {
            Debug.Log($"[VictoryCutscene] Độ khó hiện tại = {DungeonConfig.SelectedDifficulty}, yêu cầu >= {minimumDifficulty} — bỏ qua cutscene.");
            yield break;
        }

        Debug.Log($"[VictoryCutscene] Bắt đầu phát video: {victoryCutsceneClip.name} ({victoryCutsceneClip.length:F1}s)");

        _isPlaying = true;
        _isSkipped = false;

        // 1. Tạo UI overlay
        CreateOverlay();

        // 2. Tắt nhạc game nếu cần
        float previousMusicVolume = AudioListener.volume;
        if (muteGameAudioDuringVideo)
            AudioListener.volume = 0f;

        // 3. Setup VideoPlayer
        SetupVideoPlayer();

        // 4. Chuẩn bị video
        _videoPlayer.Prepare();
        float prepareTimeout = 10f;
        while (!_videoPlayer.isPrepared && prepareTimeout > 0f)
        {
            prepareTimeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_videoPlayer.isPrepared)
        {
            Debug.LogWarning("[VictoryCutscene] Video prepare timeout! Bỏ qua cutscene.");
            Cleanup();
            if (muteGameAudioDuringVideo) AudioListener.volume = previousMusicVolume;
            _isPlaying = false;
            yield break;
        }

        // 5. Fade in
        _videoPlayer.Play();
        yield return FadeCanvasGroup(0f, 1f, fadeDuration);

        // 6. Chờ video kết thúc hoặc skip
        while (_videoPlayer.isPlaying && !_isSkipped)
        {
            // Kiểm tra skip input
            if (allowSkip && IsSkipPressed())
            {
                _isSkipped = true;
                Debug.Log("[VictoryCutscene] Video SKIPPED!");
                break;
            }

            yield return null;
        }

        // 7. Fade out
        yield return FadeCanvasGroup(1f, 0f, fadeDuration);

        // 8. Cleanup
        if (muteGameAudioDuringVideo)
            AudioListener.volume = previousMusicVolume;

        Cleanup();
        _isPlaying = false;

        Debug.Log("[VictoryCutscene] Cutscene hoàn tất.");
    }

    // ─── PRIVATE METHODS ─────────────────────────────────────────────────

    private void CreateOverlay()
    {
        // Root Canvas
        _overlayRoot = new GameObject("VictoryCutsceneOverlay");
        Canvas canvas = _overlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3000; // Trên hết
        _overlayRoot.AddComponent<GraphicRaycaster>();
        var scaler = _overlayRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // CanvasGroup cho fade
        _canvasGroup = _overlayRoot.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;

        // Background đen
        GameObject bgGO = new GameObject("Background", typeof(RectTransform));
        bgGO.transform.SetParent(_overlayRoot.transform, false);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        Image bg = bgGO.AddComponent<Image>();
        bg.color = Color.black;

        // Video display (RawImage)
        GameObject videoGO = new GameObject("VideoDisplay", typeof(RectTransform));
        videoGO.transform.SetParent(_overlayRoot.transform, false);
        RectTransform videoRT = videoGO.GetComponent<RectTransform>();
        videoRT.anchorMin = Vector2.zero;
        videoRT.anchorMax = Vector2.one;
        videoRT.sizeDelta = Vector2.zero;
        _videoDisplay = videoGO.AddComponent<RawImage>();
        _videoDisplay.color = Color.white;

        // Skip button
        if (allowSkip)
        {
            CreateSkipButton();
        }
    }

    private void CreateSkipButton()
    {
        GameObject btnGO = new GameObject("SkipButton", typeof(RectTransform));
        btnGO.transform.SetParent(_overlayRoot.transform, false);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-40f, 40f);
        rt.sizeDelta = new Vector2(160f, 50f);

        Image btnBg = btnGO.AddComponent<Image>();
        btnBg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        btn.onClick.AddListener(() => _isSkipped = true);

        // Hover color
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.4f, 0.9f);
        btn.colors = colors;

        // Label
        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(btnGO.transform, false);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = skipButtonLabel;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 22f;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
    }

    private void SetupVideoPlayer()
    {
        // RenderTexture cho video
        _renderTexture = new RenderTexture(1920, 1080, 0);
        _renderTexture.Create();

        // VideoPlayer
        _videoPlayer = _overlayRoot.AddComponent<VideoPlayer>();
        _videoPlayer.playOnAwake = false;
        _videoPlayer.source = VideoSource.VideoClip;
        _videoPlayer.clip = victoryCutsceneClip;
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _videoPlayer.targetTexture = _renderTexture;
        _videoPlayer.isLooping = false;
        _videoPlayer.SetDirectAudioVolume(0, videoVolume);

        // Gán RenderTexture vào RawImage
        _videoDisplay.texture = _renderTexture;
    }

    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        if (_canvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        _canvasGroup.alpha = to;
    }

    private bool IsSkipPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            if (Keyboard.current.spaceKey.wasPressedThisFrame) return true;
            if (Keyboard.current.enterKey.wasPressedThisFrame) return true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
#endif
        // Legacy fallback
        if (Input.GetKeyDown(KeyCode.Escape)) return true;
        if (Input.GetKeyDown(KeyCode.Space)) return true;
        if (Input.GetMouseButtonDown(0)) return true;

        return false;
    }

    private void Cleanup()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.Stop();
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }

        if (_overlayRoot != null)
        {
            Destroy(_overlayRoot);
            _overlayRoot = null;
        }
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}
