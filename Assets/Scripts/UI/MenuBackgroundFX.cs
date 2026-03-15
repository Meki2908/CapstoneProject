using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu Background FX — 4 hiệu ứng cho ảnh nền kính vỡ:
/// 1. Parallax chuột (ảnh nền dịch nhẹ theo chuột)
/// 2. Tia lửa + ánh sáng nhấp nháy từ lỗ kính (focal point)
/// 3. Sương mù trôi chậm (atmosphere)
/// 4. Bụi lấp lánh (polish)
/// 
/// SETUP: Gắn script này vào ảnh nền (Image) trong Canvas.
/// Hoặc gắn vào GameObject trống trong scene menu.
/// Script tự tạo mọi thứ bằng code — không cần kéo thả gì.
/// </summary>
public class MenuBackgroundFX : MonoBehaviour
{
    [Header("=== PARALLAX ===")]
    [Tooltip("Ảnh nền Image để áp dụng parallax. Nếu null sẽ tự tìm Image trên GameObject này.")]
    [SerializeField] private RectTransform backgroundImage;
    [Tooltip("Biên độ parallax (pixels canvas)")]
    [SerializeField] private float parallaxAmount = 30f;
    [Tooltip("Tốc độ lerp parallax (nhỏ = mượt hơn)")]
    [SerializeField] private float parallaxSmooth = 3f;

    [Header("=== TIA LỬA (Sparks) ===")]
    [Tooltip("Dùng tọa độ pixel ảnh thay vì normalized 0-1")]
    [SerializeField] private bool usePixelCoords = true;
    [Tooltip("Resolution ảnh nền gốc (width × height)")]
    [SerializeField] private Vector2 imageResolution = new Vector2(1920, 1080);
    [Tooltip("Vị trí lỗ kính tính bằng pixel (gốc = góc TRÁI DƯỚI)")]
    [SerializeField] private Vector2 focalPointPixel = new Vector2(1382, 778);
    [Tooltip("Vị trí lỗ kính (0-1 normalized). Tự tính nếu dùng pixel")]
    [SerializeField] private Vector2 focalPointNormalized = new Vector2(0.72f, 0.72f);
    [Tooltip("Số tia lửa")]
    [SerializeField] private int sparkCount = 40;
    [Tooltip("Màu tia lửa")]
    [SerializeField] private Color sparkColorStart = new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color sparkColorEnd = new Color(1f, 0.4f, 0.1f, 0.6f);

    [Header("=== ÁNH SÁNG NHẤP NHÁY ===")]
    [Tooltip("Bật/tắt ánh sáng nhấp nháy ở lỗ kính")]
    [SerializeField] private bool enableGlow = true;
    [Tooltip("Cường độ glow min/max")]
    [SerializeField] private float glowIntensityMin = 0.3f;
    [SerializeField] private float glowIntensityMax = 1.0f;
    [Tooltip("Tốc độ nhấp nháy")]
    [SerializeField] private float glowPulseSpeed = 2f;

    [Header("=== SƯƠNG MÙ ===")]
    [Tooltip("Số lượng particle sương mù")]
    [SerializeField] private int fogCount = 30;
    [Tooltip("Màu sương mù")]
    [SerializeField] private Color fogColor = new Color(0.3f, 0.35f, 0.5f, 0.08f);

    [Header("=== BỤI LẤP LÁNH ===")]
    [Tooltip("Số hạt bụi")]
    [SerializeField] private int dustCount = 50;
    [Tooltip("Màu bụi")]
    [SerializeField] private Color dustColor = new Color(1f, 1f, 1f, 0.5f);

    [Header("=== BẬT/TẮT TỪNG HIỆU ỨNG ===")]
    [SerializeField] private bool enableParallax = true;
    [SerializeField] private bool enableSparks = true;
    [SerializeField] private bool enableFog = true;
    [SerializeField] private bool enableDust = true;
    [SerializeField] private bool enableEyeTracking = true;

    [Header("=== MẮT NHÌN THEO CHUỘT ===")]
    [Tooltip("Kéo sprite con ngươi vào đây")]
    [SerializeField] private Sprite pupilSprite;
    [Tooltip("Kéo sprite mống mắt vào đây (tùy chọn)")]
    [SerializeField] private Sprite irisSprite;
    [Tooltip("Kích thước pupil (pixels canvas)")]
    [SerializeField] private float pupilSize = 30f;
    [Tooltip("Kích thước iris (pixels canvas)")]
    [SerializeField] private float irisSize = 60f;
    [Tooltip("Biên độ di chuyển tối đa của pupil (pixels canvas)")]
    [SerializeField] private float pupilMaxOffset = 12f;
    [Tooltip("Tốc độ lerp pupil (nhỏ = mượt hơn)")]
    [SerializeField] private float pupilSmooth = 8f;

