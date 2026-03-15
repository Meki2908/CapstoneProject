using UnityEngine;

/// <summary>
/// Sương mù 3D volumetric dùng Particle System.
/// Các hạt sương to, mờ, trôi nhẹ trong không gian, tạo cảm giác
/// player thực sự đang đứng trong sương.
///
/// CÁCH DÙNG:
///   Gắn script này vào một GameObject trong scene. Nhấn Play là có sương.
///   Không cần material — script tự tạo bằng shader có sẵn trong project.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class VolumetricFog : MonoBehaviour
{
    [Header("Vùng phủ sương")]
    [Tooltip("Bán kính vùng sương theo chiều ngang (X, Z)")]
    public float areaRadius = 60f;
    [Tooltip("Chiều cao vùng sương")]
    public float areaHeight = 12f;
    [Tooltip("Chiều cao bắt đầu của sương tính từ Y của GameObject")]
    public float heightOffset = 0f;

    [Header("Mật độ & số lượng")]
    [Tooltip("Số hạt sương tối đa cùng lúc (càng nhiều càng dày nhưng tốn GPU)")]
    public int   maxParticles = 200;
    [Tooltip("Kích thước mỗi hạt sương (Unity units)")]
    [Range(5f, 80f)]
    public float particleSize = 30f;
    [Tooltip("Tốc độ sinh hạt (hạt/giây)")]
    public float emissionRate = 40f;

    [Header("Độ trong suốt")]
    [Tooltip("Alpha tối đa của từng hạt (0 = vô hình, 1 = đặc)")]
    [Range(0f, 1f)]
    public float maxAlpha = 0.07f;

    [Header("Màu sắc")]
    [Tooltip("Màu sương — gần màu skybox để trông tự nhiên")]
    public Color fogColor = new Color(0.80f, 0.82f, 0.85f, 1f);

    [Header("Chuyển động")]
    [Tooltip("Tốc độ trôi ngang tối đa của từng hạt")]
    public float driftSpeed = 0.3f;

    [Header("Bám theo Camera")]
    [Tooltip("Bật để sương luôn bao quanh camera khi player di chuyển")]
    public bool followCamera = true;

    private ParticleSystem _ps;
    private Camera _cam;

    void Awake()
    {
        _cam = Camera.main;
        BuildParticleSystem();
    }

    void Update()
    {
        if (followCamera && _cam != null)
        {
            Vector3 cp = _cam.transform.position;
            transform.position = new Vector3(cp.x, transform.position.y, cp.z);
        }
    }

    void BuildParticleSystem()
    {
        _ps = GetComponent<ParticleSystem>();

        // ── Renderer: dùng material additive mờ ──────────────────────────
        var rend = _ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode       = ParticleSystemRenderMode.Billboard;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows    = false;
        rend.sortingOrder      = 1;

        // Tìm shader mờ (transparent / additive)
        Shader sh = Shader.Find("Particles/Standard Unlit")
                 ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                 ?? Shader.Find("Sprites/Default")
                 ?? Shader.Find("Standard");

        Material mat = new Material(sh);
        // Blend mode: Alpha Blended (không phải additive — để sương trông tối hơn)
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 2); // Fade
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        Color c = fogColor;
        c.a = maxAlpha;
        mat.color = c;
        mat.renderQueue = 3001;
        rend.material = mat;

        // ── Main module ──────────────────────────────────────────────────
        var main            = _ps.main;
        main.loop           = true;
        main.prewarm        = true;
        main.startLifetime  = new ParticleSystem.MinMaxCurve(8f, 14f);
        main.startSpeed     = new ParticleSystem.MinMaxCurve(0f, driftSpeed);
        main.startSize      = new ParticleSystem.MinMaxCurve(particleSize * 0.7f, particleSize);
        main.maxParticles   = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;

        // Màu hạt: fade in → hold → fade out
        var startColor = fogColor;
        startColor.a   = maxAlpha;
        main.startColor = startColor;

        // ── Emission ─────────────────────────────────────────────────────
        var em       = _ps.emission;
        em.enabled   = true;
        em.rateOverTime = emissionRate;

        // ── Shape: cylinder bao phủ vùng rộng ───────────────────────────
        var shape    = _ps.shape;
        shape.enabled       = true;
        shape.shapeType     = ParticleSystemShapeType.Box;
        shape.scale         = new Vector3(areaRadius * 2f, areaHeight, areaRadius * 2f);
        shape.position      = new Vector3(0f, heightOffset + areaHeight * 0.5f, 0f);
        shape.randomDirectionAmount = 1f;

        // ── Velocity over lifetime: trôi ngang chậm ─────────────────────
        var vel      = _ps.velocityOverLifetime;
        vel.enabled  = true;
        vel.space    = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-driftSpeed, driftSpeed);
        vel.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
        vel.z = new ParticleSystem.MinMaxCurve(-driftSpeed, driftSpeed);

        // ── Color over lifetime: fade in → hold → fade out ───────────────
        var col      = _ps.colorOverLifetime;
        col.enabled  = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(fogColor, 0f),
                new GradientColorKey(fogColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f,       0f),
                new GradientAlphaKey(maxAlpha, 0.15f),
                new GradientAlphaKey(maxAlpha, 0.85f),
                new GradientAlphaKey(0f,       1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // ── Size over lifetime: hạt to dần rồi nhỏ lại ──────────────────
        var sz       = _ps.sizeOverLifetime;
        sz.enabled   = true;
        AnimationCurve szCurve = new AnimationCurve(
            new Keyframe(0f,    0.3f),
            new Keyframe(0.2f,  1f),
            new Keyframe(0.8f,  1f),
            new Keyframe(1f,    0.3f));
        sz.size = new ParticleSystem.MinMaxCurve(1f, szCurve);

        // ── Rotation over lifetime: xoay nhẹ để tránh cứng ──────────────
        var rot      = _ps.rotationOverLifetime;
        rot.enabled  = true;
        rot.z        = new ParticleSystem.MinMaxCurve(-3f, 3f);

        _ps.Play();
    }
}
