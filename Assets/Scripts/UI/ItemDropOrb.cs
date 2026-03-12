using UnityEngine;

/// <summary>
/// Orb item rơi từ quái — Genshin-style 3 giai đoạn:
/// 1. Bay tung ra từ vị trí quái chết (scatter)
/// 2. Lơ lửng tại chỗ (hover/bob)
/// 3. Khi player đến gần → hút về player (magnet) → nhặt → hiện notification
/// </summary>
public class ItemDropOrb : MonoBehaviour
{
    [Header("=== Item Data ===")]
    public string itemName = "Unknown";
    public Sprite itemIcon;
    public ItemRarity rarity = ItemRarity.Common;
    public int quantity = 1;
    [Tooltip("Item ScriptableObject từ inventory system (null = chỉ hiện notification, không thêm vào inventory)")]
    public Item itemSO;
    [HideInInspector]
    public Rarity runtimeRarity = Rarity.Common; // Runtime rarity cho inventory

    [Header("=== Movement ===")]
    [SerializeField] private float scatterForce = 3f;
    [SerializeField] private float scatterUpForce = 2.5f;
    [SerializeField] private float hoverHeight = 0.3f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobAmount = 0.1f;
    [SerializeField] private float rotateSpeed = 90f;

    [Header("=== Magnet ===")]
    [SerializeField] private float magnetRadius = 4f;
    [SerializeField] private float magnetSpeed = 4f;
    [SerializeField] private float pickupRadius = 0.8f;

    [Header("=== Lifetime ===")]
    [SerializeField] private float scatterDuration = 0.5f;
    [SerializeField] private float maxLifetime = 30f;

    [Header("=== Visual ===")]
    [SerializeField] private float glowIntensity = 2f;
    [Tooltip("Kéo Material thủ công vào đây (null = tự tạo)")]
    [SerializeField] private Material customOrbMaterial;

    // Internal
    private enum OrbState { Scatter, Hover, Magnet }
    private OrbState state = OrbState.Scatter;
    private Vector3 velocity;
    private float stateTimer;
    private float lifetime;
    private Transform playerTransform;
    private Vector3 hoverPosition;
    private Light orbLight;
    private Renderer orbRenderer;

    private void Start()
    {
        // Tìm player
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            // Fallback: tìm PlayerHealth component
            var ph = FindObjectOfType<PlayerHealth>();
            if (ph != null) player = ph.gameObject;
        }
        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogWarning("[ItemDropOrb] Không tìm thấy Player! Orb sẽ không bị hút.");

        // Random scatter direction
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        velocity = new Vector3(randomDir.x * scatterForce, scatterUpForce, randomDir.y * scatterForce);

        // Setup visual
        SetupVisual();

