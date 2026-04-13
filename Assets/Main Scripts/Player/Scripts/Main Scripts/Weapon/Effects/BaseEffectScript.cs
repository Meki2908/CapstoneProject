using UnityEngine;

public abstract class BaseEffectScript : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected bool debugMode = false;

    private float baseDamage;
    private WeaponController weaponController;
    private EquipmentSystem equipmentSystem;

    private const float BASE_CRIT_MULTIPLIER = 1.5f;

    // ── Dedup per-frame ───────────────────────────────────────────────────────
    // Vargr / boss có nhiều bone collider → OnParticleCollision / OnCollisionEnter
    // có thể fire nhiều lần trong cùng 1 frame cho cùng 1 enemy.
    // Dùng HashSet<int> (InstanceID của TakeDamageTest.gameObject) để chỉ
    // apply damage + hiệu ứng đúng 1 lần mỗi frame.
    private readonly System.Collections.Generic.HashSet<int> _hitThisFrame
        = new System.Collections.Generic.HashSet<int>();
    private int _lastResetFrame = -1;

    // ─────────────────────────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        baseDamage = damage;
        weaponController = FindFirstObjectByType<WeaponController>();

        equipmentSystem = GetComponentInParent<EquipmentSystem>();
        if (equipmentSystem == null)
            equipmentSystem = FindFirstObjectByType<EquipmentSystem>();
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
        // Tìm TakeDamageTest: trực tiếp → parent (bone-child của Vargr/boss)
        TakeDamageTest enemy = other.GetComponent<TakeDamageTest>();
        if (enemy == null) enemy = other.GetComponentInParent<TakeDamageTest>();
        if (enemy != null) ProcessHit(enemy);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        // Tương tự — leo lên parent nếu hit bone-child
        TakeDamageTest enemy = collision.collider.GetComponent<TakeDamageTest>();
        if (enemy == null) enemy = collision.collider.GetComponentInParent<TakeDamageTest>();
        if (enemy != null) ProcessHit(enemy);
    }

    // ── Core processing ───────────────────────────────────────────────────────

    /// <summary>
    /// Áp dụng damage + effect lên enemy, với per-frame dedup để tránh
    /// nhiều bone collider của boss kích hoạt nhiều lần trong 1 frame.
    /// </summary>
    private void ProcessHit(TakeDamageTest enemy)
    {
        // Reset bộ lọc khi bước sang frame mới
        int frame = Time.frameCount;
        if (frame != _lastResetFrame)
        {
            _hitThisFrame.Clear();
            _lastResetFrame = frame;
        }

        // Dedup: chỉ xử lý mỗi enemy 1 lần mỗi frame
        int id = enemy.gameObject.GetInstanceID();
        if (_hitThisFrame.Contains(id)) return;
        _hitThisFrame.Add(id);

        // Cập nhật damage theo gem (vũ khí có thể đổi trong runtime)
        UpdateDamageWithGems();

        // Weapon type
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

        // Gửi skill damage (isSkill=true → boss nhận đúng, không bị block)
        enemy.TakeSkillDamage(finalDamage * CheatPanel.DamageMultiplier, weaponType, isCrit);

        // CC / hiệu ứng đặc biệt: KHÔNG áp lên boss
        // (boss có EnemyScript.isBoss = true → immune CC, chỉ nhận damage)
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
