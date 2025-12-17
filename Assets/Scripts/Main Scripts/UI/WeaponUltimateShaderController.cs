using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller cho Ultimate Icon Shader theo từng weapon type
/// Sword: Thunder, Axe: Wind, Mage: Fire
/// </summary>
public class WeaponUltimateShaderController : MonoBehaviour
{
    [Header("Shader Materials")]
    [SerializeField] private Material swordUltimateMaterial;
    [SerializeField] private Material axeUltimateMaterial;
    [SerializeField] private Material mageUltimateMaterial;

    [Header("Shader Properties")]
    [SerializeField] private string cooldownProgressProperty = "_CooldownProgress";
    [SerializeField] private string readyGlowProperty = "_ReadyGlow";
    [SerializeField] private string readyPulseProperty = "_ReadyPulse";

    [Header("Effect Settings")]
    [SerializeField] private float readyGlowIntensity = 2.0f;
    [SerializeField] private float readyPulseIntensity = 0.8f;
    [SerializeField] private float normalGlowIntensity = 1.0f;

    [Header("Animation Settings")]
    [SerializeField] private float glowTransitionSpeed = 5.0f;

    private AbilityIconManager abilityIconManager;
    private WeaponController weaponController;
    private Image ultimateIcon;
    private Material currentMaterial;
    private Material materialInstance;
    private bool isInitialized = false;

    // Animation state
    private float targetGlowIntensity;
    private float currentGlowIntensity;
    private bool isReady = false;
    private float pulseTime = 0f;
    private WeaponType currentWeaponType = WeaponType.Sword;

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        abilityIconManager = UnityEngine.Object.FindFirstObjectByType<AbilityIconManager>();
        weaponController = UnityEngine.Object.FindFirstObjectByType<WeaponController>();

        if (abilityIconManager == null)
        {
            Debug.LogError("[WeaponUltimateShaderController] AbilityIconManager not found!");
        }

