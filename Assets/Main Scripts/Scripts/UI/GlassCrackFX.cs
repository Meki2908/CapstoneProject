using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn lên UI Image (overlay) phía trên background. Cần shader UI/GlassCrack.
/// Nếu shader thiếu (strip build / mất asset), tự ẩn Image để tránh màn trắng.
/// </summary>
public class GlassCrackFX : MonoBehaviour
{
    [Header("=== CẤU HÌNH ===")]
    [SerializeField] private int maxCracks = 10;
    [SerializeField] private float crackRadius = 0.15f;
    [SerializeField] private float crackExpandSpeed = 0.3f;
    [SerializeField] private float crackFadeTime = 1.5f;
    [SerializeField] private float crackIntensity = 1f;

    [Header("=== SCREEN SHAKE ===")]
    [SerializeField] private bool enableShake = true;
    [SerializeField] private float shakeAmount = 10f;
    [SerializeField] private float shakeDuration = 0.3f;

    [Header("=== ÂM THANH ===")]
    [SerializeField] private AudioClip crackSound;
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 0.8f;

    [Header("=== BUILD SAFETY ===")]
    [Tooltip("Nếu không tìm thấy shader UI/GlassCrack, tự ẩn overlay để tránh màn trắng.")]
    [SerializeField] private bool hideOverlayIfShaderMissing = true;

    private Material crackMaterial;
    private Image crackImage;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Vector4[] crackPoints;
    private float[] crackStartTimes;
    private int crackCount;

    private RectTransform shakeTarget;
    private Vector2 shakeOrigin;
    private float shakeTimer;

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

        var shader = Shader.Find("UI/GlassCrack");
        if (shader == null)
        {
            Debug.LogError("[GlassCrackFX] Không tìm thấy shader 'UI/GlassCrack'. Thêm vào Always Included Shaders nếu cần build.");
            if (hideOverlayIfShaderMissing && crackImage != null)
            {
                crackImage.enabled = false;
                crackImage.color = new Color(1f, 1f, 1f, 0f);
                Debug.LogWarning("[GlassCrackFX] Đã ẩn overlay (thiếu shader) để tránh màn trắng.");
            }
            enabled = false;
            return;
        }

        crackMaterial = new Material(shader);
        crackImage.material = crackMaterial;
        crackImage.raycastTarget = false;

        if (crackImage.sprite == null)
        {
            var tex = new Texture2D(4, 4);
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            crackImage.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
        }

        crackPoints = new Vector4[maxCracks];
        crackStartTimes = new float[maxCracks];

        shakeTarget = transform.parent as RectTransform;
        if (shakeTarget != null)
            shakeOrigin = shakeTarget.anchoredPosition;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && crackSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        if (crackMaterial == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, Input.mousePosition,
                parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? parentCanvas.worldCamera : null,
                out localPoint);

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

        if (enableShake)
            shakeTimer = shakeDuration;

        if (crackSound != null && audioSource != null)
            audioSource.PlayOneShot(crackSound, soundVolume);
    }

    void UpdateCrackAnimation()
    {
        if (crackMaterial == null) return;

        float totalTime = crackExpandSpeed + crackFadeTime;

        for (int i = crackCount - 1; i >= 0; i--)
        {
            float elapsed = Time.time - crackStartTimes[i];
            if (elapsed > totalTime)
            {
                for (int j = i; j < crackCount - 1; j++)
                {
                    crackPoints[j] = crackPoints[j + 1];
                    crackStartTimes[j] = crackStartTimes[j + 1];
                }
                crackCount--;
            }
        }

        for (int i = 0; i < crackCount; i++)
        {
            float elapsed = Time.time - crackStartTimes[i];
            float expandProgress = Mathf.Clamp01(elapsed / crackExpandSpeed);
            float easedExpand = 1f - Mathf.Pow(1f - expandProgress, 3f);
            float radius = crackRadius * easedExpand;
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

    public void ClearAllCracks()
    {
        crackCount = 0;
        crackPoints = new Vector4[maxCracks];
        crackStartTimes = new float[maxCracks];
        if (crackMaterial != null)
            crackMaterial.SetInt("_CrackPointCount", 0);
    }
}
