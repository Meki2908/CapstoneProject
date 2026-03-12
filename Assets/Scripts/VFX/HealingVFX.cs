using UnityEngine;

/// <summary>
/// Hiệu ứng hồi máu hoành tráng: Vòng tròn phép thuật + tia sáng + hạt xoáy
/// Gọi HealingVFX.Play(player) để kích hoạt
/// </summary>
public class HealingVFX : MonoBehaviour
{
    [Header("=== Thời gian ===")]
    [SerializeField] private float duration = 3.5f;

    [Header("=== Màu sắc ===")]
    [SerializeField] private Color healColor = new Color(0.2f, 1f, 0.4f); // xanh lá sáng
    [SerializeField] private Color brightColor = new Color(0.6f, 1f, 0.8f); // trắng xanh

    private float timer;
    private GameObject spawnedPrefab;

    /// <summary>
    /// Play VFX không có prefab
    /// </summary>
    public static HealingVFX Play(Transform target)
    {
        return Play(target, null);
    }

    /// <summary>
    /// Play VFX + spawn DAX Heal prefab
    /// </summary>
    public static HealingVFX Play(Transform target, GameObject healPrefab)
    {
        var go = new GameObject("HealingVFX");
        go.transform.position = target.position;
        go.transform.SetParent(target, true); // follow player

        var vfx = go.AddComponent<HealingVFX>();
        vfx.CreateAllEffects();

        // Spawn DAX Heal prefab nếu có
        if (healPrefab != null)
        {
            vfx.spawnedPrefab = Instantiate(healPrefab, target.position, Quaternion.identity, target);
            Destroy(vfx.spawnedPrefab, vfx.duration + 0.5f);
        }

        return vfx;
    }

    private void CreateAllEffects()
    {
        // 1. Vòng tròn phép thuật dưới chân
        CreateMagicCircle();

        // 2. Tia sáng bắn lên trời
        CreateLightBeams();

        // 3. Hạt xoáy quanh người (spiral)
        CreateSpiralParticles();

        // 4. Bụi sáng bay lên nhẹ
        CreateRisingSparkles();

        // 5. Point light xanh
        CreateHealLight();

        // 6. Flash burst ban đầu
        CreateInitialBurst();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }

    // ==========================================
    // 1. VÒNG TRÒN PHÉP THUẬT (Magic Circle)
    // ==========================================
    private void CreateMagicCircle()
    {
        // 1a. Outer Ring — viền vòng tròn ngoài, hạt chạy theo vòng
        CreateCircleRing("OuterRing", 1.6f, 0.02f, 0.045f, 50, 3.5f, healColor);

        // 1b. Inner Ring — viền vòng tròn trong, xoay ngược chiều
        CreateCircleRing("InnerRing", 1.0f, 0.015f, 0.03f, 35, -5f, brightColor);

        // 1c. Ground Glow — vầng sáng lớn trong suốt trên mặt đất
        CreateGroundGlow();

        // 1d. Rune Dots — các chấm sáng đứng yên trên viền tròn
        CreateRuneDots();
    }

    /// <summary>
    /// Tạo 1 vòng ring — hạt chạy orbital quanh trục Y
    /// </summary>
    private void CreateCircleRing(string name, float radius, float minSize, float maxSize, int count, float orbitSpeed, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0, 0.08f, 0);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = duration;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.maxParticles = count;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        // Màu
        main.startColor = new ParticleSystem.MinMaxGradient(col, brightColor);

        // Orbit xoay quanh trục Y
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.orbitalY = orbitSpeed;
        velocity.radial = 0f;

