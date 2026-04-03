using UnityEngine;

/// <summary>
/// Tạo hiệu ứng sương mù động (particles bay, kiểu huyền bí/fantasy) cho scene UI_Game.
/// Gắn script này vào Main Camera hoặc một GameObject trống trong scene.
/// Script tự tạo 3 lớp particle: 
///   1) Sương mù dày (ground fog) - bay ngang chậm
///   2) Sương mù nhẹ (ambient mist) - bay lơ lửng  
///   3) Đom đóm phát sáng (fireflies) - bay ngẫu nhiên
/// </summary>
public class MenuFogParticle : MonoBehaviour
{
    [Header("=== SỐ LƯỢNG PARTICLES ===")]
    [Tooltip("Ground fog particles")]
    [SerializeField] private int groundFogCount = 80;
    [Tooltip("Ambient mist particles")]
    [SerializeField] private int ambientMistCount = 40;
    [Tooltip("Glowing firefly particles")]
    [SerializeField] private int fireflyCount = 25;

    [Header("=== MÀU SẮC ===")]
    [SerializeField] private Color fogColorStart = new Color(0.6f, 0.65f, 0.8f, 0.15f);   // Xanh nhạt mờ
    [SerializeField] private Color fogColorEnd = new Color(0.4f, 0.45f, 0.7f, 0.05f);       // Xanh tím mờ
    [SerializeField] private Color mistColorStart = new Color(0.7f, 0.75f, 0.9f, 0.08f);    // Trắng xanh nhạt
    [SerializeField] private Color mistColorEnd = new Color(0.5f, 0.55f, 0.8f, 0.02f);      // Mờ dần
    [SerializeField] private Color fireflyColor = new Color(1f, 0.9f, 0.5f, 0.8f);          // Vàng ấm phát sáng

    [Header("=== KÍCH THƯỚC VÙNG SPAWN ===")]
    [SerializeField] private Vector3 fogArea = new Vector3(30f, 3f, 20f);
    [SerializeField] private Vector3 fogOffset = new Vector3(0f, -2f, 8f);   // Dịch xuống dưới + về phía trước camera

    [Header("=== TỐC ĐỘ ===")]
    [SerializeField] private float groundFogSpeed = 0.8f;
    [SerializeField] private float ambientMistSpeed = 0.3f;
    [SerializeField] private float fireflySpeed = 0.5f;

    [Header("=== BẬT/TẮT TỪNG LỚP ===")]
    [SerializeField] private bool enableGroundFog = true;
    [SerializeField] private bool enableAmbientMist = true;
    [SerializeField] private bool enableFireflies = true;

    private ParticleSystem groundFogPS;
    private ParticleSystem ambientMistPS;
    private ParticleSystem fireflyPS;

    private void Awake()
    {
        if (enableGroundFog)
            groundFogPS = CreateGroundFog();
        if (enableAmbientMist)
            ambientMistPS = CreateAmbientMist();
        if (enableFireflies)
            fireflyPS = CreateFireflies();
    }

