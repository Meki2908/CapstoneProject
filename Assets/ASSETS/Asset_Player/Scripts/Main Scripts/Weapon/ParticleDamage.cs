using UnityEngine;

/// <summary>
/// Handles damage for Mage projectiles with crit system and damage text
/// </summary>
public class ProjectileDamage : MonoBehaviour
{
    [SerializeField] float damage = 10f;
    [SerializeField] bool debugMode = false;

    private float baseDamage;
    private EquipmentSystem equipmentSystem;
    private WeaponController weaponController;

    private void Awake()
    {
        baseDamage = damage;
        // Find EquipmentSystem and WeaponController
        equipmentSystem = GetComponentInParent<EquipmentSystem>();
        if (equipmentSystem == null)
        {
            equipmentSystem = FindFirstObjectByType<EquipmentSystem>();
        }
        
        weaponController = GetComponentInParent<WeaponController>();
        if (weaponController == null)
        {
            weaponController = FindFirstObjectByType<WeaponController>();
        }
    }

    private void Start()
    {
        UpdateDamageWithGems();
    }

    /// <summary>
    /// Update damage based on equipped gems: damage = baseDamage + (baseDamage × %)
    /// </summary>
    private void UpdateDamageWithGems()
    {
        if (WeaponGemManager.Instance == null || equipmentSystem == null)
        {
            damage = baseDamage; // No gems, use base damage
            return;
        }

        WeaponSO currentWeapon = equipmentSystem.GetCurrentWeapon();
        if (currentWeapon == null || currentWeapon.weaponType != WeaponType.Mage)
        {
            damage = baseDamage;
            return;
        }

        // Get damage multiplier from gems (returns 1.0 + total %)
        float damageMultiplier = WeaponGemManager.Instance.GetDamageMultiplier(WeaponType.Mage);

        // Calculate: baseDamage + (baseDamage × %)
        float damagePercent = damageMultiplier - 1f; // Extract the % part (e.g., 1.15 -> 0.15)
        damage = baseDamage + (baseDamage * damagePercent);

        if (debugMode)
        {
            Debug.Log($"[ProjectileDamage] Updated damage: {baseDamage} -> {damage} (multiplier: {damageMultiplier:F2}, %: {damagePercent * 100f:F1}%)");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out TakeDamageTest enemy))
        {
            // Update damage before applying (in case weapon changed)
            UpdateDamageWithGems();
            
            // Calculate crit
            bool isCrit = false;
            float finalDamage = damage;
            
            if (EquipmentManager.Instance != null)
            {
                float critRate = EquipmentManager.Instance.GetTotalCritRateBonus();
                float randomValue = Random.Range(0f, 1f);
                isCrit = randomValue < critRate;

                if (isCrit)
                {
                    float critDamageMultiplier = EquipmentManager.Instance.GetTotalCritDamageMultiplier();
                    finalDamage *= critDamageMultiplier;
                }
            }

            if (debugMode) Debug.Log($"[ProjectileDamage] Collision hit: {enemy.name} for {finalDamage} damage (crit: {isCrit})");
            
            // Pass weapon type (Mage) and crit status
            enemy.TakeDamage(finalDamage, WeaponType.Mage, isCrit);
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other.TryGetComponent(out TakeDamageTest enemy))
        {
            // Update damage before applying (in case weapon changed)
            UpdateDamageWithGems();
            
            // Calculate crit
            bool isCrit = false;
            float finalDamage = damage;
            
            if (EquipmentManager.Instance != null)
            {
                float critRate = EquipmentManager.Instance.GetTotalCritRateBonus();
                float randomValue = Random.Range(0f, 1f);
                isCrit = randomValue < critRate;

                if (isCrit)
                {
                    float critDamageMultiplier = EquipmentManager.Instance.GetTotalCritDamageMultiplier();
                    finalDamage *= critDamageMultiplier;
                }
            }

            if (debugMode) Debug.Log($"[ProjectileDamage] Particle hit: {enemy.name} for {finalDamage} damage (crit: {isCrit})");
            
            // Pass weapon type (Mage) and crit status
            enemy.TakeDamage(finalDamage, WeaponType.Mage, isCrit);
        }
    }
}