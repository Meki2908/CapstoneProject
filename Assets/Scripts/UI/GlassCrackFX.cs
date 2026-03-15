using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn lên UI Image (overlay trong suốt) phía trên background.
/// Click chuột → tạo vết nứt kính tại vị trí click.
/// Sử dụng shader UI/GlassCrack.
/// </summary>
public class GlassCrackFX : MonoBehaviour
{
    [Header("=== CẤU HÌNH ===")]
    [Tooltip("Số vết nứt tối đa (mỗi click = 1 vết)")]
    [SerializeField] private int maxCracks = 10;
    [Tooltip("Bán kính vết nứt (normalized 0-1, so với UI element)")]
    [SerializeField] private float crackRadius = 0.15f;
    [Tooltip("Tốc độ lan vết nứt (giây)")]
    [SerializeField] private float crackExpandSpeed = 0.3f;
    [Tooltip("Thời gian vết nứt tồn tại trước khi biến mất (giây)")]
    [SerializeField] private float crackFadeTime = 1.5f;
    [Tooltip("Cường độ vết nứt")]
    [SerializeField] private float crackIntensity = 1f;

    [Header("=== SCREEN SHAKE ===")]
    [Tooltip("Rung màn hình khi click")]
    [SerializeField] private bool enableShake = true;
    [Tooltip("Biên độ rung (pixels)")]
    [SerializeField] private float shakeAmount = 10f;
    [Tooltip("Thời gian rung (giây)")]
    [SerializeField] private float shakeDuration = 0.3f;

    [Header("=== ÂM THANH ===")]
    [Tooltip("Kéo AudioClip tiếng vỡ kính vào đây (tùy chọn)")]
    [SerializeField] private AudioClip crackSound;
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 0.8f;

    // Internal
    private Material crackMaterial;
    private Image crackImage;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Vector4[] crackPoints;
    private float[] crackStartTimes;
    private int crackCount = 0;

    // Shake
    private RectTransform shakeTarget;
    private Vector2 shakeOrigin;
    private float shakeTimer;

    // Audio
    private AudioSource audioSource;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        crackImage = GetComponent<Image>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (crackImage == null)
        {
            crackImage = gameObject.AddComponent<Image>();
            crackImage.color = Color.white;
        }

        // Tạo material từ shader
        var shader = Shader.Find("UI/GlassCrack");
        if (shader == null)
        {
            Debug.LogError("[GlassCrackFX] Không tìm thấy shader 'UI/GlassCrack'!");
            enabled = false;
            return;
        }

        crackMaterial = new Material(shader);
        crackImage.material = crackMaterial;
        crackImage.raycastTarget = false; // Không chặn click các nút menu

        // Sprite mặc định (trắng) — shader sẽ vẽ crack trên đó
        if (crackImage.sprite == null)
        {
            var tex = new Texture2D(4, 4);
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            crackImage.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
        }

        // Init arrays
        crackPoints = new Vector4[maxCracks];
        crackStartTimes = new float[maxCracks];

        // Shake target = parent (thường là background image)
        shakeTarget = transform.parent as RectTransform;
        if (shakeTarget != null)
            shakeOrigin = shakeTarget.anchoredPosition;

        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && crackSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        // Detect click không qua raycast → không chặn nút menu
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, Input.mousePosition,
                parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? parentCanvas.worldCamera : null,
                out localPoint);

            // Kiểm tra click nằm trong rect
            if (rectTransform.rect.Contains(localPoint))
            {
                Vector2 uv = new Vector2(
                    (localPoint.x / rectTransform.rect.width) + rectTransform.pivot.x,
                    (localPoint.y / rectTransform.rect.height) + rectTransform.pivot.y
                );
                AddCrack(uv);
            }
        }

        UpdateCrackAnimation();
        UpdateShake();
    }

    void AddCrack(Vector2 uv)
    {
        if (crackCount >= maxCracks)
        {
            // Xoay vòng — xóa vết cũ nhất
            for (int i = 0; i < maxCracks - 1; i++)
            {
                crackPoints[i] = crackPoints[i + 1];
                crackStartTimes[i] = crackStartTimes[i + 1];
            }
            crackCount = maxCracks - 1;
        }

        crackPoints[crackCount] = new Vector4(uv.x, uv.y, 0f, crackIntensity);
        crackStartTimes[crackCount] = Time.time;
        crackCount++;

        // Shake
        if (enableShake)
            shakeTimer = shakeDuration;

        // Sound
        if (crackSound != null && audioSource != null)
            audioSource.PlayOneShot(crackSound, soundVolume);
    }

    void UpdateCrackAnimation()
    {
        if (crackMaterial == null) return;

        float totalTime = crackExpandSpeed + crackFadeTime;

        // Xóa vết nứt hết hạn (duyệt ngược)
        for (int i = crackCount - 1; i >= 0; i--)
        {
            float elapsed = Time.time - crackStartTimes[i];
            if (elapsed > totalTime)
            {
                // Xóa bằng cách dồn mảng
                for (int j = i; j < crackCount - 1; j++)
                {
                    crackPoints[j] = crackPoints[j + 1];
                    crackStartTimes[j] = crackStartTimes[j + 1];
                }
                crackCount--;
            }
        }

        // Cập nhật animation
        for (int i = 0; i < crackCount; i++)
        {
            float elapsed = Time.time - crackStartTimes[i];

            // Giai đoạn 1: Lan nhanh (0 → crackExpandSpeed)
            float expandProgress = Mathf.Clamp01(elapsed / crackExpandSpeed);
            float easedExpand = 1f - Mathf.Pow(1f - expandProgress, 3f);
            float radius = crackRadius * easedExpand;

            // Giai đoạn 2: Fade out (crackExpandSpeed → totalTime)
            float fadeProgress = Mathf.Clamp01((elapsed - crackExpandSpeed) / crackFadeTime);
            float intensity = crackIntensity * (1f - fadeProgress);

            crackPoints[i] = new Vector4(
                crackPoints[i].x,
                crackPoints[i].y,
                radius,
                intensity
            );
        }

        crackMaterial.SetVectorArray("_CrackPoints", crackPoints);
        crackMaterial.SetInt("_CrackPointCount", crackCount);
    }

    void UpdateShake()
    {
        if (!enableShake || shakeTarget == null) return;

        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            float intensity = shakeTimer / shakeDuration;
            Vector2 offset = Random.insideUnitCircle * shakeAmount * intensity;
            shakeTarget.anchoredPosition = shakeOrigin + offset;
        }
        else
        {
            shakeTarget.anchoredPosition = shakeOrigin;
        }
    }

    /// <summary>
    /// Xóa hết vết nứt (gọi từ code hoặc button)
    /// </summary>
    public void ClearAllCracks()
    {
        crackCount = 0;
        crackPoints = new Vector4[maxCracks];
        crackStartTimes = new float[maxCracks];
        if (crackMaterial != null)
            crackMaterial.SetInt("_CrackPointCount", 0);
    }
}
