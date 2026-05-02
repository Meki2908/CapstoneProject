using UnityEngine;
using TMPro;

/// <summary>
/// Displays mastery level below weapon icon (for weapon selection UI)
/// </summary>
public class MasteryLevelDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Weapon Type")]
    [SerializeField] private WeaponType weaponType;

    private void Start()
    {
        UpdateDisplay();

        // Subscribe to level up events
        if (WeaponMasteryManager.Instance != null)
        {
            WeaponMasteryManager.Instance.OnLevelUp += OnLevelUp;
        }
    }

    private void OnDestroy()
    {
        if (WeaponMasteryManager.Instance != null)
        {
            WeaponMasteryManager.Instance.OnLevelUp -= OnLevelUp;
        }
    }

    private void OnLevelUp(WeaponType type, int level)
    {
        if (type == weaponType)
        {
            UpdateDisplay();
        }
    }

    public void UpdateDisplay()
    {
        if (levelText == null) return;

        if (WeaponMasteryManager.Instance != null)
        {
            int level = WeaponMasteryManager.Instance.GetMasteryLevel(weaponType);
            levelText.text = $"Lv.{level}";
        }
        else
        {
            levelText.text = "Lv.1";
        }
    }

    public void SetWeaponType(WeaponType type)
    {
        weaponType = type;
        UpdateDisplay();
    }
}

