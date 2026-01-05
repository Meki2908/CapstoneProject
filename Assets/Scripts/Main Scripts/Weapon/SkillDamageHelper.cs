using UnityEngine;

/// <summary>
/// Helper script to ensure skill projectiles properly damage enemies.
/// Attach this to skill projectiles that should damage enemies.
/// This script will auto-find and damage enemies with TakeDamageTest components.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class SkillDamageHelper : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private WeaponType weaponType = WeaponType.Sword;
    [SerializeField] private bool isCrit = false;
    [SerializeField] private bool destroyOnHit = true;

    [Header("Skill Settings")]
    [SerializeField] private AbilityInput skillInput = AbilityInput.E;
    [SerializeField] private bool isUltimate = false;
    [Header("Hurtbox Settings")]
    [Tooltip("If true, only colliders on the specified layer mask will be considered enemy hurtboxes.")]
    [SerializeField] private bool requireHurtboxLayer = false;
    [Tooltip("Layer mask that indicates enemy hurtboxes (use for trigger colliders).")]
    [SerializeField] private LayerMask enemyHurtboxLayer = ~0;

    private bool hasHit = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // If configured, require collider to be on the hurtbox layer
        if (requireHurtboxLayer && (enemyHurtboxLayer.value & (1 << other.gameObject.layer)) == 0)
            return;

        // Try to damage enemy (support child hurtbox triggers)
        var takeDamage = other.GetComponentInParent<TakeDamageTest>();
        if (takeDamage != null)
        {
            // Use skill-specific damage method
            switch (weaponType)
            {
                case WeaponType.Sword:
                    takeDamage.TakeSwordSkillDamage(damage, isCrit);
                    break;
                case WeaponType.Axe:
                    takeDamage.TakeAxeSkillDamage(damage, isCrit);
                    break;
                case WeaponType.Mage:
                    takeDamage.TakeMageSkillDamage(damage, isCrit);
                    break;
                default:
                    takeDamage.TakeSkillDamage(damage, weaponType, isCrit);
                    break;
            }

            Debug.Log($"[SkillDamageHelper] {gameObject.name} hit {other.name} for {damage} skill damage ({weaponType})");

            hasHit = true;

            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        var other = collision.collider;

        // If configured, require collider to be on the hurtbox layer
        if (requireHurtboxLayer && (enemyHurtboxLayer.value & (1 << other.gameObject.layer)) == 0)
            return;

        // Try to damage enemy (support child colliders)
        var takeDamage = other.GetComponentInParent<TakeDamageTest>();
        if (takeDamage != null)
        {
            // Use skill-specific damage method
            switch (weaponType)
            {
                case WeaponType.Sword:
                    takeDamage.TakeSwordSkillDamage(damage, isCrit);
                    break;
                case WeaponType.Axe:
                    takeDamage.TakeAxeSkillDamage(damage, isCrit);
                    break;
                case WeaponType.Mage:
                    takeDamage.TakeMageSkillDamage(damage, isCrit);
                    break;
                default:
                    takeDamage.TakeSkillDamage(damage, weaponType, isCrit);
                    break;
            }

            Debug.Log($"[SkillDamageHelper] {gameObject.name} hit {collision.collider.name} for {damage} skill damage ({weaponType})");

            hasHit = true;

            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
    }

    // Public methods to set damage properties
    public void SetDamage(float dmg, WeaponType type, bool crit = false)
    {
        damage = dmg;
        weaponType = type;
        isCrit = crit;
    }

    public void SetSkillInfo(AbilityInput input, bool ultimate = false)
    {
        skillInput = input;
        isUltimate = ultimate;
    }
}


