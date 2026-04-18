using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays mastery progression: circular level indicator, exp bar, and exp text
/// </summary>
public class MasteryProgressionDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelText; // Text in the circle
    [SerializeField] private Image expBarFill; // The fill image of the exp bar
    [SerializeField] private TextMeshProUGUI expText; // "cur xp / max xp" text

    [Header("Weapon Reference")]
    [SerializeField] private WeaponSO currentWeaponSO; // Optional: can be set manually, otherwise auto-finds
    [SerializeField] private WeaponController weaponController; // Auto-find if not assigned

    private WeaponType currentWeaponType = WeaponType.None;

    private void Awake()
    {
        // Auto-find WeaponController if not assigned
        if (weaponController == null)
        {
            weaponController = FindFirstObjectByType<WeaponController>();
        }
    }

    private void Start()
    {
        // Subscribe to weapon change events
        if (weaponController != null)
        {
            weaponController.OnWeaponChanged += OnWeaponChanged;
            // Set initial weapon
            if (weaponController.GetCurrentWeapon() != null)
            {
                SetWeapon(weaponController.GetCurrentWeapon());
            }
        }
        else if (currentWeaponSO != null)
        {
            // Use manually assigned weapon if no controller found
            SetWeapon(currentWeaponSO);
        }

        UpdateDisplay();

        // Subscribe to mastery events
        if (WeaponMasteryManager.Instance != null)
        {
            WeaponMasteryManager.Instance.OnLevelUp += OnLevelUp;
            WeaponMasteryManager.Instance.OnExpGained += OnExpGained;
        }
    }

    private void OnDestroy()
    {
        if (weaponController != null)
        {
            weaponController.OnWeaponChanged -= OnWeaponChanged;
        }

        if (WeaponMasteryManager.Instance != null)
        {
            WeaponMasteryManager.Instance.OnLevelUp -= OnLevelUp;
            WeaponMasteryManager.Instance.OnExpGained -= OnExpGained;
        }
    }

    private void OnWeaponChanged(WeaponSO weapon)
    {
        SetWeapon(weapon);
    }

    private void OnLevelUp(WeaponType type, int level)
    {
        if (type == currentWeaponType)
        {
            UpdateDisplay();
        }
    }

    private void OnExpGained(WeaponType type)
    {
        if (type == currentWeaponType)
        {
            UpdateDisplay();
        }
    }

    public void SetWeapon(WeaponSO weaponSO)
    {
        currentWeaponSO = weaponSO;
        if (weaponSO != null)
        {
            currentWeaponType = weaponSO.weaponType;
        }
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (WeaponMasteryManager.Instance == null || currentWeaponSO == null)
        {
            // Default display
            if (levelText != null) levelText.text = "1";
            if (expBarFill != null) expBarFill.fillAmount = 0f;
            if (expText != null) expText.text = "0 / 0";
            return;
        }

        var masteryData = WeaponMasteryManager.Instance.GetMasteryData(currentWeaponType);
        int level = masteryData.currentLevel;
        float currentExp = masteryData.currentExp;
        float expRequired = currentWeaponSO.GetExpRequiredForNextLevel(level);

        // Update level text
        if (levelText != null)
        {
            levelText.text = level.ToString();
        }

        // Update exp bar
        if (expBarFill != null)
        {
            float fillAmount = expRequired > 0 ? Mathf.Clamp01(currentExp / expRequired) : 0f;
            expBarFill.fillAmount = fillAmount;
        }

        // Update exp text
        if (expText != null)
        {
            expText.text = $"{Mathf.FloorToInt(currentExp)} / {Mathf.FloorToInt(expRequired)}";
        }
    }
}

