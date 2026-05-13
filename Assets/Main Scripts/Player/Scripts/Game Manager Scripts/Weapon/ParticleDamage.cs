using UnityEngine;

/// <summary>
/// Handles damage for Mage projectiles with crit system and damage text
/// </summary>
public class ProjectileDamage : MonoBehaviour
{
    [SerializeField] float damage = 10f;
    [SerializeField] bool debugMode = false;
    [SerializeField] private AudioSource impactAudioSource;

    private float baseDamage;
    private EquipmentSystem equipmentSystem;
    private WeaponController weaponController;
    private bool hasPlayedImpactSfx;

    private void Awake()
    {
        baseDamage = damage;
        hasPlayedImpactSfx = false;
        equipmentSystem = GetComponentInParent<EquipmentSystem>();
        weaponController = GetComponentInParent<WeaponController>();
        if (impactAudioSource == null)
            impactAudioSource = GetComponentInParent<AudioSource>();
    }

    public void SetOwner(Character owner)
    {
        if (owner == null) return;
        equipmentSystem = owner.GetComponentInChildren<EquipmentSystem>();
        weaponController = owner.GetComponentInChildren<WeaponController>();
        UpdateDamageWithGems();
        TryRegisterWithProjectileManager();
    }

    private void Start()
    {
        UpdateDamageWithGems();
        TryRegisterWithProjectileManager();
    }

    void TryRegisterWithProjectileManager()
    {
        if (SkillProjectileManager.Instance == null) return;
        var wt = WeaponType.Mage;
        if (weaponController != null && weaponController.GetCurrentWeapon() != null)
            wt = weaponController.GetCurrentWeapon().weaponType;
        SkillProjectileManager.Instance.SetupSkillProjectile(gameObject, damage, wt, AbilityInput.E, false);
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
        if (currentWeapon == null)
        {
            damage = baseDamage;
            return;
        }

        float damageMultiplier = WeaponGemManager.Instance.GetDamageMultiplier(currentWeapon.weaponType);

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
        if (collision.gameObject.TryGetComponent(out TakeDamageTest enemy) == false)
        {
            // Fallback: tìm trên parent (trường hợp Vargr/boss có bone-child collider)
            enemy = collision.collider.GetComponentInParent<TakeDamageTest>();
        }

        if (enemy != null)
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
            
            // isSkill=true để skill projectile damage boss đúng cách
            var wt = weaponController != null && weaponController.GetCurrentWeapon() != null
                ? weaponController.GetCurrentWeapon().weaponType
                : WeaponType.Mage;
            enemy.TakeDamage(finalDamage, wt, isCrit, true);
            PlayImpactSfxOnce();
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        if (other.TryGetComponent(out TakeDamageTest enemy) == false)
        {
            // Fallback: tìm trên parent (bone-child collider)
            enemy = other.GetComponentInParent<TakeDamageTest>();
        }

        if (enemy != null)
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
            
            // isSkill=true để skill projectile damage boss đúng cách
            var wt = weaponController != null && weaponController.GetCurrentWeapon() != null
                ? weaponController.GetCurrentWeapon().weaponType
                : WeaponType.Mage;
            enemy.TakeDamage(finalDamage, wt, isCrit, true);
            PlayImpactSfxOnce();
        }
    }

    private void PlayImpactSfxOnce()
    {
        if (hasPlayedImpactSfx)
        {
            return;
        }

        SoundManager.PlayMageProjectileHit(impactAudioSource, 1f);
        hasPlayedImpactSfx = true;
    }
}