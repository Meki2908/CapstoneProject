using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class AbilityIconManager : MonoBehaviour
{
    [Header("Ability Icons")]
    [SerializeField] private Image eAbilityIcon;
    [SerializeField] private Image rAbilityIcon;
    [SerializeField] private Image tAbilityIcon;
    [SerializeField] private Image ultimateIcon;

    [Header("CD Overlay Images (Image Type = Filled)")]
    [SerializeField] private Image eCDOverlay;
    [SerializeField] private Image rCDOverlay;
    [SerializeField] private Image tCDOverlay;
    [SerializeField] private Image ultimateCDOverlay;

    [Header("CD Remain Text - Sword (E, R, T, Q)")]
    [SerializeField] private TextMeshProUGUI swordECDRemain;
    [SerializeField] private TextMeshProUGUI swordRCDRemain;
    [SerializeField] private TextMeshProUGUI swordTCDRemain;
    [SerializeField] private TextMeshProUGUI swordUltimateCDRemain;

    [Header("CD Remain Text - Axe (E, R, T, Q)")]
    [SerializeField] private TextMeshProUGUI axeECDRemain;
    [SerializeField] private TextMeshProUGUI axeRCDRemain;
    [SerializeField] private TextMeshProUGUI axeTCDRemain;
    [SerializeField] private TextMeshProUGUI axeUltimateCDRemain;

    [Header("CD Remain Text - Staff (E, R, T, Q)")]
    [SerializeField] private TextMeshProUGUI staffECDRemain;
    [SerializeField] private TextMeshProUGUI staffRCDRemain;
    [SerializeField] private TextMeshProUGUI staffTCDRemain;
    [SerializeField] private TextMeshProUGUI staffUltimateCDRemain;

    [Header("Default Icons")]
    [SerializeField] private Sprite defaultIcon;

    [Header("Skill Lock Overlays (gameplay only locks T and Q)")]
    [SerializeField] private Image tLockOverlay;
    [SerializeField] private Image ultimateLockOverlay;

    [Header("Weapon reference (for per-weapon cooldown flag)")]
    [SerializeField] private WeaponController weaponController;

    // Cooldown per (WeaponType, AbilityInput) so each weapon has its own CD
    private Dictionary<(WeaponType, AbilityInput), float> cooldownEndTimes = new Dictionary<(WeaponType, AbilityInput), float>();
    private Dictionary<(WeaponType, AbilityInput), float> cooldownDurations = new Dictionary<(WeaponType, AbilityInput), float>();

    // Current weapon type for UI and mastery
    private WeaponType currentWeaponType = WeaponType.None;

    // Store current abilities to refresh cooldown when gems change
    private AbilitySO[] currentAbilities;

    private void Awake()
    {
        if (weaponController == null)
            weaponController = FindFirstObjectByType<WeaponController>();

        // Initialize with default icons
        SetDefaultIcons();
        InitializeCooldownUI();

        // Subscribe to gem changes to refresh cooldown durations
        if (WeaponGemManager.Instance != null)
        {
            WeaponGemManager.Instance.OnGemsChanged += OnGemsChanged;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from gem changes
        if (WeaponGemManager.Instance != null)
        {
            WeaponGemManager.Instance.OnGemsChanged -= OnGemsChanged;
        }
    }

    private void OnGemsChanged(WeaponType weaponType)
    {
        // Refresh cooldown durations if current weapon matches
        if (weaponType == currentWeaponType && currentAbilities != null)
        {
            StoreCooldownDurations(currentAbilities);
            Debug.Log($"[AbilityIconManager] Refreshed cooldown durations for {weaponType} after gem change");
        }
    }

    private void Update()
    {
        UpdateCooldownUI();
        UpdateSkillLockOverlays();
    }

    private void SetDefaultIcons()
    {
        if (eAbilityIcon != null) eAbilityIcon.sprite = defaultIcon;
        if (rAbilityIcon != null) rAbilityIcon.sprite = defaultIcon;
        if (tAbilityIcon != null) tAbilityIcon.sprite = defaultIcon;
        if (ultimateIcon != null) ultimateIcon.sprite = defaultIcon;
    }

    private void InitializeCooldownUI()
    {
        // Initialize CD overlays to be invisible (fillAmount = 0)
        if (eCDOverlay != null) eCDOverlay.fillAmount = 0f;
        if (rCDOverlay != null) rCDOverlay.fillAmount = 0f;
        if (tCDOverlay != null) tCDOverlay.fillAmount = 0f;
        if (ultimateCDOverlay != null) ultimateCDOverlay.fillAmount = 0f;

        // Initialize all CD remain texts (Sword / Axe / Staff) to invisible
        if (swordECDRemain != null) swordECDRemain.gameObject.SetActive(false);
        if (swordRCDRemain != null) swordRCDRemain.gameObject.SetActive(false);
        if (swordTCDRemain != null) swordTCDRemain.gameObject.SetActive(false);
        if (swordUltimateCDRemain != null) swordUltimateCDRemain.gameObject.SetActive(false);
        if (axeECDRemain != null) axeECDRemain.gameObject.SetActive(false);
        if (axeRCDRemain != null) axeRCDRemain.gameObject.SetActive(false);
        if (axeTCDRemain != null) axeTCDRemain.gameObject.SetActive(false);
        if (axeUltimateCDRemain != null) axeUltimateCDRemain.gameObject.SetActive(false);
        if (staffECDRemain != null) staffECDRemain.gameObject.SetActive(false);
        if (staffRCDRemain != null) staffRCDRemain.gameObject.SetActive(false);
        if (staffTCDRemain != null) staffTCDRemain.gameObject.SetActive(false);
        if (staffUltimateCDRemain != null) staffUltimateCDRemain.gameObject.SetActive(false);

        // Initialize lock overlays
        UpdateSkillLockOverlays();
    }

    private void UpdateCooldownUI()
    {
        UpdateCooldownForAbility(AbilityInput.E, eCDOverlay, swordECDRemain, axeECDRemain, staffECDRemain);
        UpdateCooldownForAbility(AbilityInput.R, rCDOverlay, swordRCDRemain, axeRCDRemain, staffRCDRemain);
        UpdateCooldownForAbility(AbilityInput.T, tCDOverlay, swordTCDRemain, axeTCDRemain, staffTCDRemain);
        UpdateCooldownForAbility(AbilityInput.Q_Ultimate, ultimateCDOverlay, swordUltimateCDRemain, axeUltimateCDRemain, staffUltimateCDRemain);
    }

    private void UpdateCooldownForAbility(AbilityInput input, Image cdOverlay,
        TextMeshProUGUI swordText, TextMeshProUGUI axeText, TextMeshProUGUI staffText)
    {
        if (cdOverlay == null) return;

        TextMeshProUGUI activeText = null;
        switch (currentWeaponType)
        {
            case WeaponType.Sword: activeText = swordText; break;
            case WeaponType.Axe:   activeText = axeText;   break;
            case WeaponType.Mage:  activeText = staffText; break;
            default:
                cdOverlay.fillAmount = 0f;
                if (swordText != null) swordText.gameObject.SetActive(false);
                if (axeText != null)   axeText.gameObject.SetActive(false);
                if (staffText != null) staffText.gameObject.SetActive(false);
                return;
        }

        var key = (currentWeaponType, input);
        if (!cooldownEndTimes.TryGetValue(key, out float endTime) || !cooldownDurations.TryGetValue(key, out float duration))
        {
            cdOverlay.fillAmount = 0f;
            if (activeText != null) activeText.gameObject.SetActive(false);
            if (swordText != null && swordText != activeText) swordText.gameObject.SetActive(false);
            if (axeText != null && axeText != activeText) axeText.gameObject.SetActive(false);
            if (staffText != null && staffText != activeText) staffText.gameObject.SetActive(false);
            return;
        }

        float currentTime = Time.time;

        if (currentTime < endTime)
        {
            float remainingTime = endTime - currentTime;
            float progress = 1f - (remainingTime / duration);
            cdOverlay.fillAmount = 1f - progress;

            if (activeText != null)
            {
                activeText.gameObject.SetActive(true);
                activeText.text = Mathf.Ceil(remainingTime).ToString();
            }
            if (swordText != null && swordText != activeText) swordText.gameObject.SetActive(false);
            if (axeText != null && axeText != activeText) axeText.gameObject.SetActive(false);
            if (staffText != null && staffText != activeText) staffText.gameObject.SetActive(false);
        }
        else
        {
            cdOverlay.fillAmount = 0f;
            if (activeText != null) activeText.gameObject.SetActive(false);
            if (swordText != null && swordText != activeText) swordText.gameObject.SetActive(false);
            if (axeText != null && axeText != activeText) axeText.gameObject.SetActive(false);
            if (staffText != null && staffText != activeText) staffText.gameObject.SetActive(false);
            cooldownEndTimes.Remove(key);
        }
    }

    // Animation Event: Set ability icons when weapon is drawn
    public void AE_SetAbilityIcons(AbilitySO[] abilities)
    {
        Debug.Log($"[AbilityIconManager] AE_SetAbilityIcons called with {abilities?.Length ?? 0} abilities");

        if (abilities == null)
        {
            Debug.LogWarning("[AbilityIconManager] Abilities array is null!");
            return;
        }

        // Store current abilities for gem change refresh
        currentAbilities = abilities;

        // Reset all icons to default first
        SetDefaultIcons();
        Debug.Log("[AbilityIconManager] Reset to default icons");

        int iconsSet = 0;
        // Set icons based on ability input
        foreach (var ability in abilities)
        {
            if (ability == null || ability.abilityIcon == null)
            {
                Debug.LogWarning($"[AbilityIconManager] Ability is null or has no icon: {ability?.abilityName ?? "null"}");
                continue;
            }

            Debug.Log($"[AbilityIconManager] Setting icon for {ability.abilityName} (input: {ability.input})");

            switch (ability.input)
            {
                case AbilityInput.E:
                    if (eAbilityIcon != null)
                    {
                        eAbilityIcon.sprite = ability.abilityIcon;
                        iconsSet++;
                        Debug.Log("[AbilityIconManager] Set E ability icon");
                    }
                    break;
                case AbilityInput.R:
                    if (rAbilityIcon != null)
                    {
                        rAbilityIcon.sprite = ability.abilityIcon;
                        iconsSet++;
                        Debug.Log("[AbilityIconManager] Set R ability icon");
                    }
                    break;
                case AbilityInput.T:
                    if (tAbilityIcon != null)
                    {
                        tAbilityIcon.sprite = ability.abilityIcon;
                        iconsSet++;
                        Debug.Log("[AbilityIconManager] Set T ability icon");
                    }
                    break;
                case AbilityInput.Q_Ultimate:
                    if (ultimateIcon != null)
                    {
                        ultimateIcon.sprite = ability.abilityIcon;
                        iconsSet++;
                        Debug.Log("[AbilityIconManager] Set Ultimate ability icon");
                    }
                    break;
            }
        }

        Debug.Log($"[AbilityIconManager] Successfully set {iconsSet} ability icons out of {abilities.Length} abilities");

        // Update skill lock overlays first (to ensure currentWeaponType is set)
        UpdateSkillLockOverlays();

        // Store cooldown durations for each ability (with gem multipliers applied)
        StoreCooldownDurations(abilities);
    }

    // Set current weapon type for mastery checking
    public void SetCurrentWeaponType(WeaponType weaponType)
    {
        currentWeaponType = weaponType;
        UpdateSkillLockOverlays();
    }

    private void UpdateSkillLockOverlays()
    {
        if (WeaponMasteryManager.Instance == null || currentWeaponType == WeaponType.None)
        {
            if (tLockOverlay != null) tLockOverlay.gameObject.SetActive(false);
            if (ultimateLockOverlay != null) ultimateLockOverlay.gameObject.SetActive(false);
            return;
        }

        bool tUnlocked = WeaponMasteryManager.Instance.IsSkillUnlocked(currentWeaponType, AbilityInput.T);
        bool qUnlocked = WeaponMasteryManager.Instance.IsSkillUnlocked(currentWeaponType, AbilityInput.Q_Ultimate);

        if (tLockOverlay != null) tLockOverlay.gameObject.SetActive(!tUnlocked);
        if (ultimateLockOverlay != null) ultimateLockOverlay.gameObject.SetActive(!qUnlocked);
    }

    private void StoreCooldownDurations(AbilitySO[] abilities)
    {
        if (currentWeaponType == WeaponType.None) return;
        foreach (var ability in abilities)
        {
            if (ability != null)
            {
                float cooldownValue = ability.GetModifiedCooldown(currentWeaponType);
                var key = (currentWeaponType, ability.input);
                cooldownDurations[key] = cooldownValue;
                Debug.Log($"[AbilityIconManager] Stored cooldown for {currentWeaponType} {ability.input}: {cooldownValue}s (base: {ability.cooldown}s)");
            }
        }
    }

    // Animation Event: Clear ability icons when weapon is sheathed
    public void AE_ClearAbilityIcons()
    {
        SetDefaultIcons();
        InitializeCooldownUI();
        // DON'T clear cooldownEndTimes to prevent cheat when sheath -> draw
        // cooldownEndTimes.Clear(); // REMOVED to prevent CD reset cheat
        // Keep cooldownDurations for when weapon is drawn again
        Debug.Log("[AbilityIconManager] Cleared all ability icons but kept cooldowns to prevent cheat");
    }

    // Animation Event: Trigger cooldown when ability is used
    public void AE_TriggerCooldown(int inputIndex)
    {
        var input = (AbilityInput)inputIndex;
        TriggerCooldown(input);
    }

    // Method to trigger cooldown when ability is used (flags CD to current weapon from WeaponController)
    public void TriggerCooldown(AbilityInput input)
    {
        WeaponType weaponType = WeaponType.None;
        if (weaponController != null && weaponController.GetCurrentWeapon() != null)
            weaponType = weaponController.GetCurrentWeapon().weaponType;
        if (weaponType == WeaponType.None)
            weaponType = currentWeaponType;

        var key = (weaponType, input);
        if (!cooldownDurations.TryGetValue(key, out float duration))
        {
            Debug.LogWarning($"[AbilityIconManager] No cooldown duration for {weaponType} {input}. Trigger ignored.");
            return;
        }

        float endTime = Time.time + duration;
        cooldownEndTimes[key] = endTime;
        Debug.Log($"[AbilityIconManager] Triggered cooldown for {weaponType} {input}: {duration}s (ends at {endTime:F2})");
    }

    // Check if ability is on cooldown for current weapon
    public bool IsOnCooldown(AbilityInput input)
    {
        if (currentWeaponType == WeaponType.None) return false;
        var key = (currentWeaponType, input);
        if (!cooldownEndTimes.TryGetValue(key, out float endTime)) return false;
        return Time.time < endTime;
    }

    // Get remaining cooldown time for current weapon
    public float GetRemainingCooldown(AbilityInput input)
    {
        if (currentWeaponType == WeaponType.None) return 0f;
        var key = (currentWeaponType, input);
        if (!cooldownEndTimes.TryGetValue(key, out float endTime)) return 0f;
        return Mathf.Max(0f, endTime - Time.time);
    }

    // Get cooldown duration for current weapon
    public float GetCooldownDuration(AbilityInput input)
    {
        if (currentWeaponType == WeaponType.None) return 0f;
        var key = (currentWeaponType, input);
        return cooldownDurations.TryGetValue(key, out float d) ? d : 0f;
    }

    // Debug method to check cooldown system status
    public void DebugCooldownStatus()
    {
        Debug.Log("=== Cooldown System Debug ===");
        Debug.Log($"Cooldown Durations: {cooldownDurations.Count} entries");
        foreach (var kvp in cooldownDurations)
            Debug.Log($"  {kvp.Key.Item1} {kvp.Key.Item2}: {kvp.Value}s");

        Debug.Log($"Active Cooldowns: {cooldownEndTimes.Count} entries");
        foreach (var kvp in cooldownEndTimes)
        {
            float remaining = Mathf.Max(0f, kvp.Value - Time.time);
            Debug.Log($"  {kvp.Key.Item1} {kvp.Key.Item2}: {remaining:F1}s remaining");
        }
        Debug.Log("=== End Debug ===");
    }

    // Manual method to set specific ability icon
    public void SetAbilityIcon(AbilityInput input, Sprite icon)
    {
        switch (input)
        {
            case AbilityInput.E:
                if (eAbilityIcon != null) eAbilityIcon.sprite = icon;
                break;
            case AbilityInput.R:
                if (rAbilityIcon != null) rAbilityIcon.sprite = icon;
                break;
            case AbilityInput.T:
                if (tAbilityIcon != null) tAbilityIcon.sprite = icon;
                break;
            case AbilityInput.Q_Ultimate:
                if (ultimateIcon != null) ultimateIcon.sprite = icon;
                break;
        }
    }

}
