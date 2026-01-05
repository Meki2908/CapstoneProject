using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controller cho Ultimate Icon Shader với hiệu ứng glow khi hồi chiêu
/// </summary>
public class UltimateIconShaderController : MonoBehaviour
{
    [Header("Shader References")]
    [SerializeField] private Image ultimateIcon;
    [SerializeField] private Material ultimateMaterial;

    [Header("Shader Properties")]
    [SerializeField] private string cooldownProgressProperty = "_CooldownProgress";
    [SerializeField] private string glowIntensityProperty = "_GlowIntensity";
    [SerializeField] private string readyGlowProperty = "_ReadyGlow";
    [SerializeField] private string readyPulseProperty = "_ReadyPulse";

    [Header("Effect Settings")]
    [SerializeField] private float readyGlowIntensity = 2.5f;
    [SerializeField] private float readyPulseIntensity = 0.8f;
    [SerializeField] private float normalGlowIntensity = 1.0f;

    [Header("Animation Settings")]
    [SerializeField] private float glowTransitionSpeed = 5.0f;
    [SerializeField] private AnimationCurve readyPulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private AbilityIconManager abilityIconManager;
    private WeaponController weaponController;
    private Material materialInstance;
    private bool isInitialized = false;

    // Animation state
    private float targetGlowIntensity;
    private float currentGlowIntensity;
    private bool isReady = false;
    private float pulseTime = 0f;

    private void Awake()
    {
        InitializeMaterial();
    }

    private void Start()
    {
        abilityIconManager = UnityEngine.Object.FindFirstObjectByType<AbilityIconManager>();
        weaponController = UnityEngine.Object.FindFirstObjectByType<WeaponController>();
        if (abilityIconManager == null)
        {
            Debug.LogError("[UltimateIconShaderController] AbilityIconManager not found!");
        }
        if (weaponController == null)
        {
            Debug.LogError("[UltimateIconShaderController] WeaponController not found!");
        }
    }

    private void InitializeMaterial()
    {
        if (ultimateIcon == null)
        {
            ultimateIcon = GetComponent<Image>();
        }

        if (ultimateIcon != null && ultimateMaterial != null)
        {
            // Create material instance để không ảnh hưởng đến material gốc
            materialInstance = new Material(ultimateMaterial);
            ultimateIcon.material = materialInstance;
            isInitialized = true;
            Debug.Log("[UltimateIconShaderController] Material initialized successfully");
        }
        else
        {
            Debug.LogError("[UltimateIconShaderController] UltimateIcon or UltimateMaterial not assigned!");
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

        bool wasReady = isReady;
        bool isOnCooldown = abilityIconManager.IsOnCooldown(AbilityInput.Q_Ultimate);

        // Check if Ultimate is unlocked (level 60)
        bool isUltimateUnlocked = false;
        if (WeaponMasteryManager.Instance != null && weaponController != null)
        {
            WeaponSO currentWeapon = weaponController.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                isUltimateUnlocked = WeaponMasteryManager.Instance.IsSkillUnlocked(currentWeapon.weaponType, AbilityInput.Q_Ultimate);
            }
        }

        // Ultimate is ready when: not on cooldown AND Ultimate is unlocked
        isReady = !isOnCooldown && isUltimateUnlocked;

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

        // Update glow intensity
        materialInstance.SetFloat(glowIntensityProperty, currentGlowIntensity);

        // Update ready effects
        if (isReady)
        {
            materialInstance.SetFloat(readyGlowProperty, readyGlowIntensity);

            // Animated pulse for ready state
            float pulseValue = readyPulseCurve.Evaluate(Mathf.PingPong(pulseTime * 2f, 1f));
            materialInstance.SetFloat(readyPulseProperty, pulseValue * readyPulseIntensity);
        }
        else
        {
            materialInstance.SetFloat(readyGlowProperty, 0f);
            materialInstance.SetFloat(readyPulseProperty, 0f);
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

    private void OnDestroy()
    {
        // Clean up material instance
        if (materialInstance != null)
        {
            DestroyImmediate(materialInstance);
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
