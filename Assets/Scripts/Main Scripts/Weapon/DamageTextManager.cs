using UnityEngine;
using DamageNumbersPro;

/// <summary>
/// Manages different damage text prefabs for different weapon types and crit states
/// </summary>
public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [Header("Sword Damage Text Prefabs")]
    [SerializeField] private DamageNumber swordNormalPrefab;
    [SerializeField] private DamageNumber swordCritPrefab;

    [Header("Axe Damage Text Prefabs")]
    [SerializeField] private DamageNumber axeNormalPrefab;
    [SerializeField] private DamageNumber axeCritPrefab;

    [Header("Mage Damage Text Prefabs")]
    [SerializeField] private DamageNumber mageNormalPrefab;
    [SerializeField] private DamageNumber mageCritPrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[DamageTextManager] Multiple instances found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Spawn damage text at position with appropriate prefab based on weapon type and crit status
    /// </summary>
    public void SpawnDamageText(Vector3 position, float damage, WeaponType weaponType, bool isCrit)
    {
        DamageNumber prefab = GetDamageTextPrefab(weaponType, isCrit);
        
        if (prefab == null)
        {
            Debug.LogWarning($"[DamageTextManager] No damage text prefab found for {weaponType} (crit: {isCrit})! Using fallback.");
            return;
        }

        // Spawn damage number above the target
        Vector3 spawnPosition = position + Vector3.up * 2f;
        var damageNumber = prefab.Spawn(spawnPosition, damage);

        // Customize appearance based on crit status
        if (isCrit)
        {
            damageNumber.SetColor(Color.yellow); // Gold/yellow for crit
            damageNumber.SetScale(1.5f); // Larger for crit
        }
        else
        {
            damageNumber.SetColor(Color.red); // Red for normal
            damageNumber.SetScale(1.2f); // Normal size
        }
    }

    /// <summary>
    /// Get the appropriate damage text prefab based on weapon type and crit status
    /// </summary>
    private DamageNumber GetDamageTextPrefab(WeaponType weaponType, bool isCrit)
    {
        switch (weaponType)
        {
            case WeaponType.Sword:
                return isCrit ? swordCritPrefab : swordNormalPrefab;
            case WeaponType.Axe:
                return isCrit ? axeCritPrefab : axeNormalPrefab;
            case WeaponType.Mage:
                return isCrit ? mageCritPrefab : mageNormalPrefab;
            default:
                return swordNormalPrefab; // Fallback to sword normal
        }
    }
}

