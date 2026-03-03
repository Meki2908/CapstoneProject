using UnityEngine;

/// <summary>
/// Manager to automatically setup skill projectiles with damage components.
/// This ensures all skill projectiles can damage enemies properly.
/// </summary>
public class SkillProjectileManager : MonoBehaviour
{
    public static SkillProjectileManager Instance { get; private set; }

    [Header("Default Settings")]
    [SerializeField] private float defaultDamage = 25f;
    [SerializeField] private WeaponType defaultWeaponType = WeaponType.Sword;
    [SerializeField] private bool defaultDestroyOnHit = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Auto-initialize if no instance exists
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject managerObject = new GameObject("SkillProjectileManager");
            managerObject.AddComponent<SkillProjectileManager>();
            Debug.Log("[SkillProjectileManager] Auto-initialized singleton instance");
        }
    }

    /// <summary>
    /// Setup a skill projectile with damage capabilities
    /// </summary>
    public void SetupSkillProjectile(GameObject projectile, float damage = -1, WeaponType weaponType = WeaponType.Sword, AbilityInput skillInput = AbilityInput.E, bool destroyOnHit = true)
    {
        if (projectile == null) return;

        // Use defaults if not specified
        if (damage < 0) damage = defaultDamage;
        if (weaponType == WeaponType.Sword && damage == defaultDamage) weaponType = defaultWeaponType;
        if (destroyOnHit == defaultDestroyOnHit) destroyOnHit = defaultDestroyOnHit;

        // Ensure it has a concrete collider BEFORE adding the SkillDamageHelper
        if (projectile.GetComponent<Collider>() == null)
        {
            // Add trigger collider if no collider exists
            SphereCollider collider = projectile.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
            Debug.Log($"[SkillProjectileManager] Added SphereCollider to {projectile.name}");
        }

        // Add SkillDamageHelper if not already present
        SkillDamageHelper damageHelper = projectile.GetComponent<SkillDamageHelper>();
        if (damageHelper == null)
        {
            damageHelper = projectile.AddComponent<SkillDamageHelper>();
            Debug.Log($"[SkillProjectileManager] Added SkillDamageHelper to {projectile.name}");
        }

        // Setup the damage helper
        damageHelper.SetDamage(damage, weaponType, false);
        damageHelper.SetSkillInfo(skillInput, false);

        Debug.Log($"[SkillProjectileManager] Setup skill projectile {projectile.name} with {damage} damage ({weaponType})");
    }

    /// <summary>
    /// Quick setup for basic projectiles
    /// </summary>
    public void SetupBasicProjectile(GameObject projectile, WeaponType weaponType = WeaponType.Sword)
    {
        SetupSkillProjectile(projectile, defaultDamage, weaponType, AbilityInput.E, defaultDestroyOnHit);
    }
}