    // Internal
    private Vector2 parallaxOrigin;
    private ParticleSystem sparkPS;
    private Image glowImage;
    private RectTransform pupilRT;
    private RectTransform irisRT;
    private Canvas parentCanvas;
    private Camera canvasCamera;

    void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) parentCanvas = FindFirstObjectByType<Canvas>();
        canvasCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera : null;

        // Auto-convert pixel → normalized
        ConvertPixelToNormalized();

        // Parallax setup
        if (backgroundImage == null)
        {
            var img = GetComponent<Image>();
            if (img != null) backgroundImage = img.rectTransform;
        }
        if (backgroundImage != null)
            parallaxOrigin = backgroundImage.anchoredPosition;

        // Create effects
        if (enableSparks) CreateSparkEffect();
        if (enableGlow) CreateGlowEffect();
        if (enableFog) CreateFogEffect();
        if (enableDust) CreateDustEffect();
        if (enableEyeTracking) InitEyeTracking();
    }

    void Update()
    {
        ConvertPixelToNormalized();
        if (enableParallax) UpdateParallax();
        if (enableGlow) UpdateGlow();
        if (enableEyeTracking) UpdateEyeTracking();
        if (enableFog) UpdateFog();
        if (enableDust) UpdateDust();
        UpdateFocalPosition();
    }

    // ─────────────────────────────────────────────────
    // 1. PARALLAX
    // ─────────────────────────────────────────────────
    void UpdateParallax()
    {
        if (backgroundImage == null) return;

        // Normalize mouse position to -1..1
        float mx = (Input.mousePosition.x / Screen.width - 0.5f) * 2f;
        float my = (Input.mousePosition.y / Screen.height - 0.5f) * 2f;

        Vector2 target = parallaxOrigin + new Vector2(-mx * parallaxAmount, -my * parallaxAmount);
        backgroundImage.anchoredPosition = Vector2.Lerp(
            backgroundImage.anchoredPosition, target, Time.deltaTime * parallaxSmooth);
    }

    // ─────────────────────────────────────────────────
    // 2. TIA LỬA (Sparks from focal point)
    // ─────────────────────────────────────────────────
    void CreateSparkEffect()
    {
        Vector3 worldPos = GetFocalWorldPosition();

        var go = new GameObject("MenuSparks");
        Transform fxParent = backgroundImage != null ? backgroundImage.transform : transform;
        go.transform.SetParent(fxParent, false);
        go.transform.position = worldPos;
        IgnoreLayout(go);

        sparkPS = go.AddComponent<ParticleSystem>();
        var main = sparkPS.main;
        main.maxParticles = sparkCount;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(sparkColorStart, sparkColorEnd);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;
        main.playOnAwake = true;
        main.loop = true;

        var emission = sparkPS.emission;
        emission.rateOverTime = sparkCount / 2f;

        // Burst — tia lửa bắn ra theo đợt
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 3, 8, 1, 0.8f)
        });

        var shape = sparkPS.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        shape.radius = 0.1f;

        // Fade out
        var col = sparkPS.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(sparkColorStart, 0f),
                new GradientColorKey(sparkColorEnd, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = grad;

        // Size shrink
        var sol = sparkPS.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f)
        ));

        // Trail — đuôi sáng
        var trails = sparkPS.trails;
        trails.enabled = true;
        trails.lifetime = 0.2f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(1f, 0f)
        ));
        trails.colorOverLifetime = new ParticleSystem.MinMaxGradient(sparkColorStart, new Color(1f, 0.3f, 0f, 0f));

        // Renderer
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateAdditiveMaterial();
        renderer.trailMaterial = CreateAdditiveMaterial();
        renderer.sortingOrder = 2;
    }

    // ─────────────────────────────────────────────────
    // 2b. GLOW — ánh sáng nhấp nháy tại focal point
    // ─────────────────────────────────────────────────
    void CreateGlowEffect()
    {
        // Tạo UI Image overlay ở vị trí lỗ kính
        var glowGO = new GameObject("FocalGlow", typeof(RectTransform));
        Transform fxParent = backgroundImage != null ? backgroundImage.transform : transform;
        glowGO.transform.SetParent(fxParent, false);
        IgnoreLayout(glowGO);

        glowImage = glowGO.AddComponent<Image>();
        glowImage.sprite = CreateSoftCircleSprite(128);
        glowImage.color = new Color(1f, 0.7f, 0.2f, 0.5f);
        glowImage.raycastTarget = false;

        var rt = glowGO.GetComponent<RectTransform>();
        rt.anchorMin = focalPointNormalized;
        rt.anchorMax = focalPointNormalized;
        rt.sizeDelta = new Vector2(300, 300);
        rt.anchoredPosition = Vector2.zero;
    }

    void UpdateGlow()
    {
        if (glowImage == null) return;

        // Pulse intensity with Perlin noise for organic feel
        float noise = Mathf.PerlinNoise(Time.time * glowPulseSpeed, 0.5f);
        float intensity = Mathf.Lerp(glowIntensityMin, glowIntensityMax, noise);

        // Secondary faster flicker
        float flicker = Mathf.PerlinNoise(Time.time * glowPulseSpeed * 3f, 10f);
        intensity *= Mathf.Lerp(0.85f, 1.15f, flicker);

        glowImage.color = new Color(1f, 0.7f, 0.2f, intensity * 0.5f);

        // Subtle size pulse
        float scale = Mathf.Lerp(0.9f, 1.1f, noise);
        glowImage.rectTransform.localScale = Vector3.one * scale;
    }

    /// <summary>
    /// Cập nhật vị trí glow + sparks theo focalPointNormalized (realtime trong Inspector)
    /// </summary>
    void UpdateFocalPosition()
    {
        // Update glow anchor
        if (glowImage != null)
        {
            var rt = glowImage.rectTransform;
            rt.anchorMin = focalPointNormalized;
            rt.anchorMax = focalPointNormalized;
        }

        // Update spark position
        if (sparkPS != null)
        {
            sparkPS.transform.position = GetFocalWorldPosition();
        }
    }

    void InitEyeTracking()
    {
        if (pupilSprite == null) return;

        Transform fxParent = backgroundImage != null ? backgroundImage.transform : transform;

        // Iris (lớp dưới, nếu có sprite)
        if (irisSprite != null)
        {
            var irisGO = new GameObject("EyeIris", typeof(RectTransform));
            irisGO.transform.SetParent(fxParent, false);
            IgnoreLayout(irisGO);

            var irisImg = irisGO.AddComponent<Image>();
            irisImg.sprite = irisSprite;
            irisImg.preserveAspect = true;
            irisImg.raycastTarget = false;

            irisRT = irisGO.GetComponent<RectTransform>();
            irisRT.anchorMin = focalPointNormalized;
            irisRT.anchorMax = focalPointNormalized;
            irisRT.sizeDelta = new Vector2(irisSize, irisSize);
            irisRT.anchoredPosition = Vector2.zero;
        }

        // Pupil (lớp trên)
        var pupilGO = new GameObject("EyePupil", typeof(RectTransform));
        pupilGO.transform.SetParent(fxParent, false);
        IgnoreLayout(pupilGO);

        var pupilImg = pupilGO.AddComponent<Image>();
        pupilImg.sprite = pupilSprite;
        pupilImg.preserveAspect = true;
        pupilImg.raycastTarget = false;

        pupilRT = pupilGO.GetComponent<RectTransform>();
        pupilRT.anchorMin = focalPointNormalized;
        pupilRT.anchorMax = focalPointNormalized;
        pupilRT.sizeDelta = new Vector2(pupilSize, pupilSize);
        pupilRT.anchoredPosition = Vector2.zero;
    }

    void UpdateEyeTracking()
    {
        if (pupilRT == null) return;

        // Realtime size + anchor update
        pupilRT.sizeDelta = new Vector2(pupilSize, pupilSize);
        pupilRT.anchorMin = focalPointNormalized;
        pupilRT.anchorMax = focalPointNormalized;
        if (irisRT != null)
        {
            irisRT.sizeDelta = new Vector2(irisSize, irisSize);
            irisRT.anchorMin = focalPointNormalized;
            irisRT.anchorMax = focalPointNormalized;
        }

        // Tính hướng từ pupil → chuột
        Camera cam = parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : canvasCamera;
        Vector2 eyeScreenPos = RectTransformUtility.WorldToScreenPoint(cam, pupilRT.position);

        Vector2 mousePos = Input.mousePosition;
        Vector2 dir = mousePos - eyeScreenPos;
        float dist = dir.magnitude;
        if (dist > 0.01f) dir /= dist;

        float offsetAmount = Mathf.Clamp(dist / (Screen.height * 0.3f), 0f, 1f) * pupilMaxOffset;
        Vector2 targetOffset = dir * offsetAmount;

        // Pupil di chuyển theo chuột
        pupilRT.anchoredPosition = Vector2.Lerp(
            pupilRT.anchoredPosition, targetOffset, Time.deltaTime * pupilSmooth);

        // Iris dịch nhẹ hơn (40%)
        if (irisRT != null)
        {
            irisRT.anchoredPosition = Vector2.Lerp(
                irisRT.anchoredPosition, targetOffset * 0.4f, Time.deltaTime * pupilSmooth);
        }
    }

    // ─────────────────────────────────────────────────
    // 3. SƯƠNG MÙ (UI-based — hoạt động mọi Canvas mode)
    // ─────────────────────────────────────────────────
    private struct UIParticle
    {
        public RectTransform rt;
        public Image img;
        public Vector2 velocity;
        public float lifetime;
        public float age;
        public float maxAlpha;
        public float rotSpeed;
    }

    private UIParticle[] fogParticles;
    private UIParticle[] dustParticles;

    void CreateFogEffect()
    {
        fogParticles = new UIParticle[fogCount];
        var sprite = CreateSoftCircleSprite(128);
        var canvasRT = parentCanvas.GetComponent<RectTransform>();
        float cw = canvasRT.rect.width;
        float ch = canvasRT.rect.height;
        float halfW = cw * 0.6f;
        float halfH = ch * 0.6f;

        for (int i = 0; i < fogCount; i++)
        {
            var go = new GameObject($"Fog_{i}", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            IgnoreLayout(go);

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = fogColor;
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            float size = Random.Range(cw * 0.15f, cw * 0.35f);
            rt.sizeDelta = new Vector2(size, size);
            // Spawn rải đều trên màn hình
            rt.anchoredPosition = new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));

            // Velocity tính theo % canvas width → tự scale mọi resolution
            float speed = Random.Range(cw * 0.03f, cw * 0.08f);
            fogParticles[i] = new UIParticle
            {
                rt = rt, img = img,
                velocity = new Vector2(-speed, Random.Range(-ch * 0.005f, ch * 0.005f)),
                lifetime = Random.Range(12f, 22f),
                age = Random.Range(0f, 12f), // stagger để không spawn cùng lúc
                maxAlpha = fogColor.a,
                rotSpeed = Random.Range(-3f, 3f)
            };
        }
    }

    void UpdateFog()
    {
        if (fogParticles == null) return;
        var canvasRT = parentCanvas.GetComponent<RectTransform>();
        float cw = canvasRT.rect.width;
        float ch = canvasRT.rect.height;
        float halfW = cw * 0.6f;
        float halfH = ch * 0.6f;

        for (int i = 0; i < fogParticles.Length; i++)
        {
            ref var p = ref fogParticles[i];
            p.age += Time.deltaTime;

            // Respawn khi hết lifetime HOẶC ra khỏi màn hình bên trái
            if (p.age >= p.lifetime || p.rt.anchoredPosition.x < -halfW - 300f)
            {
                p.age = 0f;
                // Spawn bên phải, ngoài màn hình một chút
                float size = Random.Range(cw * 0.15f, cw * 0.35f);
                p.rt.sizeDelta = new Vector2(size, size);
                p.rt.anchoredPosition = new Vector2(halfW + size * 0.5f, Random.Range(-halfH, halfH));
                float speed = Random.Range(cw * 0.03f, cw * 0.08f);
                p.velocity = new Vector2(-speed, Random.Range(-ch * 0.005f, ch * 0.005f));
                p.lifetime = Random.Range(12f, 22f);
            }

            // Di chuyển
            p.rt.anchoredPosition += p.velocity * Time.deltaTime;
            p.rt.Rotate(0, 0, p.rotSpeed * Time.deltaTime);

            // Fade in/out
            float t = p.age / p.lifetime;
            float alpha = t < 0.15f ? t / 0.15f : (t > 0.85f ? (1f - t) / 0.15f : 1f);
            var c = fogColor;
            c.a = alpha * p.maxAlpha;
            p.img.color = c;
        }
    }

    // ─────────────────────────────────────────────────
    // 4. BỤI LẤP LÁNH (UI-based — hoạt động mọi Canvas mode)
    // ─────────────────────────────────────────────────
    void CreateDustEffect()
    {
        dustParticles = new UIParticle[dustCount];
        var sprite = CreateSoftCircleSprite(32);
        var canvasRT = parentCanvas.GetComponent<RectTransform>();
        float halfW = canvasRT.rect.width * 0.5f;
        float halfH = canvasRT.rect.height * 0.5f;

        for (int i = 0; i < dustCount; i++)
        {
            var go = new GameObject($"Dust_{i}", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            IgnoreLayout(go);

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = dustColor;
            img.raycastTarget = false;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            float size = Random.Range(3f, 8f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));

            dustParticles[i] = new UIParticle
            {
                rt = rt, img = img,
                velocity = new Vector2(Random.Range(-8f, 8f), Random.Range(2f, 10f)),
                lifetime = Random.Range(5f, 12f),
                age = Random.Range(0f, 8f),
                maxAlpha = dustColor.a,
                rotSpeed = 0
            };
        }
    }

    void UpdateDust()
    {
        if (dustParticles == null) return;
        var canvasRT = parentCanvas.GetComponent<RectTransform>();
        float halfW = canvasRT.rect.width * 0.5f;
        float halfH = canvasRT.rect.height * 0.5f;

        for (int i = 0; i < dustParticles.Length; i++)
        {
            ref var p = ref dustParticles[i];
            p.age += Time.deltaTime;
            if (p.age >= p.lifetime)
            {
                p.age = 0f;
                p.rt.anchoredPosition = new Vector2(Random.Range(-halfW, halfW), -halfH);
                p.velocity = new Vector2(Random.Range(-8f, 8f), Random.Range(2f, 10f));
                p.lifetime = Random.Range(5f, 12f);
            }
            p.rt.anchoredPosition += p.velocity * Time.deltaTime;
            float t = p.age / p.lifetime;
            float twinkle = Mathf.Sin(p.age * 4f) * 0.5f + 0.5f;
            float fadeAlpha = t < 0.15f ? t / 0.15f : (t > 0.85f ? (1f - t) / 0.15f : 1f);
            var c = dustColor;
            c.a = fadeAlpha * twinkle * p.maxAlpha;
            p.img.color = c;
        }
    }

    // ─────────────────────────────────────────────────
    // HELPER METHODS
    // ─────────────────────────────────────────────────

    /// <summary>
    /// Chuyển đổi pixel → normalized (0-1) nếu usePixelCoords = true
    /// Gốc pixel = góc TRÁI DƯỚI (giống Unity Sprite Editor)
    /// </summary>
    void ConvertPixelToNormalized()
    {
        if (!usePixelCoords) return;
        if (imageResolution.x <= 0 || imageResolution.y <= 0) return;

        focalPointNormalized = new Vector2(
            focalPointPixel.x / imageResolution.x,
            focalPointPixel.y / imageResolution.y
        );
    }

    Vector3 GetFocalWorldPosition()
    {
        if (parentCanvas == null) return transform.position;

        var canvasRT = parentCanvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRT.rect.size;
        Vector2 localPos = new Vector2(
            (focalPointNormalized.x - 0.5f) * canvasSize.x,
            (focalPointNormalized.y - 0.5f) * canvasSize.y
        );
        return canvasRT.TransformPoint(localPos);
    }

    /// <summary>
    /// Đánh dấu GameObject ignore layout để LayoutGroup không đẩy VFX
    /// </summary>
    void IgnoreLayout(GameObject go)
    {
        var le = go.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
    }

    Material CreateAdditiveMaterial()
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Mobile/Particles/Additive");

        var mat = new Material(shader);
        mat.mainTexture = CreateSoftCircleTexture(64);
        mat.renderQueue = 3100;
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        return mat;
    }

    Material CreateAlphaBlendMaterial()
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Mobile/Particles/Alpha Blended");

        var mat = new Material(shader);
        mat.mainTexture = CreateSoftCircleTexture(128);
        mat.renderQueue = 3000;
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        return mat;
    }

    Texture2D CreateSoftCircleTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(1f - (dist / center));
                alpha = alpha * alpha * alpha; // smooth cubic falloff
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    Sprite CreateSoftCircleSprite(int size)
    {
        var tex = CreateSoftCircleTexture(size);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
