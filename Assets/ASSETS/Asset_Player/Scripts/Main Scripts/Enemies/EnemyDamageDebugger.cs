using UnityEngine;

/// <summary>
/// Debug helper for enemy damage issues
/// Attach to enemy to debug skill damage problems
/// </summary>
public class EnemyDamageDebugger : MonoBehaviour
{
    [SerializeField] private bool enableDebugLogging = true;
    [SerializeField] private bool showCollisionInfo = true;

    private TakeDamageTest damageComponent;

    void Start()
    {
        damageComponent = GetComponent<TakeDamageTest>();
        if (damageComponent == null)
        {
            Debug.LogError($"[EnemyDamageDebugger] No TakeDamageTest component found on {gameObject.name}!");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (showCollisionInfo && enableDebugLogging)
        {
            Debug.Log($"[EnemyDamageDebugger] {gameObject.name} collision with: {collision.gameObject.name} (tag: {collision.gameObject.tag})");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (showCollisionInfo && enableDebugLogging)
        {
            Debug.Log($"[EnemyDamageDebugger] {gameObject.name} trigger with: {other.gameObject.name} (tag: {other.gameObject.tag}, layer: {LayerMask.LayerToName(other.gameObject.layer)})");

            // Try to get weapon component
            var weapon = other.GetComponent<WeaponController>();
            if (weapon != null)
            {
                Debug.Log($"[EnemyDamageDebugger] Weapon detected: {weapon.GetCurrentWeapon()?.weaponType}");
            }
        }
    }

    // Public method for skills to call
    public void DebugSkillDamage(float damage, WeaponType weaponType, string skillName)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[EnemyDamageDebugger] {gameObject.name} receiving skill damage: {damage} from {weaponType} skill: {skillName}");

            if (damageComponent != null)
            {
                Debug.Log($"[EnemyDamageDebugger] Damage component found, calling TakeDamage");
            }
            else
            {
                Debug.LogError($"[EnemyDamageDebugger] No TakeDamageTest component found!");
            }
        }
    }
}
