        // Auto-destroy after maxLifetime
        Destroy(gameObject, maxLifetime);
    }

    private void Update()
    {
        lifetime += Time.deltaTime;
        stateTimer += Time.deltaTime;

        switch (state)
        {
            case OrbState.Scatter:
                UpdateScatter();
                break;
            case OrbState.Hover:
                UpdateHover();
                break;
            case OrbState.Magnet:
                UpdateMagnet();
                break;
        }

        // Xoay orb
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    // === GIAI ĐOẠN 1: Bay tung ra ===
    private void UpdateScatter()
    {
        // Gravity
        velocity.y -= 9.8f * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        // Chạm đất → dừng scatter
        if (stateTimer > 0.1f && transform.position.y <= hoverHeight)
        {
            Vector3 pos = transform.position;
            pos.y = hoverHeight;
            transform.position = pos;
            hoverPosition = pos;

            state = OrbState.Hover;
            stateTimer = 0f;
        }

        // Timeout → force hover
        if (stateTimer > scatterDuration)
        {
            hoverPosition = transform.position;
            hoverPosition.y = Mathf.Max(hoverPosition.y, hoverHeight);

            state = OrbState.Hover;
            stateTimer = 0f;
        }
    }

    // === GIAI ĐOẠN 2: Lơ lửng tại chỗ ===
    private void UpdateHover()
    {
        // Bob lên xuống
        float bob = Mathf.Sin(stateTimer * bobSpeed) * bobAmount;
        transform.position = hoverPosition + Vector3.up * bob;

        // Kiểm tra player gần → chuyển sang magnet
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist < magnetRadius)
            {
                state = OrbState.Magnet;
                stateTimer = 0f;
            }
        }
    }

    // === GIAI ĐOẠN 3: Hút về player ===
    private void UpdateMagnet()
    {
        if (playerTransform == null) return;

        // Điểm đích: giữa thân player
        Vector3 targetPoint = playerTransform.position + Vector3.up * 1f;

        // Bay về player với tốc độ tăng dần
        Vector3 dir = (targetPoint - transform.position).normalized;
        float speed = magnetSpeed * (1f + stateTimer * 3f); // Tăng tốc
        transform.position += dir * speed * Time.deltaTime;

        // Đo khoảng cách từ CÙNG điểm đích
        float dist = Vector3.Distance(transform.position, targetPoint);

        // Thu nhỏ khi gần
        float baseScale = 0.3f;
        float scale = Mathf.Clamp(dist / 2f, 0.1f, 1f) * baseScale;
        transform.localScale = Vector3.one * scale;

        // Pickup!
        if (dist < pickupRadius)
        {
            OnPickup();
        }
    }

    // === NHẶT ITEM ===
    private void OnPickup()
    {
        // 1. Thêm vào inventory nếu có Item SO — dùng runtime rarity
        if (itemSO != null && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(itemSO, quantity, runtimeRarity);
            Debug.Log($"[ItemDrop] Added to inventory: {itemSO.itemName} [{runtimeRarity}] ×{quantity}");
        }

        // 2. Hiện notification
        if (ItemPickupNotification.Instance != null)
        {
            Sprite icon = itemIcon != null ? itemIcon : (itemSO != null ? itemSO.icon : null);
            ItemPickupNotification.Instance.ShowNotification(itemName, icon, rarity, quantity);
        }

        Debug.Log($"[ItemDrop] Picked up: {itemName} [{runtimeRarity}] ×{quantity}");
        Destroy(gameObject);
    }

    // === VISUAL ===
    private void SetupVisual()
    {
        Color rarityCol = GetRarityLightColor();

        // Nếu chưa có mesh → tạo sphere đơn giản
        if (GetComponent<MeshFilter>() == null)
        {
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateOrbMesh();

            orbRenderer = gameObject.AddComponent<MeshRenderer>();
            orbRenderer.material = customOrbMaterial != null 
                ? new Material(customOrbMaterial)  // clone để thay đổi emission mà không ảnh hưởng gốc
                : CreateOrbMaterial();

            // Nếu dùng custom material → set emission theo rarity
            if (customOrbMaterial != null)
            {
                Color col = GetRarityLightColor();
                if (orbRenderer.material.HasProperty("_EmissionColor"))
                {
                    orbRenderer.material.SetColor("_EmissionColor", col * glowIntensity);
                    orbRenderer.material.EnableKeyword("_EMISSION");
                }
                if (orbRenderer.material.HasProperty("_BaseColor"))
                {
                    Color baseCol = new Color(col.r, col.g, col.b, orbRenderer.material.color.a);
                    orbRenderer.material.SetColor("_BaseColor", baseCol);
                }
            }
        }
        else
        {
            orbRenderer = GetComponent<Renderer>();
        }

        // === CƯỜNG ĐỘ THEO RARITY ===
        float rarityIntensity = GetRarityIntensity(); // 0.5 → 2.0

        // Point light — rarity cao = sáng + rộng hơn
        if (GetComponentInChildren<Light>() == null)
        {
            var lightGO = new GameObject("OrbLight");
            lightGO.transform.SetParent(transform, false);
            orbLight = lightGO.AddComponent<Light>();
            orbLight.type = LightType.Point;
            orbLight.range = 2f + rarityIntensity * 1.5f;        // Common=2.75, Legendary=5
            orbLight.intensity = glowIntensity * rarityIntensity;  // sáng hơn
            orbLight.color = rarityCol;
        }

        // === SPARKLE — rarity cao = nhiều + to hơn ===
        CreateSparkleParticles(rarityCol, rarityIntensity);

        // === ORBIT PARTICLES — vòng xoáy quanh orb ===
        CreateOrbitParticles(rarityCol, rarityIntensity);

        // === TRAIL PARTICLES — vệt sáng khi bay ===
        CreateTrailParticles(rarityCol, rarityIntensity);
    }

    /// <summary>
    /// Sparkle: bụi sáng siêu mịn bay lên xung quanh orb
    /// </summary>
    private void CreateSparkleParticles(Color col, float intensity)
    {
        var sparkleGO = new GameObject("Sparkle");
        sparkleGO.transform.SetParent(transform, false);

        var ps = sparkleGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f * intensity);
        main.startSize = new ParticleSystem.MinMaxCurve(0.005f, 0.035f * intensity); // SIÊU NHỎ
        main.maxParticles = (int)(60 + 80 * intensity); // RẤT NHIỀU
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.08f; // bay lên cực nhẹ

        // Màu mềm: sáng rực → trắng mờ → tắt
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        Color brightCol = Color.Lerp(col, Color.white, 0.5f);
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(brightCol, 0f),
                new GradientColorKey(Color.white, 0.2f),
                new GradientColorKey(col, 0.6f),
                new GradientColorKey(brightCol, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.8f, 0.1f),
                new GradientAlphaKey(0.5f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        // Size: mượt mà
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0, 0.2f),
            new Keyframe(0.15f, 1f),
            new Keyframe(0.7f, 0.4f),
            new Keyframe(1f, 0f)
        ));

        // Emission: liên tục + burst mịn
        var emission = ps.emission;
        emission.rateOverTime = 30f + 40f * intensity;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 5, 10, -1, 0.3f)
        });

        // Shape: sphere mỏng
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.18f;

        // Noise: chuyển động lung linh mềm
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.15f;
        noise.frequency = 2f;
        noise.scrollSpeed = 0.5f;

        // Material
        var renderer = sparkleGO.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial(col);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.minParticleSize = 0.001f; // cho phép hạt siêu nhỏ
    }

    /// <summary>
    /// Orbit: bụi sáng xoay mịn quanh orb
    /// </summary>
    private void CreateOrbitParticles(Color col, float intensity)
    {
        var orbitGO = new GameObject("Orbit");
        orbitGO.transform.SetParent(transform, false);

        var ps = orbitGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 2f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.008f * intensity, 0.025f * intensity); // micro
        main.maxParticles = (int)(20 + 25 * intensity);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        // Màu mịn
        Color orbitCol = Color.Lerp(col, Color.white, 0.6f);
        main.startColor = new ParticleSystem.MinMaxGradient(orbitCol, Color.white);

        // Orbit mềm
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.orbitalX = 1.5f * intensity;
        velocityOverLifetime.orbitalY = 2.5f * intensity;
        velocityOverLifetime.orbitalZ = 1f * intensity;
        velocityOverLifetime.radial = -0.3f;

        // Size mềm
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0, 0.3f),
            new Keyframe(0.2f, 1f),
            new Keyframe(0.8f, 0.6f),
            new Keyframe(1f, 0f)
        ));

        // Alpha mượt
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.7f, 0.15f),
                new GradientAlphaKey(0.5f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        // Emission
        var emission = ps.emission;
        emission.rateOverTime = 12f + 8f * intensity;

        // Shape
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        // Noise nhẹ
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 1.5f;

        // Material
        var renderer = orbitGO.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial(col);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.minParticleSize = 0.001f;
    }

    /// <summary>
    /// Trail: sương sáng mịn kéo dài khi bay
    /// </summary>
    private void CreateTrailParticles(Color col, float intensity)
    {
        var trailGO = new GameObject("Trail");
        trailGO.transform.SetParent(transform, false);

        var ps = trailGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f + 0.2f * intensity);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.01f * intensity, 0.04f * intensity); // mịn
        main.maxParticles = (int)(40 + 50 * intensity);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Màu mềm
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.Lerp(col, Color.white, 0.4f), 0f),
                new GradientColorKey(col, 0.4f),
                new GradientColorKey(col * 0.5f, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.5f, 0f),
                new GradientAlphaKey(0.3f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        // Size mịn
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0, 1), new Keyframe(0.4f, 0.5f), new Keyframe(1, 0)
        ));

        // Emission dày đặc
        var emission = ps.emission;
        emission.rateOverTime = 25f + 30f * intensity;
        emission.rateOverDistance = 8f + 10f * intensity;

        // Shape
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.04f;

        // Material
        var renderer = trailGO.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial(col);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.minParticleSize = 0.001f;
    }

    /// <summary>
    /// Material additive cho particle
    /// </summary>
    private Material CreateParticleMaterial(Color col)
    {
        // URP particle shader
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");

        var mat = new Material(shader);
        mat.color = col;

        // Surface type: Transparent, Blend: Additive
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1); // Transparent
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 1); // Additive

        // Fallback blending
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3100;

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", col * glowIntensity);
            mat.EnableKeyword("_EMISSION");
        }

        return mat;
    }

    private Mesh CreateOrbMesh()
    {
        // Dùng Unity primitive sphere mesh
        var tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var mesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
        // Xóa collider tự tạo từ primitive
        var tempCol = tempSphere.GetComponent<Collider>();
        if (tempCol != null) DestroyImmediate(tempCol);
        Destroy(tempSphere);
        return mesh;
    }

    private Material CreateOrbMaterial()
    {
        Color col = GetRarityLightColor();

        // URP shader
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);

        // === TRANSPARENT + GLOW ===
        // Surface type: Transparent
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1); // 0=Opaque, 1=Transparent
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0); // 0=Alpha, 1=Additive

        // Alpha blending
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;

        // Màu semi-transparent (nhìn xuyên qua nhẹ)
        Color baseColor = new Color(col.r, col.g, col.b, 0.6f);
        mat.color = baseColor;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", baseColor);

        // Metallic + Smoothness cao → phản chiếu đẹp
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0.8f);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.95f);
        if (mat.HasProperty("_GlossMapScale"))
            mat.SetFloat("_GlossMapScale", 0.95f);

        // Emission mạnh → phát sáng rực
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", col * glowIntensity * 1.5f);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        return mat;
    }

    private Color GetRarityLightColor()
    {
        switch (rarity)
        {
            case ItemRarity.Common: return new Color(0.8f, 0.8f, 0.8f);
            case ItemRarity.Uncommon: return new Color(0.3f, 0.9f, 0.4f);
            case ItemRarity.Rare: return new Color(0.3f, 0.6f, 1f);
            case ItemRarity.Epic: return new Color(0.7f, 0.3f, 1f);
            case ItemRarity.Legendary: return new Color(1f, 0.75f, 0.2f);
            default: return Color.white;
        }
    }

    /// <summary>
    /// Hệ số cường độ particle theo rarity
    /// Common=nhẹ, Legendary=rực rỡ
    /// </summary>
    private float GetRarityIntensity()
    {
        switch (rarity)
        {
            case ItemRarity.Common: return 0.5f;
            case ItemRarity.Uncommon: return 0.8f;
            case ItemRarity.Rare: return 1.2f;
            case ItemRarity.Epic: return 1.6f;
            case ItemRarity.Legendary: return 2.0f;
            default: return 1f;
        }
    }

    // === PUBLIC: Cấu hình từ code ===
    public void Setup(string name, Sprite icon, ItemRarity itemRarity, int qty = 1)
    {
        itemName = name;
        itemIcon = icon;
        rarity = itemRarity;
        quantity = qty;
    }

    /// <summary>
    /// Setup từ Item ScriptableObject — tự lấy name, icon, rarity
    /// </summary>
    public void Setup(Item item, int qty = 1)
    {
        Setup(item, item.rarity, qty);
    }

    /// <summary>
    /// Setup với runtime rarity — rarity khác SO rarity
    /// </summary>
    public void Setup(Item item, Rarity rtRarity, int qty = 1)
    {
        itemSO = item;
        itemName = item.itemName;
        itemIcon = item.icon;
        quantity = qty;
        runtimeRarity = rtRarity;

        // Map Rarity → ItemRarity cho visual
        rarity = RarityToItemRarity(rtRarity);
    }

    /// <summary>
    /// Convert Rarity enum → ItemRarity enum
    /// </summary>
    public static ItemRarity RarityToItemRarity(Rarity r)
    {
        switch (r)
        {
            case Rarity.Common:    return ItemRarity.Common;
            case Rarity.Uncommon:  return ItemRarity.Uncommon;
            case Rarity.Epic:      return ItemRarity.Epic;
            case Rarity.Legendary: return ItemRarity.Legendary;
            case Rarity.Mythic:    return ItemRarity.Mythic;
            default:               return ItemRarity.Common;
        }
    }
}
