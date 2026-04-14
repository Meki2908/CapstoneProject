using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tập trung quản lý VFX Object Pooling và Animation Events cho Wolf Boss phase 2.
///
/// ── Gắn lên ROOT Vargr (cùng với WolfBossAI) ──
///
/// ═══ BOSS ANIMATION EVENTS ═══
/// Đặt các Animation Event sau trong Animator clips của Boss:
///   • SpawnUltimateVFX()         → clip "ulti" / "roar"
///   • SpawnSpecialVFX()          → clip "sa" (special attack)
///   • SpawnNormalAttackVFX(0/1)  → clip "na" (0=tay trái, 1=tay phải)
///
/// ═══ FANG ANIMATION EVENTS ═══
/// Các method sau được gọi từ FangAnimationRelay.cs gắn trên Fang prefabs:
///   • SpawnFireFangAutoVFX()  → auto-attack của Fire Fang
///   • SpawnIceFangAutoVFX()   → auto-attack của Ice Fang
///   • SpawnCombinedFangVFX()  → đòn kết hợp (chỉ khi CẢ 2 fang còn sống)
///
/// ═══ VFX OBJECT POOL ═══
/// Mỗi prefab VFX sẽ có pool riêng (Queue&lt;GameObject&gt;).
/// Instance được pre-warm lúc Awake, tự trả pool qua PooledVFXReturn.cs.
///
/// ═══ FANG DAMAGE RECEIVING ═══
/// Fangs nhận damage qua TakeDamageTest trên prefab của chúng (auto).
/// Script này theo dõi fang transforms qua RegisterFang/UnregisterFang
/// để biết vị trí spawn VFX và áp damage đúng chỗ.
/// </summary>
public class WolfBossVFXEvents : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════

    [Header("=== References ===")]
    [Tooltip("WolfBossAI trên Root. Tự tìm nếu để trống.")]
    [SerializeField] private WolfBossAI bossAI;

    // ── VFX Prefabs ────────────────────────────────────────────────────────

    [Header("=== Boss VFX Prefabs ===")]
    [Tooltip("VFX dùng cho animation Ultimate / Roar (phase 2 entry).")]
    [SerializeField] private GameObject ultimateVFXPrefab;

    [Tooltip("VFX dùng cho animation Special Attack (sa).")]
    [SerializeField] private GameObject specialVFXPrefab;

    [Tooltip("VFX Normal Attack — tay TRÁI (index 0).")]
    [SerializeField] private GameObject normalAttackLeftVFXPrefab;

    [Tooltip("VFX Normal Attack — tay PHẢI (index 1).")]
    [SerializeField] private GameObject normalAttackRightVFXPrefab;

    [Header("=== Fang VFX Prefabs ===")]
    [Tooltip("VFX auto-attack của Fire Fang.")]
    [SerializeField] private GameObject fireFangAutoVFXPrefab;

    [Tooltip("VFX auto-attack của Ice Fang.")]
    [SerializeField] private GameObject iceFangAutoVFXPrefab;

    [Tooltip("VFX đòn kết hợp khi CẢ 2 Fang còn sống (spawned ở trung điểm giữa 2 fang).")]
    [SerializeField] private GameObject combinedFangVFXPrefab;

    // ── Spawn Points ───────────────────────────────────────────────────────

    [Header("=== Spawn Points ===")]
    [Tooltip("Vị trí spawn VFX Ultimate. Để trống = root position.")]
    [SerializeField] private Transform ultimateVFXSpawnPoint;

    [Tooltip("Vị trí spawn VFX Special Attack. Để trống = root position.")]
    [SerializeField] private Transform specialVFXSpawnPoint;

    [Tooltip("Vị trí spawn VFX đòn kết hợp. Để trống = điểm giữa 2 fang.")]
    [SerializeField] private Transform combinedFangVFXSpawnPoint;

    // ── Fang Damage ────────────────────────────────────────────────────────

    [Header("=== Fang Attack Settings ===")]
    [Tooltip("Sát thương mỗi đòn Fire Fang auto-attack.")]
    [SerializeField] private float fireFangDamage = 15f;

    [Tooltip("Sát thương mỗi đòn Ice Fang auto-attack.")]
    [SerializeField] private float iceFangDamage = 12f;

    [Tooltip("Sát thương đòn kết hợp 2 Fang (lớn hơn đơn lẻ).")]
    [SerializeField] private float combinedFangDamage = 40f;

    [Tooltip("Bán kính OverlapSphere detect player khi Fang tấn công.")]
    [SerializeField] private float fangAttackRadius = 3f;

    [Tooltip("Bán kính đòn kết hợp Fang (thường lớn hơn đơn lẻ).")]
    [SerializeField] private float combinedFangAttackRadius = 5f;

    [Tooltip("Layer mask của Player để detect hit khi Fang attack.")]
    [SerializeField] private LayerMask playerLayer = ~0;

    // ── Pool ───────────────────────────────────────────────────────────────

    [Header("=== Pool Settings ===")]
    [Tooltip("Số instance khởi tạo trước (pre-warm) cho mỗi loại VFX.")]
    [SerializeField] private int defaultPoolSize = 3;

    [Tooltip("Thời gian tối đa một VFX instance được phép sống (seconds). Sau đó tự trả pool.")]
    [SerializeField] private float vfxMaxLifetime = 6f;

    // ── Debug ──────────────────────────────────────────────────────────────

    [Header("=== Debug ===")]
    [SerializeField] private bool showDebugLog = true;

    // ═══════════════════════════════════════════════════════════════════════
    //  RUNTIME
    // ═══════════════════════════════════════════════════════════════════════

    // Pool: key = prefab GameObject, value = queue của inactive instances
    private readonly Dictionary<GameObject, Queue<GameObject>> _pool =
        new Dictionary<GameObject, Queue<GameObject>>();

    // Container Transform giữ pool objects cho gọn Hierarchy
    private Transform _poolContainer;

    // Fang transforms — set qua RegisterFang / UnregisterFang
    private Transform _fireFangTransform;
    private Transform _iceFangTransform;

    // ═══════════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (bossAI == null)
        {
            bossAI = GetComponent<WolfBossAI>();
            if (bossAI == null) bossAI = GetComponentInParent<WolfBossAI>();
        }

        // Tạo container cho pooled objects (ở root hierarchy, không bị affected bởi boss transform)
        var containerGO = new GameObject("[WolfBoss VFX Pool]");
        DontDestroyOnLoad(containerGO);   // optional: giữ qua scene nếu cần
        _poolContainer = containerGO.transform;

        // Pre-warm pool cho tất cả prefabs được gán
        PrewarmPool(ultimateVFXPrefab);
        PrewarmPool(specialVFXPrefab);
        PrewarmPool(normalAttackLeftVFXPrefab);
        PrewarmPool(normalAttackRightVFXPrefab);
        PrewarmPool(fireFangAutoVFXPrefab);
        PrewarmPool(iceFangAutoVFXPrefab);
        PrewarmPool(combinedFangVFXPrefab);
    }

    private void OnDestroy()
    {
        // Dọn dẹp pool container khi boss bị destroy / scene unload
        if (_poolContainer != null)
            Destroy(_poolContainer.gameObject);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  BOSS ANIMATION EVENTS
    //  ── Đặt Animation Event trong Animator của Wolfboss_A ──
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// AE: Spawn VFX Ultimate / Roar.
    /// Đặt event vào frame hiệu ứng bùng phát trong clip "ulti" hoặc "roar".
    /// </summary>
    public void SpawnUltimateVFX()
    {
        if (ultimateVFXPrefab == null)
        {
            Log("[WolfBossVFXEvents] ultimateVFXPrefab chưa được gán!");
            return;
        }

        Vector3 pos = ultimateVFXSpawnPoint != null ? ultimateVFXSpawnPoint.position : transform.position;
        SpawnVFX(ultimateVFXPrefab, pos, transform.rotation);
        Log("[WolfBossVFXEvents] SpawnUltimateVFX ✓");
    }

    /// <summary>
    /// AE: Spawn VFX Special Attack.
    /// Đặt event vào frame tấn công trong clip "sa".
    /// </summary>
    public void SpawnSpecialVFX()
    {
        if (specialVFXPrefab == null)
        {
            Log("[WolfBossVFXEvents] specialVFXPrefab chưa được gán!");
            return;
        }

        Vector3 pos = specialVFXSpawnPoint != null ? specialVFXSpawnPoint.position : transform.position;
        SpawnVFX(specialVFXPrefab, pos, transform.rotation);
        Log("[WolfBossVFXEvents] SpawnSpecialVFX ✓");
    }

    /// <summary>
    /// AE: Spawn VFX Normal Attack.
    /// Đặt event vào frame trong clip "na".
    /// pawIndex: 0 = tay trái, 1 = tay phải.
    /// </summary>
    public void SpawnNormalAttackVFX(int pawIndex)
    {
        GameObject prefab = pawIndex == 0 ? normalAttackLeftVFXPrefab : normalAttackRightVFXPrefab;
        if (prefab == null)
        {
            Log($"[WolfBossVFXEvents] normalAttackVFXPrefab (paw {pawIndex}) chưa được gán!");
            return;
        }

        SpawnVFX(prefab, transform.position, transform.rotation);
        Log($"[WolfBossVFXEvents] SpawnNormalAttackVFX paw={pawIndex} ✓");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  FANG ANIMATION EVENTS
    //  ── Gọi từ FangAnimationRelay.cs trên từng Fang prefab ──
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Spawn VFX + gây damage diện tích cho đòn auto-attack của Fire Fang.
    /// Gọi qua FangAnimationRelay.OnAutoAttack() trên Fire Fang.
    /// </summary>
    public void SpawnFireFangAutoVFX()
    {
        if (_fireFangTransform == null)
        {
            Log("[WolfBossVFXEvents] SpawnFireFangAutoVFX — Fire Fang không còn tồn tại.");
            return;
        }

        if (fireFangAutoVFXPrefab != null)
            SpawnVFX(fireFangAutoVFXPrefab, _fireFangTransform.position, _fireFangTransform.rotation);

        DealFangDamage(_fireFangTransform.position, fireFangDamage, fangAttackRadius, "Fire Fang");
        Log("[WolfBossVFXEvents] SpawnFireFangAutoVFX ✓");
    }

    /// <summary>
    /// Spawn VFX + gây damage diện tích cho đòn auto-attack của Ice Fang.
    /// Gọi qua FangAnimationRelay.OnAutoAttack() trên Ice Fang.
    /// </summary>
    public void SpawnIceFangAutoVFX()
    {
        if (_iceFangTransform == null)
        {
            Log("[WolfBossVFXEvents] SpawnIceFangAutoVFX — Ice Fang không còn tồn tại.");
            return;
        }

        if (iceFangAutoVFXPrefab != null)
            SpawnVFX(iceFangAutoVFXPrefab, _iceFangTransform.position, _iceFangTransform.rotation);

        DealFangDamage(_iceFangTransform.position, iceFangDamage, fangAttackRadius, "Ice Fang");
        Log("[WolfBossVFXEvents] SpawnIceFangAutoVFX ✓");
    }

    /// <summary>
    /// Spawn VFX + damage cho đòn KẾT HỢP của 2 Fang.
    /// CHỈ có hiệu lực khi cả hai Fang còn sống trên sân.
    /// VFX spawn ở điểm giữa 2 Fang, radius và damage lớn hơn.
    /// </summary>
    public void SpawnCombinedFangVFX()
    {
        // Điều kiện: cả 2 Fang phải còn sống
        if (_fireFangTransform == null || _iceFangTransform == null)
        {
            Log("[WolfBossVFXEvents] SpawnCombinedFangVFX bị bỏ qua — không đủ 2 Fang trên sân.");
            return;
        }

        // Điểm spawn: giữa 2 fang (hoặc spawn point được chỉ định)
        Vector3 center = (_fireFangTransform.position + _iceFangTransform.position) * 0.5f;
        if (combinedFangVFXSpawnPoint != null)
            center = combinedFangVFXSpawnPoint.position;

        if (combinedFangVFXPrefab != null)
            SpawnVFX(combinedFangVFXPrefab, center, transform.rotation);

        DealFangDamage(center, combinedFangDamage, combinedFangAttackRadius, "Combined Fang");
        Log("[WolfBossVFXEvents] SpawnCombinedFangVFX ✓");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  FANG REGISTRATION API
    //  ── Gọi từ FangAnimationRelay khi Fang spawn / die ──
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Đăng ký một Fang vừa được spawn để script biết vị trí VFX / damage.
    /// Gọi trong FangAnimationRelay.Start().
    /// </summary>
    public void RegisterFang(BossFang fang)
    {
        if (fang == null) return;

        if (fang.Type == BossFang.FangType.FireFang)
            _fireFangTransform = fang.transform;
        else
            _iceFangTransform = fang.transform;

        Log($"[WolfBossVFXEvents] Registered {fang.Type} @ {fang.transform.position}");
    }

    /// <summary>
    /// Hủy đăng ký Fang khi nó chết/bị destroy.
    /// Gọi trong FangAnimationRelay.OnDestroy().
    /// </summary>
    public void UnregisterFang(BossFang fang)
    {
        if (fang == null) return;

        if (fang.Type == BossFang.FangType.FireFang)
            _fireFangTransform = null;
        else
            _iceFangTransform = null;

        Log($"[WolfBossVFXEvents] Unregistered {fang.Type}");
    }

    /// <summary>
    /// Kiểm tra công khai: cả 2 fang có còn sống không?
    /// </summary>
    public bool BothFangsAlive() => _fireFangTransform != null && _iceFangTransform != null;

    // ═══════════════════════════════════════════════════════════════════════
    //  DAMAGE HELPER
    // ═══════════════════════════════════════════════════════════════════════

    private void DealFangDamage(Vector3 center, float damage, float radius, string sourceName)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius, playerLayer);
        foreach (var col in hits)
        {
            var ph = col.GetComponentInParent<PlayerHealth>();
            if (ph == null) ph = col.GetComponent<PlayerHealth>();
            if (ph == null) continue;

            ph.TakeDamage(damage, center);
            Log($"[WolfBossVFXEvents] {sourceName} → {col.name} {damage} dmg");
            break; // Chỉ damage 1 player mỗi lần
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  OBJECT POOL IMPLEMENTATION
    // ═══════════════════════════════════════════════════════════════════════

    private void PrewarmPool(GameObject prefab)
    {
        if (prefab == null) return;
        if (_pool.ContainsKey(prefab)) return; // đã warm rồi

        var queue = new Queue<GameObject>(defaultPoolSize);
        _pool[prefab] = queue;

        for (int i = 0; i < defaultPoolSize; i++)
        {
            var inst = CreateInstance(prefab);
            queue.Enqueue(inst);
        }

        Log($"[WolfBossVFXEvents] Pool pre-warmed: {prefab.name} ×{defaultPoolSize}");
    }

    private GameObject CreateInstance(GameObject prefab)
    {
        var inst = Instantiate(prefab, Vector3.zero, Quaternion.identity, _poolContainer);
        inst.SetActive(false);

        // Gắn PooledVFXReturn để tự trả về pool
        var ret = inst.GetComponent<PooledVFXReturn>();
        if (ret == null) ret = inst.AddComponent<PooledVFXReturn>();
        ret.Init(prefab, this, vfxMaxLifetime);

        return inst;
    }

    /// <summary>Lấy VFX từ pool (hoặc tạo mới nếu pool cạn) và kích hoạt tại vị trí đã cho.</summary>
    private void SpawnVFX(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return;

        // Đảm bảo pool tồn tại
        if (!_pool.TryGetValue(prefab, out var queue))
        {
            PrewarmPool(prefab);
            _pool.TryGetValue(prefab, out queue);
        }

        GameObject inst;
        if (queue != null && queue.Count > 0)
        {
            inst = queue.Dequeue();
        }
        else
        {
            // Pool cạn — tạo thêm instance mới
            Log($"[WolfBossVFXEvents] Pool cạn cho {prefab.name}, tạo thêm instance.");
            inst = CreateInstance(prefab);
        }

        inst.transform.SetPositionAndRotation(position, rotation);
        inst.transform.SetParent(null); // ra ngoài pool container khi đang dùng
        inst.SetActive(true);
    }

    /// <summary>
    /// Trả một VFX instance về pool sau khi hết hiệu ứng.
    /// Được gọi bởi PooledVFXReturn.
    /// </summary>
    public void ReturnToPool(GameObject prefabKey, GameObject instance)
    {
        if (instance == null) return;

        instance.SetActive(false);
        instance.transform.SetParent(_poolContainer);

        if (_pool.TryGetValue(prefabKey, out var queue))
            queue.Enqueue(instance);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DEBUG
    // ═══════════════════════════════════════════════════════════════════════

    private void Log(string msg)
    {
        if (showDebugLog) Debug.Log(msg);
    }

    private void OnDrawGizmosSelected()
    {
        // Hiển thị bán kính attack của mỗi fang trong Scene View
        if (_fireFangTransform != null)
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);
            Gizmos.DrawWireSphere(_fireFangTransform.position, fangAttackRadius);
        }

        if (_iceFangTransform != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Gizmos.DrawWireSphere(_iceFangTransform.position, fangAttackRadius);
        }

        // Combined attack radius (điểm giữa 2 fang)
        if (_fireFangTransform != null && _iceFangTransform != null)
        {
            Vector3 center = (_fireFangTransform.position + _iceFangTransform.position) * 0.5f;
            if (combinedFangVFXSpawnPoint != null) center = combinedFangVFXSpawnPoint.position;
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.25f);
            Gizmos.DrawWireSphere(center, combinedFangAttackRadius);
        }
    }
}
