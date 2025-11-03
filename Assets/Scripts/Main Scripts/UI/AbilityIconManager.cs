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

    [Header("CD Remain Text")]
    [SerializeField] private TextMeshProUGUI eCDRemain;
    [SerializeField] private TextMeshProUGUI rCDRemain;
    [SerializeField] private TextMeshProUGUI tCDRemain;
    [SerializeField] private TextMeshProUGUI ultimateCDRemain;

    [Header("Default Icons")]
    [SerializeField] private Sprite defaultIcon;

    [Header("Ultimate Icon Shader")]
    [SerializeField] private UltimateIconShaderController ultimateShaderController;

    // Cooldown tracking
    private Dictionary<AbilityInput, float> cooldownEndTimes = new Dictionary<AbilityInput, float>();
    private Dictionary<AbilityInput, float> cooldownDurations = new Dictionary<AbilityInput, float>();

    private void Awake()
    {
        // Initialize with default icons
        SetDefaultIcons();
        InitializeCooldownUI();

        // Auto-find ultimate shader controller
        if (ultimateShaderController == null && ultimateIcon != null)
        {
            ultimateShaderController = ultimateIcon.GetComponent<UltimateIconShaderController>();
        }
    }

    private void Update()
    {
        UpdateCooldownUI();
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

        // Initialize CD remain texts to be invisible
        if (eCDRemain != null) eCDRemain.gameObject.SetActive(false);
        if (rCDRemain != null) rCDRemain.gameObject.SetActive(false);
        if (tCDRemain != null) tCDRemain.gameObject.SetActive(false);
        if (ultimateCDRemain != null) ultimateCDRemain.gameObject.SetActive(false);
    }

    private void UpdateCooldownUI()
    {
        UpdateCooldownForAbility(AbilityInput.E, eCDOverlay, eCDRemain);
        UpdateCooldownForAbility(AbilityInput.R, rCDOverlay, rCDRemain);
        UpdateCooldownForAbility(AbilityInput.T, tCDOverlay, tCDRemain);
        UpdateCooldownForAbility(AbilityInput.Q_Ultimate, ultimateCDOverlay, ultimateCDRemain);
    }

    private void UpdateCooldownForAbility(AbilityInput input, Image cdOverlay, TextMeshProUGUI cdRemain)
    {
        if (!cooldownEndTimes.ContainsKey(input) || cdOverlay == null) return;

        float currentTime = Time.time;
        float endTime = cooldownEndTimes[input];
        float duration = cooldownDurations[input];

        if (currentTime < endTime)
        {
            // Still on cooldown
            float remainingTime = endTime - currentTime;
            float progress = 1f - (remainingTime / duration);

            // Update CD overlay (fillAmount goes from 1 to 0)
            cdOverlay.fillAmount = 1f - progress;

            // Update CD remain text
            if (cdRemain != null)
            {
                cdRemain.gameObject.SetActive(true);
                cdRemain.text = Mathf.Ceil(remainingTime).ToString();
            }
        }
        else
        {
            // Cooldown finished
            cdOverlay.fillAmount = 0f;
            if (cdRemain != null)
            {
                cdRemain.gameObject.SetActive(false);
            }

            // Only remove end time, keep duration for future use
            cooldownEndTimes.Remove(input);
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

        // Store cooldown durations for each ability
        StoreCooldownDurations(abilities);
    }

    private void StoreCooldownDurations(AbilitySO[] abilities)
    {
        cooldownDurations.Clear();
        foreach (var ability in abilities)
        {
            if (ability != null)
            {
                cooldownDurations[ability.input] = ability.cooldown;
                Debug.Log($"[AbilityIconManager] Stored cooldown for {ability.input}: {ability.cooldown}s");
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

    // Method to trigger cooldown when ability is used
    public void TriggerCooldown(AbilityInput input)
    {
        if (!cooldownDurations.ContainsKey(input))
        {
            Debug.LogWarning($"[AbilityIconManager] No cooldown duration found for {input}. Available durations: {string.Join(", ", cooldownDurations.Keys)}");
            return;
        }

        float duration = cooldownDurations[input];
        float endTime = Time.time + duration;

        cooldownEndTimes[input] = endTime;

        Debug.Log($"[AbilityIconManager] Triggered cooldown for {input}: {duration}s (ends at {endTime:F2})");
    }

    // Check if ability is on cooldown
    public bool IsOnCooldown(AbilityInput input)
    {
        if (!cooldownEndTimes.ContainsKey(input)) return false;
        return Time.time < cooldownEndTimes[input];
    }

    // Get remaining cooldown time
    public float GetRemainingCooldown(AbilityInput input)
    {
        if (!cooldownEndTimes.ContainsKey(input)) return 0f;
        return Mathf.Max(0f, cooldownEndTimes[input] - Time.time);
    }

    // Get cooldown duration for an ability
    public float GetCooldownDuration(AbilityInput input)
    {
        if (cooldownDurations.ContainsKey(input))
        {
            return cooldownDurations[input];
        }
        return 0f;
    }

    // Debug method to check cooldown system status
    public void DebugCooldownStatus()
    {
        Debug.Log("=== Cooldown System Debug ===");
        Debug.Log($"Cooldown Durations: {cooldownDurations.Count} entries");
        foreach (var kvp in cooldownDurations)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value}s");
        }

        Debug.Log($"Active Cooldowns: {cooldownEndTimes.Count} entries");
        foreach (var kvp in cooldownEndTimes)
        {
            float remaining = GetRemainingCooldown(kvp.Key);
            Debug.Log($"  {kvp.Key}: {remaining:F1}s remaining");
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

    // Ultimate Icon Shader Effects
    public void TriggerUltimateReadyEffect()
    {
        if (ultimateShaderController != null)
        {
            ultimateShaderController.TriggerReadyEffect();
        }
    }

    public void SetUltimateReadyState(bool ready)
    {
        if (ultimateShaderController != null)
        {
            ultimateShaderController.SetReadyState(ready);
        }
    }

    // Animation Event for ultimate ready effect
    public void AE_TriggerUltimateReady()
    {
        TriggerUltimateReadyEffect();
    }
}