        if (weaponController == null)
        {
            Debug.LogError("[WeaponUltimateShaderController] WeaponController not found!");
        }
        else
        {
            // Subscribe to weapon change events
            weaponController.OnWeaponChanged += OnWeaponChanged;
        }
    }

    private void InitializeComponents()
    {
        ultimateIcon = GetComponent<Image>();
        if (ultimateIcon == null)
        {
            Debug.LogError("[WeaponUltimateShaderController] No Image component found on this GameObject!");
            return;
        }

        // Set initial material based on current weapon
        UpdateMaterialForWeapon(WeaponType.Sword);
        isInitialized = true;
    }

    private void OnWeaponChanged(WeaponSO weapon)
    {
        if (weapon != null)
        {
            UpdateMaterialForWeapon(weapon.weaponType);
        }
    }

    public void UpdateMaterialForWeapon(WeaponType weaponType)
    {
        if (!isInitialized) return;
        abilityIconManager = UnityEngine.Object.FindFirstObjectByType<AbilityIconManager>();
        weaponController = UnityEngine.Object.FindFirstObjectByType<WeaponController>();
        Material targetMaterial = null;

        switch (weaponType)
        {
            case WeaponType.Sword:
                targetMaterial = swordUltimateMaterial;
                break;
            case WeaponType.Axe:
                targetMaterial = axeUltimateMaterial;
                break;
            case WeaponType.Mage:
                targetMaterial = mageUltimateMaterial;
                break;
            default:
                targetMaterial = swordUltimateMaterial; // Default fallback
                break;
        }

        if (targetMaterial != null)
        {
            // Create material instance để không ảnh hưởng đến material gốc
            if (materialInstance != null)
            {
                DestroyImmediate(materialInstance);
            }

            materialInstance = new Material(targetMaterial);
            ultimateIcon.material = materialInstance;
            currentMaterial = targetMaterial;

            Debug.Log($"[WeaponUltimateShaderController] Switched to {weaponType} ultimate shader");
        }
        else
        {
            Debug.LogWarning($"[WeaponUltimateShaderController] No material found for {weaponType}");
        }
    }

    private void Update()
    {
        if (!isInitialized || materialInstance == null) return;

        UpdateCooldownState();
        UpdateGlowAnimation();
        UpdateShaderProperties();
    }

    private void UpdateCooldownState()
    {
        if (abilityIconManager == null) return;

        // Check if weapon is drawn
        bool isWeaponDrawn = false;
        if (weaponController != null)
        {
            var character = weaponController.GetComponent<Character>();
            if (character != null)
            {
                isWeaponDrawn = character.isWeaponDrawn;
            }
        }

        bool wasReady = isReady;
        bool isOnCooldown = abilityIconManager.IsOnCooldown(AbilityInput.Q_Ultimate);

        // Check if Ultimate is unlocked (level 60)
        bool isUltimateUnlocked = false;
        if (WeaponMasteryManager.Instance != null && currentWeaponType != WeaponType.None)
        {
            isUltimateUnlocked = WeaponMasteryManager.Instance.IsSkillUnlocked(currentWeaponType, AbilityInput.Q_Ultimate);
        }

        // Ultimate is ready when: not on cooldown AND weapon is drawn AND Ultimate is unlocked
        isReady = !isOnCooldown && isWeaponDrawn && isUltimateUnlocked;

        // Update target glow intensity based on state
        if (isReady)
        {
            targetGlowIntensity = readyGlowIntensity;
        }
        else
        {
            targetGlowIntensity = normalGlowIntensity;
        }

        // Reset pulse time when becoming ready
        if (isReady && !wasReady)
        {
            pulseTime = 0f;
        }

        // Debug log for troubleshooting
        if (isReady != wasReady)
        {
            Debug.Log($"[WeaponUltimateShaderController] Ready state changed: {wasReady} -> {isReady} (Cooldown: {isOnCooldown}, Drawn: {isWeaponDrawn})");
        }
    }

    private void UpdateGlowAnimation()
    {
        // Smooth transition for glow intensity
        currentGlowIntensity = Mathf.Lerp(currentGlowIntensity, targetGlowIntensity,
            Time.deltaTime * glowTransitionSpeed);

        // Update pulse time for ready state
        if (isReady)
        {
            pulseTime += Time.deltaTime;
        }
    }

    private void UpdateShaderProperties()
    {
        if (materialInstance == null) return;

        // Check if weapon is drawn
        bool isWeaponDrawn = false;
        if (weaponController != null)
        {
            var character = weaponController.GetComponent<Character>();
            if (character != null)
            {
                isWeaponDrawn = character.isWeaponDrawn;
            }
        }

        // Update cooldown progress
        float cooldownProgress = 0f;
        if (abilityIconManager != null && !isReady)
        {
            float remainingTime = abilityIconManager.GetRemainingCooldown(AbilityInput.Q_Ultimate);
            float totalTime = abilityIconManager.GetCooldownDuration(AbilityInput.Q_Ultimate);
            if (totalTime > 0)
            {
                cooldownProgress = 1f - (remainingTime / totalTime);
            }
        }
        materialInstance.SetFloat(cooldownProgressProperty, cooldownProgress);

        // Update glow intensity based on weapon type and drawn state
        string intensityProperty = GetIntensityPropertyForWeapon(currentWeaponType);
        if (!string.IsNullOrEmpty(intensityProperty))
        {
            // Only show glow when weapon is drawn
            float finalIntensity = isWeaponDrawn ? currentGlowIntensity : 0f;
            materialInstance.SetFloat(intensityProperty, finalIntensity);
        }

        // Update ready effects
        if (isReady && isWeaponDrawn)
        {
            materialInstance.SetFloat(readyGlowProperty, readyGlowIntensity);

            // Animated pulse for ready state
            float pulseValue = Mathf.Sin(pulseTime * 4f) * 0.5f + 0.5f;
            materialInstance.SetFloat(readyPulseProperty, pulseValue * readyPulseIntensity);
        }
        else
        {
            materialInstance.SetFloat(readyGlowProperty, 0f);
            materialInstance.SetFloat(readyPulseProperty, 0f);
        }
    }

    private string GetIntensityPropertyForWeapon(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Sword:
                return "_ThunderIntensity";
            case WeaponType.Axe:
                return "_WindIntensity";
            case WeaponType.Mage:
                return "_FireIntensity";
            default:
                return "_ThunderIntensity";
        }
    }

    // Public methods để control từ bên ngoài
    public void SetReadyState(bool ready)
    {
        isReady = ready;
        if (ready)
        {
            pulseTime = 0f;
        }
    }

    public void TriggerReadyEffect()
    {
        if (isReady)
        {
            pulseTime = 0f;
            currentGlowIntensity = readyGlowIntensity;
        }
    }

    // Animation Event để trigger từ skill animations
    public void AE_TriggerUltimateReady()
    {
        TriggerReadyEffect();
    }

    // Unassign material when weapon is sheathed
    public void UnassignMaterial()
    {
        if (ultimateIcon != null)
        {
            ultimateIcon.material = null;
            Debug.Log("[WeaponUltimateShaderController] Unassigned Ultimate shader material");
        }
    }

    private void OnDestroy()
    {
        // Clean up material instance
        if (materialInstance != null)
        {
            DestroyImmediate(materialInstance);
        }

        // Unsubscribe from events
        if (weaponController != null)
        {
            weaponController.OnWeaponChanged -= OnWeaponChanged;
        }
    }

    private void OnValidate()
    {
        // Auto-assign ultimate icon if not set
        if (ultimateIcon == null)
        {
            ultimateIcon = GetComponent<Image>();
        }
    }
}
