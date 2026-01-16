using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Script quản lý Background cho Menu - Hỗ trợ Image và Video
/// Cách dùng: Gắn script này vào Canvas, sau đó kéo Sprite/Video vào Inspector
/// </summary>
[ExecuteInEditMode]
public class MenuBackground : MonoBehaviour
{
    public enum BackgroundType { Image, Video }
    
    [Header("=== BACKGROUND TYPE ===")]
    public BackgroundType backgroundType = BackgroundType.Image;
    
    [Header("=== IMAGE SETTINGS ===")]
    public Sprite backgroundSprite;
    public Color imageColor = Color.white;
    
    [Header("=== VIDEO SETTINGS ===")]
    public VideoClip backgroundVideo;
    public bool loopVideo = true;
    [Range(0f, 1f)]
    public float videoVolume = 0f;
    
    [Header("=== OVERLAY (Tùy chọn) ===")]
    public bool useOverlay = true;
    public Color overlayColor = new Color(0, 0, 0, 0.4f);
    
    [Header("=== CANVAS SORTING ===")]
    [Tooltip("Tự động đặt Sort Order thấp để nằm dưới các Canvas khác")]
    public bool autoSetSortOrder = true;
    [Tooltip("Giá trị Sort Order (số càng nhỏ càng nằm dưới)")]
    public int sortOrder = -100;
    
    // Serialized references - giữ lại khi reload
    [Header("=== REFERENCES (Tự động) ===")]
    [SerializeField] private Image bgImage;
    [SerializeField] private RawImage videoRawImage;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Image overlay;
    
    private RenderTexture renderTexture;
    
    void OnEnable()
    {
        // Tự động tạo background khi enable
        if (bgImage == null && videoRawImage == null)
        {
            CreateBackground();
        }
        
        SetupCanvasSorting();
        EnsureBackgroundBehind();
    }
    
    void LateUpdate()
    {
        // Đảm bảo background luôn nằm dưới cùng
        EnsureBackgroundBehind();
    }
    
    void EnsureBackgroundBehind()
    {
        Transform bgContainer = transform.Find("_MenuBackground_");
        if (bgContainer != null && bgContainer.GetSiblingIndex() != 0)
        {
            bgContainer.SetAsFirstSibling();
        }
    }
    
    void SetupCanvasSorting()
    {
        if (!autoSetSortOrder) return;
        
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            // Nếu không có Canvas, thêm vào
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // Thêm các component cần thiết
            if (GetComponent<UnityEngine.UI.CanvasScaler>() == null)
                gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        // Đảm bảo đây là root Canvas với sort order thấp
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortOrder;
        
        Debug.Log($"[MenuBackground] Canvas sorting set to {sortOrder}. Make sure Main_Menu Canvas has Sort Order > {sortOrder}");
    }
    
    void OnValidate()
    {
        // Cập nhật ngay khi thay đổi trong Inspector
        if (bgImage != null)
        {
            bgImage.sprite = backgroundSprite;
            bgImage.color = imageColor;
            bgImage.gameObject.SetActive(backgroundType == BackgroundType.Image);
        }
        
        if (videoRawImage != null)
        {
            videoRawImage.gameObject.SetActive(backgroundType == BackgroundType.Video);
        }
        
        if (overlay != null)
        {
            overlay.color = overlayColor;
            overlay.gameObject.SetActive(useOverlay);
        }
        
        SetupCanvasSorting();
    }
    
    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            DestroyImmediate(renderTexture);
        }
    }
    
    /// <summary>
    /// Tạo background - tự động gọi khi enable hoặc có thể gọi thủ công
    /// </summary>
    [ContextMenu("Create/Refresh Background")]
    public void CreateBackground()
    {
        // Xóa container cũ nếu có
        Transform existing = transform.Find("_MenuBackground_");
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }
        
        // Tạo container mới
        GameObject backgroundContainer = new GameObject("_MenuBackground_");
        backgroundContainer.transform.SetParent(transform);
        backgroundContainer.transform.SetAsFirstSibling();
        
        RectTransform containerRect = backgroundContainer.AddComponent<RectTransform>();
        StretchRectTransform(containerRect);
        
        // Tạo Image Background
        CreateImageBackground(backgroundContainer);
        
        // Tạo Video Background
        CreateVideoBackground(backgroundContainer);
        
        // Tạo overlay
        CreateOverlay(backgroundContainer);
        
        // Set active dựa trên type
        UpdateVisibility();
    }
    
    void CreateImageBackground(GameObject container)
    {
        GameObject imageGO = new GameObject("BG_Image");
        imageGO.transform.SetParent(container.transform);
        
        RectTransform rect = imageGO.AddComponent<RectTransform>();
        StretchRectTransform(rect);
        
        bgImage = imageGO.AddComponent<Image>();
        bgImage.sprite = backgroundSprite;
        bgImage.color = imageColor;
        bgImage.raycastTarget = false;
        bgImage.preserveAspect = false;
    }
    
    void CreateVideoBackground(GameObject container)
    {
        GameObject videoGO = new GameObject("BG_Video");
        videoGO.transform.SetParent(container.transform);
        videoGO.transform.SetAsFirstSibling();
        
        RectTransform rect = videoGO.AddComponent<RectTransform>();
        StretchRectTransform(rect);
        
        videoRawImage = videoGO.AddComponent<RawImage>();
        videoRawImage.raycastTarget = false;
        
        videoPlayer = videoGO.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = true;
        videoPlayer.isLooping = loopVideo;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        
        if (backgroundVideo != null)
        {
            SetupVideoTexture();
        }
    }
    
    void SetupVideoTexture()
    {
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(1920, 1080, 0);
            renderTexture.Create();
        }
        
        if (videoPlayer != null)
        {
            videoPlayer.clip = backgroundVideo;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.SetDirectAudioVolume(0, videoVolume);
        }
        
        if (videoRawImage != null)
        {
            videoRawImage.texture = renderTexture;
        }
    }
    
    void CreateOverlay(GameObject container)
    {
        GameObject overlayGO = new GameObject("BG_Overlay");
        overlayGO.transform.SetParent(container.transform);
        overlayGO.transform.SetAsLastSibling();
        
        RectTransform rect = overlayGO.AddComponent<RectTransform>();
        StretchRectTransform(rect);
        
        overlay = overlayGO.AddComponent<Image>();
        overlay.color = overlayColor;
        overlay.raycastTarget = false;
        overlay.gameObject.SetActive(useOverlay);
    }
    
    void UpdateVisibility()
    {
        if (bgImage != null)
            bgImage.gameObject.SetActive(backgroundType == BackgroundType.Image);
        
        if (videoRawImage != null)
            videoRawImage.gameObject.SetActive(backgroundType == BackgroundType.Video);
        
        if (overlay != null)
            overlay.gameObject.SetActive(useOverlay);
    }
    
    void StretchRectTransform(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;
    }
    
    // === PUBLIC METHODS ===
    
    public void SetImage(Sprite newSprite)
    {
        backgroundSprite = newSprite;
        if (bgImage != null)
            bgImage.sprite = newSprite;
    }
    
    public void SetVideo(VideoClip newVideo)
    {
        backgroundVideo = newVideo;
        if (videoPlayer != null)
        {
            videoPlayer.clip = newVideo;
            SetupVideoTexture();
        }
    }
    
    public void ToggleVideo()
    {
        if (videoPlayer == null) return;
        
        if (videoPlayer.isPlaying)
            videoPlayer.Pause();
        else
            videoPlayer.Play();
    }
}