    /// <summary>
    /// Lớp 1: Sương mù dày bay ngang — tạo cảm giác ground fog huyền bí
    /// </summary>
    private ParticleSystem CreateGroundFog()
    {
        var go = new GameObject("GroundFog");
        go.transform.SetParent(transform);
        go.transform.localPosition = fogOffset;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.maxParticles = groundFogCount;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(groundFogSpeed * 0.5f, groundFogSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startColor = new ParticleSystem.MinMaxGradient(fogColorStart, fogColorEnd);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.loop = true;
        main.gravityModifier = -0.02f; // bay lên nhẹ

        // Spawn rate
        var emission = ps.emission;
        emission.rateOverTime = groundFogCount / 10f;

        // Spawn trong hình hộp
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = fogArea;

        // Mờ dần theo thời gian
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 0.3f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // Xoay chậm
        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

        // Size tăng dần
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(0.8f, new AnimationCurve(
            new Keyframe(0f, 0.6f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 1.2f)
        ));

        // Renderer setup
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateFogMaterial();
        renderer.sortingOrder = -1;

        return ps;
    }

    /// <summary>
    /// Lớp 2: Sương mù nhẹ bay lơ lửng — tạo bầu không khí mờ ảo
    /// </summary>
    private ParticleSystem CreateAmbientMist()
    {
        var go = new GameObject("AmbientMist");
        go.transform.SetParent(transform);
        go.transform.localPosition = fogOffset + new Vector3(0, 2f, 0);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.maxParticles = ambientMistCount;
        main.startLifetime = new ParticleSystem.MinMaxCurve(12f, 20f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(ambientMistSpeed * 0.3f, ambientMistSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(5f, 12f);
        main.startColor = new ParticleSystem.MinMaxGradient(mistColorStart, mistColorEnd);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.loop = true;
        main.gravityModifier = 0.01f;

        var emission = ps.emission;
        emission.rateOverTime = ambientMistCount / 15f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = fogArea * 1.2f;

        // Fade in/out
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.6f, 0.3f),
                new GradientAlphaKey(0.6f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // Noise — chuyển động hỗn loạn nhẹ
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.5f);
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.1f;
        noise.octaveCount = 2;

        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateFogMaterial();
        renderer.sortingOrder = -2;

        return ps;
    }

    /// <summary>
    /// Lớp 3: Đom đóm phát sáng — điểm nhấn huyền bí fantasy
    /// </summary>
    private ParticleSystem CreateFireflies()
    {
        var go = new GameObject("Fireflies");
        go.transform.SetParent(transform);
        go.transform.localPosition = fogOffset + new Vector3(0, 1f, 0);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.maxParticles = fireflyCount;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(fireflySpeed * 0.2f, fireflySpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = fireflyColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.loop = true;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = fireflyCount / 5f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = fogArea * 0.8f;

        // Nhấp nháy — alpha lên xuống
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(fireflyColor, 0f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(fireflyColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(0.3f, 0.5f),
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        // Noise — bay ngẫu nhiên như đom đóm thật
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(1.5f);
        noise.frequency = 0.8f;
        noise.scrollSpeed = 0.5f;
        noise.octaveCount = 3;

        // Size nhấp nháy
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.25f, 1f),
            new Keyframe(0.5f, 0.6f),
            new Keyframe(0.75f, 1f),
            new Keyframe(1f, 0.3f)
        ));

        // Renderer — dùng material additive phát sáng
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateGlowMaterial();
        renderer.sortingOrder = 1;

        return ps;
    }

    /// <summary>
    /// Tạo material mờ cho sương mù (Soft Additive)
    /// </summary>
    private Material CreateFogMaterial()
    {
        // Dùng particle shader có sẵn trong Unity
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Mobile/Particles/Alpha Blended");

        var mat = new Material(shader);
        mat.SetFloat("_Surface", 0); // Transparent
        mat.SetFloat("_Blend", 0);   // Alpha
        
        // Tạo texture tròn mờ bằng code
        mat.mainTexture = CreateSoftCircleTexture(128);
        
        // Enable transparency
        mat.SetFloat("_Mode", 2); // Fade
        mat.renderQueue = 3000;
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");

        return mat;
    }

    /// <summary>
    /// Tạo material phát sáng cho đom đóm (Additive)
    /// </summary>
    private Material CreateGlowMaterial()
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Mobile/Particles/Additive");

        var mat = new Material(shader);
        mat.mainTexture = CreateSoftCircleTexture(64);
        
        // Additive blending — phát sáng
        mat.renderQueue = 3100;
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");

        return mat;
    }

    /// <summary>
    /// Tạo texture hình tròn mềm (soft circle) bằng code — không cần import asset
    /// </summary>
    private Texture2D CreateSoftCircleTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float maxDist = center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(1f - (dist / maxDist));
                // Smooth falloff — mờ dần từ tâm ra ngoài
                alpha = alpha * alpha * alpha;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    /// <summary>
    /// Public API — bật/tắt từng lớp runtime
    /// </summary>
    public void SetGroundFogActive(bool active)
    {
        if (groundFogPS != null) 
        {
            if (active) groundFogPS.Play();
            else groundFogPS.Stop();
        }
    }

    public void SetAmbientMistActive(bool active)
    {
        if (ambientMistPS != null)
        {
            if (active) ambientMistPS.Play();
            else ambientMistPS.Stop();
        }
    }

    public void SetFirefliesActive(bool active)
    {
        if (fireflyPS != null)
        {
            if (active) fireflyPS.Play();
            else fireflyPS.Stop();
        }
    }
}
