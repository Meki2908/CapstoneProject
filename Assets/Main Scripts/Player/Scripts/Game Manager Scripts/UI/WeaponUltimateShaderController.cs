using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    // Scene transition grace period — skip shader updates for a few frames after scene load
    private int sceneLoadGraceFrames = 0;
    private const int GRACE_FRAME_COUNT = 3;
    private WeaponType currentWeaponType = WeaponType.Sword;
    
    // Anti-flicker grace window after draw/weapon change (Fusion sync can be delayed a few frames)
    private int cooldownGraceFrames = 0;
    private bool lastWeaponDrawnState = false; // Edge detection for draw/sheath (UI-driven)

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        RefreshReferences();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // Re-find references in case we were re-enabled after scene load
        RefreshReferences();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Scene mới load xong → force tìm lại references + grace period
        sceneLoadGraceFrames = GRACE_FRAME_COUNT;
        RefreshReferences();
    }

    private void RefreshReferences()
    {
        abilityIconManager = FindFirstObjectByType<AbilityIconManager>();

        // 1) Tạo biến tạm để lưu Controller mới tìm được (đúng LocalPlayer trong Fusion)
        WeaponController newWeaponController = null;

        if (Character.LocalCharacter != null)
        {
            newWeaponController = Character.LocalCharacter.GetComponent<WeaponController>();
            if (newWeaponController == null)
                newWeaponController = Character.LocalCharacter.GetComponentInChildren<WeaponController>(true);
        }
        else
        {
            // In multiplayer, avoid binding to arbitrary player while LocalCharacter is not ready.
            // Offline fallback is allowed when there is no active Fusion runner.
            var anyCharacter = FindFirstObjectByType<Character>();
            bool fusionRunning = anyCharacter != null && anyCharacter.Runner != null && anyCharacter.Runner.IsRunning;
            newWeaponController = fusionRunning ? null : FindFirstObjectByType<WeaponController>();
        }

        // 2) Chỉ xử lý Subscribe/Unsubscribe nếu Player thực sự thay đổi
        if (newWeaponController != weaponController)
        {
            if (weaponController != null)
            {
                weaponController.OnWeaponChanged -= OnWeaponChanged;
            }

            weaponController = newWeaponController;

            if (weaponController != null)
            {
                weaponController.OnWeaponChanged += OnWeaponChanged;

                // Tránh "Missed Event" lúc khởi tạo: sync vũ khí hiện tại ngay lập tức
                WeaponSO currentWp = weaponController.GetCurrentWeapon();
                if (currentWp != null)
                {
                    UpdateMaterialForWeapon(currentWp.weaponType);
                }

                // Initialize edge-detection state based on current local character.
                // Prevents "fake draw edge" when entering a scene where weapon is already drawn.
                lastWeaponDrawnState = Character.LocalCharacter != null && Character.LocalCharacter.isWeaponDrawn;
            }
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
        // Weapon swap while already drawn: update shader material and apply a short grace window.
        if (weapon == null) return;
        UpdateMaterialForWeapon(weapon.weaponType);
        cooldownGraceFrames = 5;
    }

    public void UpdateMaterialForWeapon(WeaponType weaponType)
    {
        if (!isInitialized) return;

        currentWeaponType = weaponType;
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
        }
        else
        {
            Debug.LogWarning($"[WeaponUltimateShaderController] No material found for {weaponType}");
        }
    }

    private void Update()
    {
        // 1) Quét Player trước để tránh bị return sớm chặn retry
        if (abilityIconManager == null || weaponController == null)
        {
            if (Time.frameCount % 30 == 0) RefreshReferences();
            return;
        }

        // 2) Nếu thiếu Image component hoặc init fail thì ngắt luôn
        if (!isInitialized)
        {
            return;
        }

        // 3) Grace period sau scene load — skip update để tránh flicker
        if (sceneLoadGraceFrames > 0)
        {
            sceneLoadGraceFrames--;
            if (sceneLoadGraceFrames == 0)
            {
                RefreshReferences();
            }
            return;
        }

        // 4) UI-driven draw/sheath edge detection (Fusion safe: only uses local character state)
        var charTarget = Character.LocalCharacter;
        bool isDrawnNow = charTarget != null && charTarget.isWeaponDrawn;

        if (isDrawnNow != lastWeaponDrawnState)
        {
            if (isDrawnNow)
            {
                // Draw: re-assign material for current weapon + grace frames to avoid sync flicker
                cooldownGraceFrames = 5;
                WeaponSO currentWp = weaponController != null ? weaponController.GetCurrentWeapon() : null;
                if (currentWp != null)
                    UpdateMaterialForWeapon(currentWp.weaponType);
            }
            else
            {
                // Sheath: unassign material & force off ready state
                UnassignMaterial();
                SetReadyState(false);
            }

            lastWeaponDrawnState = isDrawnNow;
        }

        // If we are sheathed (no material), skip shader updates.
        if (materialInstance == null)
        {
            return;
        }

        // 5) Everything ready → update shader
        UpdateCooldownState();
        UpdateGlowAnimation();
        UpdateShaderProperties();
    }

    private void UpdateCooldownState()
    {
        if (abilityIconManager == null) return;

        // Check if weapon is drawn (Fusion-safe: always read LocalCharacter)
        var localChar = Character.LocalCharacter;
        bool isWeaponDrawn = localChar != null && localChar.isWeaponDrawn;

        bool wasReady = isReady;
        bool isOnCooldown = abilityIconManager.IsOnCooldown(AbilityInput.Q_Ultimate);

        // Check if Ultimate is unlocked (level 60)
        bool isUltimateUnlocked = false;
        if (WeaponMasteryManager.Instance != null && currentWeaponType != WeaponType.None)
        {
            isUltimateUnlocked = WeaponMasteryManager.Instance.IsSkillUnlocked(currentWeaponType, AbilityInput.Q_Ultimate);
        }

        // Ultimate is ready when: not on cooldown AND weapon is drawn AND Ultimate is unlocked
        bool readyNow = !isOnCooldown && isWeaponDrawn && isUltimateUnlocked;

        // Grace frames: Fusion can deliver cooldown/unlock data a few frames late.
        // If we were ready and immediately read "not ready" during the grace window,
        // keep ready unless the player sheathed (materialInstance would be null and Update would have returned).
        if (cooldownGraceFrames > 0)
        {
            cooldownGraceFrames--;
            if (isReady && !readyNow && isOnCooldown)
                readyNow = true;
        }

        isReady = readyNow;

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

        // Check if weapon is drawn (Fusion-safe: always read LocalCharacter)
        var localChar = Character.LocalCharacter;
        bool isWeaponDrawn = localChar != null && localChar.isWeaponDrawn;

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
        }

        // Ensure Update() can detect sheathed state and avoid leaking instances.
        if (materialInstance != null)
        {
            DestroyImmediate(materialInstance);
            materialInstance = null;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (weaponController != null)
        {
            weaponController.OnWeaponChanged -= OnWeaponChanged;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
