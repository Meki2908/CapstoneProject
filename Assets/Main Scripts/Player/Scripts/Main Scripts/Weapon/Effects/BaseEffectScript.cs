using UnityEngine;

public abstract class BaseEffectScript : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected bool debugMode = false;

    private float baseDamage; // Store original damage value
    private WeaponController weaponController;
    private EquipmentSystem equipmentSystem;

    // Crit system
    private const float BASE_CRIT_MULTIPLIER = 1.5f; // Default 1.5x crit multiplier

    protected virtual void Awake()
    {
        baseDamage = damage; // Store original damage
        weaponController = FindFirstObjectByType<WeaponController>();
        
        // Find EquipmentSystem for crit calculation
        equipmentSystem = GetComponentInParent<EquipmentSystem>();
        if (equipmentSystem == null)
        {
            equipmentSystem = FindFirstObjectByType<EquipmentSystem>();
        }
    }

    protected virtual void Start()
    {
        UpdateDamageWithGems();
    }

    /// <summary>
    /// Update damage based on equipped gems: damage = baseDamage + (baseDamage × %)
    /// </summary>
    private void UpdateDamageWithGems()
    {
        if (WeaponGemManager.Instance == null || weaponController == null)
        {
            damage = baseDamage; // No gems, use base damage
            return;
        }

        WeaponSO currentWeapon = weaponController.GetCurrentWeapon();
        if (currentWeapon == null)
        {
            damage = baseDamage;
            return;
        }

        // Get damage multiplier from gems (returns 1.0 + total %)
        float damageMultiplier = WeaponGemManager.Instance.GetDamageMultiplier(currentWeapon.weaponType);

        // Calculate: baseDamage + (baseDamage × %)
        // damageMultiplier = 1.0 + totalPercent, so we need to extract the percent part
        float damagePercent = damageMultiplier - 1f; // Extract the % part (e.g., 1.15 -> 0.15)
        damage = baseDamage + (baseDamage * damagePercent);

        if (debugMode)
        {
            Debug.Log($"[{GetType().Name}] Updated damage: {baseDamage} -> {damage} (multiplier: {damageMultiplier:F2}, %: {damagePercent * 100f:F1}%)");
        }
    }

    protected virtual void OnParticleCollision(GameObject other)
    {
        if (other.TryGetComponent(out TakeDamageTest enemy))
        {
            // Update damage before applying (in case weapon changed)
            UpdateDamageWithGems();

            // Calculate crit
            bool isCrit = false;
            float finalDamage = damage;
            WeaponType weaponType = WeaponType.None;

            // Get weapon type
            if (weaponController != null && weaponController.GetCurrentWeapon() != null)
            {
                weaponType = weaponController.GetCurrentWeapon().weaponType;
            }

            // Check for critical hit
            if (EquipmentManager.Instance != null)
            {
                float critRate = EquipmentManager.Instance.GetTotalCritRateBonus();
                float randomValue = Random.Range(0f, 1f);
                isCrit = randomValue < critRate;

                if (isCrit)
                {
                    // Base crit multiplier (1.5x) + equipment bonus
                    float critDamageMultiplier = BASE_CRIT_MULTIPLIER;
                    float equipmentCritBonus = EquipmentManager.Instance.GetTotalCritDamageMultiplier();
                    // Equipment returns total multiplier (e.g., 1.5), so we need to extract the bonus part
                    // If equipment gives 1.5x, and base is 1.5x, total should be 1.5 + (1.5 - 1.0) = 2.0x
                    float equipmentBonus = equipmentCritBonus - 1f; // Extract bonus part (e.g., 1.5 -> 0.5)
                    critDamageMultiplier = BASE_CRIT_MULTIPLIER + equipmentBonus;
                    finalDamage *= critDamageMultiplier;
                }
            }

            if (debugMode) Debug.Log($"[{GetType().Name}] Particle hit: {enemy.name} for {finalDamage} damage (crit: {isCrit})");

            // Apply damage with weapon type and crit status
            enemy.TakeSkillDamage(finalDamage, weaponType, isCrit);

            // Apply specific effect
            ApplyEffect(enemy);
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out TakeDamageTest enemy))
        {
            // Update damage before applying (in case weapon changed)
            UpdateDamageWithGems();

            // Calculate crit
            bool isCrit = false;
            float finalDamage = damage;
            WeaponType weaponType = WeaponType.None;

            // Get weapon type
            if (weaponController != null && weaponController.GetCurrentWeapon() != null)
            {
                weaponType = weaponController.GetCurrentWeapon().weaponType;
            }

            // Check for critical hit
            if (EquipmentManager.Instance != null)
            {
                float critRate = EquipmentManager.Instance.GetTotalCritRateBonus();
                float randomValue = Random.Range(0f, 1f);
                isCrit = randomValue < critRate;

                if (isCrit)
                {
                    // Base crit multiplier (1.5x) + equipment bonus
                    float critDamageMultiplier = BASE_CRIT_MULTIPLIER;
                    float equipmentCritBonus = EquipmentManager.Instance.GetTotalCritDamageMultiplier();
                    // Equipment returns total multiplier (e.g., 1.5), so we need to extract the bonus part
                    float equipmentBonus = equipmentCritBonus - 1f; // Extract bonus part (e.g., 1.5 -> 0.5)
                    critDamageMultiplier = BASE_CRIT_MULTIPLIER + equipmentBonus;
                    finalDamage *= critDamageMultiplier;
                }
            }

            if (debugMode) Debug.Log($"[{GetType().Name}] Collision hit: {enemy.name} for {finalDamage} damage (crit: {isCrit})");

            // Apply damage with weapon type and crit status
            enemy.TakeSkillDamage(finalDamage, weaponType, isCrit);

            // Apply specific effect
            ApplyEffect(enemy);
        }
    }

    protected abstract void ApplyEffect(TakeDamageTest enemy);
}