        // Alpha fade in/out
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.8f, 0.1f),
                new GradientAlphaKey(0.7f, 0.7f),
                new GradientAlphaKey(0f, 0.95f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // Size pulse nhẹ
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0, 0f),
            new Keyframe(0.1f, 1f),
            new Keyframe(0.5f, 0.7f),
            new Keyframe(0.8f, 1f),
            new Keyframe(1f, 0f)
        ));

        // Emission: burst tất cả cùng lúc
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        // Shape: circle edge (chỉ sinh trên viền tròn)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 0f; // chỉ trên viền, không random bên trong
        go.transform.localRotation = Quaternion.Euler(-90f, 0, 0);

        // Noise nhẹ cho lung linh
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.015f;
        noise.frequency = 3f;

        // Material
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMat(col);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.minParticleSize = 0.001f;
    }

    /// <summary>
    /// Vầng sáng lớn trong suốt nằm trên mặt đất
    /// </summary>
    private void CreateGroundGlow()
    {
        var go = new GameObject("GroundGlow");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0, 0.03f, 0);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = duration;
        main.startSpeed = 0f;
        main.startSize = 4f;
        main.maxParticles = 1;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startRotation = 0f;

        // Xoay liên tục rất chậm
        var rotOverLifetime = ps.rotationOverLifetime;
        rotOverLifetime.enabled = true;
        rotOverLifetime.z = new ParticleSystem.MinMaxCurve(0.5f);

        // Màu: rất trong suốt
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(healColor, 0f),
                new GradientColorKey(brightColor, 0.5f),
                new GradientColorKey(healColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.15f, 0.12f),  // rất nhẹ
                new GradientAlphaKey(0.12f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // Size: phình ra rồi thu
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0, 0f),
            new Keyframe(0.12f, 1f),
            new Keyframe(0.85f, 1.05f),
            new Keyframe(1f, 0f)
        ));

        // Emit 1 hạt
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        // Render nằm ngang
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMat(healColor);
        renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
    }

    /// <summary>
    /// Các chấm rune sáng đứng yên trên viền tròn ngoài
    /// </summary>
    private void CreateRuneDots()
    {
        var go = new GameObject("RuneDots");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0, 0.1f, 0);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = duration;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.maxParticles = 8;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        // Màu: sáng hơn ring
        main.startColor = new ParticleSystem.MinMaxGradient(brightColor, Color.white);

        // Nhấp nháy (pulse alpha)
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.9f, 0.1f),
                new GradientAlphaKey(0.3f, 0.4f),
                new GradientAlphaKey(0.9f, 0.6f),
                new GradientAlphaKey(0.3f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // Size pulse
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0, 0f),
            new Keyframe(0.08f, 1f),
            new Keyframe(0.3f, 0.6f),
            new Keyframe(0.5f, 1.2f),
            new Keyframe(0.7f, 0.6f),
            new Keyframe(0.9f, 1f),
            new Keyframe(1f, 0f)
        ));

        // Emit trên viền tròn
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1.6f;
        shape.radiusThickness = 0f;
        go.transform.localRotation = Quaternion.Euler(-90f, 0, 0);

        // Material
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMat(brightColor);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.minParticleSize = 0.002f;
    }

    // ==========================================
    // 2. TIA SÁNG BẮN LÊN (Light Beams)
    // ==========================================
    private void CreateLightBeams()
    {
        var go = new GameObject("LightBeams");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 5f); // bắn lên mạnh
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.3f; // bay lên thêm

        // Màu
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(brightColor, 0.3f),
                new GradientColorKey(healColor, 0.7f),
                new GradientColorKey(healColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0.6f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // Size fade
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0, 0.5f),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 0f)
        ));

        // Emission: burst lúc đầu
        var emission = ps.emission;
        emission.rateOverTime = 15f;
        emission.SetBursts(new[] {
            new ParticleSystem.Burst(0.1f, 15, 25),
            new ParticleSystem.Burst(0.5f, 8, 12)
        });

        // Shape: vòng tròn hẹp dưới chân → bắn lên
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.9f;
        // Hướng lên trên
        go.transform.localRotation = Quaternion.Euler(-90f, 0, 0);

        // Trail
        var trails = ps.trails;
        trails.enabled = true;
        trails.lifetime = 0.3f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0, 1f), new Keyframe(1, 0f)
        ));
        trails.colorOverLifetime = new ParticleSystem.MinMaxGradient(healColor, Color.clear);
        trails.dieWithParticles = true;

        // Material
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMat(brightColor);
        renderer.trailMaterial = CreateParticleMat(healColor);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.minParticleSize = 0.001f;

        // Tự dừng emit
        Destroy(go, duration - 0.3f);
    }

    // ==========================================
    // 3. HẠT XOÁY QUANH NGƯỜI (Spiral)
    // ==========================================
    private void CreateSpiralParticles()
    {
        var go = new GameObject("Spiral");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0, 1f, 0); // ngang thân

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        // Màu xanh → trắng
        main.startColor = new ParticleSystem.MinMaxGradient(healColor, brightColor);

        // Orbit xoáy — tất cả dùng constant mode
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.orbitalX = 0f;
        velocity.orbitalY = 4f;
        velocity.orbitalZ = 0f;
        velocity.radial = -0.3f;
        velocity.y = 1f; // bay lên dần (constant)

        // Size
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0, 0.3f),
            new Keyframe(0.3f, 1f),
            new Keyframe(0.7f, 0.6f),
            new Keyframe(1f, 0f)
        ));

        // Alpha
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.7f, 0.15f),
                new GradientAlphaKey(0.4f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // Emission
        var emission = ps.emission;
        emission.rateOverTime = 20f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.2f, 10, 15) });

        // Shape: sphere nhỏ
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        // Noise nhẹ
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.1f;
        noise.frequency = 1.5f;

        // Material
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMat(healColor);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.minParticleSize = 0.001f;

        Destroy(go, duration - 0.2f);
    }

    // ==========================================
    // 4. BỤI SÁNG BAY LÊN (Rising Sparkles)
    // ==========================================
    private void CreateRisingSparkles()
    {
        var go = new GameObject("RisingSparkles");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.025f);
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.2f;

        // Màu
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(brightColor, 0f),
                new GradientColorKey(healColor, 0.5f),
                new GradientColorKey(healColor * 0.5f, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.6f, 0.1f),
                new GradientAlphaKey(0.3f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // Emission
        var emission = ps.emission;
        emission.rateOverTime = 30f;

        // Shape: vòng tròn rộng quanh chân
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1.1f;
        go.transform.localRotation = Quaternion.Euler(-90f, 0, 0);

        // Noise
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
        noise.frequency = 2f;

        // Material
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMat(healColor);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.minParticleSize = 0.001f;

        Destroy(go, duration - 0.3f);
    }

    // ==========================================
    // 5. POINT LIGHT XANH
    // ==========================================
    private void CreateHealLight()
    {
        var go = new GameObject("HealLight");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0, 1f, 0);

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = healColor;
        light.intensity = 0f;
        light.range = 5f;

        // Animate: sáng lên rồi tắt
        StartCoroutine(AnimateLight(light));
    }

    private System.Collections.IEnumerator AnimateLight(Light light)
    {
        float maxIntensity = 3f;
        float fadeInTime = 0.3f;
        float holdTime = duration * 0.5f;
        float fadeOutTime = duration - fadeInTime - holdTime;

        // Fade in
        float t = 0;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            if (light != null)
                light.intensity = Mathf.Lerp(0, maxIntensity, t / fadeInTime);
            yield return null;
        }

        // Hold
        yield return new WaitForSeconds(holdTime);

        // Fade out
        t = 0;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            if (light != null)
                light.intensity = Mathf.Lerp(maxIntensity, 0, t / fadeOutTime);
            yield return null;
        }
    }

    // ==========================================
    // 6. FLASH BURST BAN ĐẦU
    // ==========================================
    private void CreateInitialBurst()
    {
        var go = new GameObject("InitialBurst");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0, 0.5f, 0);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Màu flash trắng → xanh
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, brightColor);

        // Size fade
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0, 1f), new Keyframe(0.3f, 0.5f), new Keyframe(1f, 0f)
        ));

        // Alpha fade
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0), new GradientColorKey(healColor, 1) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0), new GradientAlphaKey(0f, 1) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // Emission: 1 burst duy nhất
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20, 30) });

        // Shape: sphere
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        // Material
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMat(brightColor);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Destroy(go, 1f);
    }

    // ==========================================
    // MATERIAL HELPER
    // ==========================================
    private Material CreateParticleMat(Color col)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");

        var mat = new Material(shader);
        mat.color = col;

        // Gán texture tròn mềm (tránh hình vuông)
        mat.mainTexture = GetSoftCircleTexture();

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1); // Transparent
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 1); // Additive

        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One); // Additive
        mat.renderQueue = 3000;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", col);
        if (mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", GetSoftCircleTexture());
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", col * 2f);
            mat.EnableKeyword("_EMISSION");
        }

        return mat;
    }

    // Cache texture tĩnh — chỉ tạo 1 lần
    private static Texture2D _softCircleTex;

    /// <summary>
    /// Tạo texture tròn gradient mềm (trắng ở tâm, trong suốt ở viền)
    /// </summary>
    private static Texture2D GetSoftCircleTexture()
    {
        if (_softCircleTex != null) return _softCircleTex;

        int size = 64;
        _softCircleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float maxRadius = center;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / maxRadius;

                // Soft circle: alpha giảm dần từ tâm ra ngoài
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha; // mềm hơn (quadratic falloff)

                _softCircleTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        _softCircleTex.Apply();
        _softCircleTex.wrapMode = TextureWrapMode.Clamp;
        _softCircleTex.filterMode = FilterMode.Bilinear;
        return _softCircleTex;
    }
}
