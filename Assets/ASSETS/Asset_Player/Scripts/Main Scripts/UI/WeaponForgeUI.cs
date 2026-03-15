using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Weapon Forge UI: display weapon icon, 3 gem slots, mastery text, and actions (Equip Best / Remove All)
/// </summary>
public class WeaponForgeUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject forgePanel;

    [Header("References")]
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private Image weaponIconImage;

    [Header("Gem Slots (any gem type can go into any slot)")]
    [SerializeField] private GemSlotDropZone[] gemSlotDropZones = new GemSlotDropZone[3];

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI masteryText;

    private WeaponType currentWeaponType = WeaponType.None;
    private WeaponSO currentWeaponSO; // Store current weapon for icon

    private void Awake()
    {
        if (weaponController == null)
        {
            weaponController = FindFirstObjectByType<WeaponController>();
        }



        // Setup gem slot drop zones
        for (int i = 0; i < gemSlotDropZones.Length && i < 3; i++)
        {
            if (gemSlotDropZones[i] != null)
            {
                gemSlotDropZones[i].SetSlotIndex(i);
            }
        }


    }

    private void Start()
    {
        if (forgePanel != null)
        {
            forgePanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        Subscribe();
        RefreshAll();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }



    private void Subscribe()
    {
        if (weaponController != null)
        {
            weaponController.OnWeaponChanged -= OnWeaponChanged;
            weaponController.OnWeaponChanged += OnWeaponChanged;
        }
        if (WeaponGemManager.Instance != null)
        {
            WeaponGemManager.Instance.OnGemsChanged -= OnGemsChanged;
            WeaponGemManager.Instance.OnGemsChanged += OnGemsChanged;
        }
        if (WeaponMasteryManager.Instance != null)
        {
            WeaponMasteryManager.Instance.OnLevelUp -= OnMasteryLevelUp;
            WeaponMasteryManager.Instance.OnLevelUp += OnMasteryLevelUp;
        }
    }

    private void Unsubscribe()
    {
        if (weaponController != null)
        {
            weaponController.OnWeaponChanged -= OnWeaponChanged;
        }
        if (WeaponGemManager.Instance != null)
        {
            WeaponGemManager.Instance.OnGemsChanged -= OnGemsChanged;
        }
        if (WeaponMasteryManager.Instance != null)
        {
            WeaponMasteryManager.Instance.OnLevelUp -= OnMasteryLevelUp;
        }
    }

    private void OnWeaponChanged(WeaponSO weapon)
    {
        SetWeapon(weapon);
    }

    private void OnGemsChanged(WeaponType wt)
    {
        if (wt == currentWeaponType)
        {
            Debug.Log($"[WeaponForgeUI] OnGemsChanged for {wt}, refreshing slots...");
            RefreshGemSlots();
        }
    }

    private void OnMasteryLevelUp(WeaponType wt, int level)
    {
        if (wt == currentWeaponType)
        {
            RefreshMasteryText();
        }
    }

    /// <summary>
    /// Open the forge panel for a specific weapon
    /// </summary>
    public void OpenForge(WeaponSO weapon)
    {
        if (forgePanel != null)
        {
            forgePanel.SetActive(true);
        }
        SetWeapon(weapon);
    }

    /// <summary>
    /// Close the forge panel
    /// </summary>
    public void CloseForge()
    {
        if (forgePanel != null)
        {
            forgePanel.SetActive(false);
        }
    }

    public void SetWeapon(WeaponSO weapon)
    {
        currentWeaponSO = weapon;
        if (weapon == null)
        {
            currentWeaponType = WeaponType.None;
            if (weaponIconImage != null) weaponIconImage.sprite = null;
            RefreshMasteryText();
            RefreshGemSlots();
            UpdateDropZones();
            return;
        }
        currentWeaponType = weapon.weaponType;
        if (weaponIconImage != null && weapon.icon != null)
        {
            weaponIconImage.sprite = weapon.icon;
        }
        RefreshMasteryText();
        RefreshGemSlots();
        UpdateDropZones();
    }

    private void UpdateDropZones()
    {
        for (int i = 0; i < gemSlotDropZones.Length; i++)
        {
            if (gemSlotDropZones[i] != null)
            {
                gemSlotDropZones[i].SetWeaponType(currentWeaponType);
            }
        }
    }

    private void RefreshAll()
    {
        if (weaponController != null && weaponController.GetCurrentWeapon() != null)
        {
            SetWeapon(weaponController.GetCurrentWeapon());
        }
        else
        {
            RefreshMasteryText();
            RefreshGemSlots();
        }
    }

    private void RefreshMasteryText()
    {
        if (masteryText == null) return;
        if (WeaponMasteryManager.Instance != null && currentWeaponType != WeaponType.None)
        {
            int level = WeaponMasteryManager.Instance.GetMasteryLevel(currentWeaponType);
            masteryText.text = $"Lv.{level}";
        }
        else
        {
            masteryText.text = "Lv.-";
        }
    }

    private void RefreshGemSlots()
    {
        if (gemSlotDropZones == null || gemSlotDropZones.Length < 3)
        {
            Debug.LogWarning("[WeaponForgeUI] gemSlotDropZones is null or length < 3!");
            return;
        }
        
        if (WeaponGemManager.Instance == null || currentWeaponType == WeaponType.None)
        {
            // Clear to empty
            for (int i = 0; i < gemSlotDropZones.Length; i++)
            {
                if (gemSlotDropZones[i] != null)
                {
                    gemSlotDropZones[i].SetSlotIcon(null);
                }
            }
            return;
        }

        for (int i = 0; i < 3; i++)
        {
            var gem = WeaponGemManager.Instance.GetEquippedGem(currentWeaponType, i);
            if (gemSlotDropZones[i] != null)
            {
                Sprite gemIcon = gem != null && gem.icon != null ? gem.icon : null;
                gemSlotDropZones[i].SetSlotIcon(gemIcon);
                Debug.Log($"[WeaponForgeUI] Slot {i}: {(gem != null ? gem.itemName : "empty")}, Icon: {(gemIcon != null ? "set" : "null")}");
            }
        }
    }

    // Equip Best, Remove All, Gems Viewport đã chuyển sang NPC Thợ Rèn (BlacksmithUI)

}


