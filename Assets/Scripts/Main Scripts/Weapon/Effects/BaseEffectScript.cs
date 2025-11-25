using UnityEngine;

public abstract class BaseEffectScript : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected bool debugMode = false;

    private float baseDamage; // Store original damage value
    private WeaponController weaponController;

    protected virtual void Awake()
    {
        baseDamage = damage; // Store original damage
        weaponController = FindObjectOfType<WeaponController>();
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

            if (debugMode) Debug.Log($"[{GetType().Name}] Particle hit: {enemy.name} for {damage} damage");

            // Apply damage first
            enemy.TakeDamage(damage);

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

            if (debugMode) Debug.Log($"[{GetType().Name}] Collision hit: {enemy.name} for {damage} damage");

            // Apply damage first
            enemy.TakeDamage(damage);

            // Apply specific effect
            ApplyEffect(enemy);
        }
    }

    protected abstract void ApplyEffect(TakeDamageTest enemy);
}
