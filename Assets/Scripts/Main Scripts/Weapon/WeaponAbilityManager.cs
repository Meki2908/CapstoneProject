using UnityEngine;

public class WeaponAbilityManager : MonoBehaviour
{
    [Header("Weapon Abilities")]
    [SerializeField] private AbilitySO[] weaponAbilities;

    [Header("GUI Manager")]
    [SerializeField] private AbilityIconManager iconManager;

    [Header("Weapon Reference")]
    [SerializeField] private WeaponSO weaponSO;

    private void Awake()
    {
        // Find icon manager if not assigned
        if (iconManager == null)
        {
            iconManager = FindObjectOfType<AbilityIconManager>();
            if (iconManager != null)
            {
                Debug.Log("[WeaponAbilityManager] Auto-found AbilityIconManager");
            }
            else
            {
                Debug.LogWarning("[WeaponAbilityManager] No AbilityIconManager found in scene! Ability icons will not work.");
            }
        }
    }

    // Animation Event: Set ability icons when weapon is drawn
    public void AE_SetWeaponAbilities()
    {
        Debug.Log($"[WeaponAbilityManager] AE_SetWeaponAbilities called - iconManager: {iconManager != null}, abilities: {weaponAbilities?.Length ?? 0}");

        if (iconManager != null && weaponAbilities != null)
        {
            iconManager.AE_SetAbilityIcons(weaponAbilities);

            // Set weapon type for mastery checking
            if (weaponSO != null)
            {
                iconManager.SetCurrentWeaponType(weaponSO.weaponType);
            }
            else
            {
                // Try to get weapon from parent WeaponController
                var weaponController = GetComponentInParent<WeaponController>();
                if (weaponController != null && weaponController.GetCurrentWeapon() != null)
                {
                    iconManager.SetCurrentWeaponType(weaponController.GetCurrentWeapon().weaponType);
                }
            }

            Debug.Log("[WeaponAbilityManager] Successfully called iconManager.AE_SetAbilityIcons");
        }
        else
        {
            Debug.LogWarning($"[WeaponAbilityManager] IconManager or Abilities not found - iconManager: {iconManager != null}, abilities: {weaponAbilities != null}");
        }
    }

    public void SetWeaponSO(WeaponSO weapon)
    {
        weaponSO = weapon;
    }

    // Animation Event: Clear ability icons when weapon is sheathed
    public void AE_ClearWeaponAbilities()
    {
        if (iconManager != null)
        {
            iconManager.AE_ClearAbilityIcons();
        }
    }

    // Get abilities for this weapon
    public AbilitySO[] GetWeaponAbilities()
    {
        return weaponAbilities;
    }

    // Get specific ability by input
    public AbilitySO GetAbilityByInput(AbilityInput input)
    {
        if (weaponAbilities == null) return null;

        foreach (var ability in weaponAbilities)
        {
            if (ability != null && ability.input == input)
            {
                return ability;
            }
        }

        return null;
    }

    // Set abilities manually
    public void SetAbilities(AbilitySO[] abilities)
    {
        weaponAbilities = abilities;
        Debug.Log($"[WeaponAbilityManager] Set {abilities?.Length ?? 0} abilities");
    }
}
