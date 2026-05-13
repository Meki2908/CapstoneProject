using UnityEngine;
using System.Collections.Generic;

public abstract class BaseEffectScript : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected bool debugMode = false;

    [Header("Damage Tick Settings")]
    [Tooltip("Thời gian giãn cách giữa 2 lần nhận sát thương. (Ví dụ: 0.5 = nửa giây giật máu 1 lần. Đặt = 0 nếu chiêu chỉ đánh 1 phát duy nhất).")]
    [SerializeField] protected float damageInterval = 0.5f;

    [Tooltip("Số lần tối đa 1 kẻ địch có thể nhận damage từ skill này (0 = không giới hạn). Dùng để tránh boss ăn full combo lốc xoáy từ đầu đến cuối.")]
    [SerializeField] protected int maxHitsPerEnemy = 0;

    private float baseDamage;
    private WeaponController weaponController;
    private EquipmentSystem equipmentSystem;

    private const float BASE_CRIT_MULTIPLIER = 1.5f;

    // BỘ NHỚ LƯU TRỮ LỊCH SỬ CHỊU ĐÒN CỦA TỪNG KẺ ĐỊCH
    private readonly Dictionary<int, float> _lastHitTime = new Dictionary<int, float>();
    private readonly Dictionary<int, int> _hitCount = new Dictionary<int, int>();

    protected virtual void Awake()
    {
        baseDamage = damage;
        weaponController = GetComponentInParent<WeaponController>();
        equipmentSystem = GetComponentInParent<EquipmentSystem>();
    }

    /// <summary>For projectiles/VFX instantiated without a parent chain to the caster.</summary>
    public void SetOwner(Character owner)
    {
        if (owner == null) return;
        weaponController = owner.GetComponentInChildren<WeaponController>();
        equipmentSystem = owner.GetComponentInChildren<EquipmentSystem>();
        UpdateDamageWithGems();
    }

    /// <summary>Assign caster to all networked damage helpers under a spawned root.</summary>
    public static void WireSpellOwnership(Character owner, GameObject spawnedRoot)
    {
        if (owner == null || spawnedRoot == null) return;
        foreach (var p in spawnedRoot.GetComponentsInChildren<ProjectileDamage>(true))
            p.SetOwner(owner);
        foreach (var e in spawnedRoot.GetComponentsInChildren<BaseEffectScript>(true))
            e.SetOwner(owner);
    }

    protected virtual void Start()
    {
        UpdateDamageWithGems();
    }

    private void UpdateDamageWithGems()
    {
        if (WeaponGemManager.Instance == null || weaponController == null)
        {
            damage = baseDamage;
            return;
        }

        WeaponSO currentWeapon = weaponController.GetCurrentWeapon();
        if (currentWeapon == null)
        {
            damage = baseDamage;
            return;
        }

        float damageMultiplier = WeaponGemManager.Instance.GetDamageMultiplier(currentWeapon.weaponType);
        float damagePercent = damageMultiplier - 1f;
        damage = baseDamage + (baseDamage * damagePercent);

        if (debugMode)
            Debug.Log($"[{GetType().Name}] Updated damage: {baseDamage} -> {damage} (x{damageMultiplier:F2})");
    }

    // ── Collision callbacks ───────────────────────────────────────────────────

    protected virtual void OnParticleCollision(GameObject other)
    {
        HandleCollision(other);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject);
    }

    /// <summary>
    /// Xử lý va chạm chung cho cả hạt và vật lý
    /// </summary>
    private void HandleCollision(GameObject other)
    {
        // 1. Xác định đối tượng bị trúng đòn
        TakeDamageTest enemy = other.GetComponent<TakeDamageTest>();
        if (enemy == null) enemy = other.GetComponentInParent<TakeDamageTest>();

        NetworkHealth netHealth = other.GetComponent<NetworkHealth>();
        if (netHealth == null) netHealth = other.GetComponentInParent<NetworkHealth>();

        if (enemy == null && netHealth == null) return;

        // 2. Lấy ID duy nhất của cục quái (Tránh bị tính là 2 kẻ địch nếu nó có 2 cục xương)
        int targetId = enemy != null ? enemy.gameObject.GetInstanceID() : netHealth.gameObject.GetInstanceID();

        // 3. KIỂM TRA SỐ LẦN HIT TỐI ĐA
        if (maxHitsPerEnemy > 0)
        {
            if (_hitCount.TryGetValue(targetId, out int hits) && hits >= maxHitsPerEnemy)
                return; // Đã dính đủ đòn, miễn nhiễm luôn
        }

        // 4. KIỂM TRA GIÃN CÁCH (COOLDOWN)
        if (damageInterval <= 0f)
        {
            // Nếu Interval = 0, Skill này chỉ gây sát thương ĐÚNG 1 LẦN trong đời (như hất tung)
            if (_lastHitTime.ContainsKey(targetId)) return;
        }
        else
        {
            // Nếu có Interval, kiểm tra xem đã qua đủ thời gian chưa
            if (_lastHitTime.TryGetValue(targetId, out float lastTime))
            {
                if (Time.time - lastTime < damageInterval) return; // Vẫn đang trong thời gian invul, bỏ qua
            }
        }

        // --- NẾU VƯỢT QUA ĐƯỢC CÁC BƯỚC TRÊN ---
        // Cập nhật lại lịch sử ăn đòn
        _lastHitTime[targetId] = Time.time;
        if (_hitCount.ContainsKey(targetId)) _hitCount[targetId]++;
        else _hitCount[targetId] = 1;

        // Thực thi Damage & Hiệu ứng
        if (enemy != null) ProcessHit(enemy);
        
        if (netHealth != null)
        {
            UpdateDamageWithGems();
            int finalDamage = Mathf.RoundToInt(damage);
            netHealth.TakeDamage(finalDamage);
        }
    }

    // ── Core processing ───────────────────────────────────────────────────────

    private void ProcessHit(TakeDamageTest enemy)
    {
        // Cập nhật damage theo gem (vũ khí có thể đổi trong runtime)
        UpdateDamageWithGems();

        WeaponType weaponType = WeaponType.None;
        if (weaponController != null && weaponController.GetCurrentWeapon() != null)
            weaponType = weaponController.GetCurrentWeapon().weaponType;

        // Crit
        bool isCrit = false;
        float finalDamage = damage;
        if (EquipmentManager.Instance != null)
        {
            float critRate = EquipmentManager.Instance.GetTotalCritRateBonus();
            if (Random.Range(0f, 1f) < critRate)
            {
                isCrit = true;
                float equipBonus = EquipmentManager.Instance.GetTotalCritDamageMultiplier() - 1f;
                finalDamage *= BASE_CRIT_MULTIPLIER + equipBonus;
            }
        }

        if (debugMode)
            Debug.Log($"[{GetType().Name}] Hit {enemy.name} for {finalDamage:F1} (crit:{isCrit}, weapon:{weaponType})");

        // Gửi skill damage
        enemy.TakeSkillDamage(finalDamage, weaponType, isCrit);

        // Phân biệt Boss và lính lác để gọi CC (Hất tung, Kéo...)
        var enemyScriptComp = enemy.GetComponent<EnemyScript>();
        if (enemyScriptComp == null) enemyScriptComp = enemy.GetComponentInParent<EnemyScript>();
        bool isBoss = enemyScriptComp != null && enemyScriptComp.isBoss;

        if (!isBoss)
            ApplyEffect(enemy);
        else if (debugMode)
            Debug.Log($"[{GetType().Name}] Boss detected — CC effect skipped, damage applied only.");
    }

    protected abstract void ApplyEffect(TakeDamageTest enemy);
}
