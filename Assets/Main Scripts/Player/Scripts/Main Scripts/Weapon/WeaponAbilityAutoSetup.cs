using UnityEngine;

[System.Serializable]
public class WeaponAbilitySetup
{
    public WeaponType weaponType;
    public AbilitySO[] abilities;
}

public class WeaponAbilityAutoSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private WeaponAbilitySetup[] weaponAbilitySetups;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private void Awake()
    {
        // Auto-setup abilities for current weapon
        SetupAbilitiesForCurrentWeapon();
    }

    private void SetupAbilitiesForCurrentWeapon()
    {
        var weaponController = GetComponent<WeaponController>();
        if (weaponController == null)
        {
            Debug.LogWarning("[WeaponAbilityAutoSetup] No WeaponController found!");
            return;
        }

        var currentWeapon = weaponController.GetCurrentWeapon();
        if (currentWeapon == null)
        {
            Debug.LogWarning("[WeaponAbilityAutoSetup] No current weapon found!");
            return;
        }

        // Find abilities for current weapon type
        AbilitySO[] abilities = GetAbilitiesForWeaponType(currentWeapon.weaponType);
        if (abilities == null || abilities.Length == 0)
        {
            if (debugMode) Debug.Log($"[WeaponAbilityAutoSetup] No abilities found for {currentWeapon.weaponType}");
            return;
        }

        // Create or find WeaponAbilityManager
        var abilityManager = GetComponent<WeaponAbilityManager>();
        if (abilityManager == null)
        {
            abilityManager = gameObject.AddComponent<WeaponAbilityManager>();
            if (debugMode) Debug.Log("[WeaponAbilityAutoSetup] Created WeaponAbilityManager");
        }

        // Set abilities
        abilityManager.SetAbilities(abilities);
        if (debugMode) Debug.Log($"[WeaponAbilityAutoSetup] Set {abilities.Length} abilities for {currentWeapon.weaponType}");
    }

    private AbilitySO[] GetAbilitiesForWeaponType(WeaponType weaponType)
    {
        if (weaponAbilitySetups == null) return null;

        foreach (var setup in weaponAbilitySetups)
        {
            if (setup.weaponType == weaponType)
            {
                return setup.abilities;
            }
        }

        return null;
    }

    // Public method to manually setup abilities
    public void SetupAbilities(WeaponType weaponType, AbilitySO[] abilities)
    {
        var abilityManager = GetComponent<WeaponAbilityManager>();
        if (abilityManager == null)
        {
            abilityManager = gameObject.AddComponent<WeaponAbilityManager>();
        }

        abilityManager.SetAbilities(abilities);
        if (debugMode) Debug.Log($"[WeaponAbilityAutoSetup] Manually set {abilities.Length} abilities for {weaponType}");
    }
}
