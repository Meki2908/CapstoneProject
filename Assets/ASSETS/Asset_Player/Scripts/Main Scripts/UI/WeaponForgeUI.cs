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

    [Header("Gems Viewport")]
    [SerializeField] private Transform gemsViewportContent;
    [SerializeField] private InventoryController inventoryController; // To get itemUIPrefab

    private GameObject itemUIPrefab; // Shared prefab from InventoryController

    private WeaponType currentWeaponType = WeaponType.None;
    private List<GemItemUI> currentGemUIs = new List<GemItemUI>();
    private WeaponSO currentWeaponSO; // Store current weapon for icon

    private void Awake()
    {
        if (weaponController == null)
        {
            weaponController = FindFirstObjectByType<WeaponController>();
        }

        // Get itemUIPrefab from InventoryController
        if (inventoryController == null)
        {
            inventoryController = FindFirstObjectByType<InventoryController>();
        }

        // Setup gem slot drop zones
        for (int i = 0; i < gemSlotDropZones.Length && i < 3; i++)
        {
            if (gemSlotDropZones[i] != null)
            {
                gemSlotDropZones[i].SetSlotIndex(i);
            }
        }

        // Subscribe to inventory changes
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshGemsViewport;
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

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshGemsViewport;
        }
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
            RefreshGemsViewport(); // Refresh to show updated inventory
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
        RefreshGemsViewport();
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

    // Button: Equip Best
    public void OnClick_EquipBest()
    {
        if (WeaponGemManager.Instance == null || currentWeaponType == WeaponType.None) return;
        WeaponGemManager.Instance.EquipBest(currentWeaponType);
        RefreshAfterGemEquip();
    }

    /// <summary>
    /// Refresh UI after gem equip/remove
    /// </summary>
    public void RefreshAfterGemEquip()
    {
        Debug.Log("[WeaponForgeUI] RefreshAfterGemEquip called");
        // Small delay to ensure EquipGem has finished saving
        StartCoroutine(RefreshAfterDelay());
    }

    private System.Collections.IEnumerator RefreshAfterDelay()
    {
        yield return new WaitForEndOfFrame();
        RefreshGemSlots();
        RefreshGemsViewport();
    }

    // Button: Remove All
    public void OnClick_RemoveAll()
    {
        if (WeaponGemManager.Instance == null || currentWeaponType == WeaponType.None) return;
        WeaponGemManager.Instance.RemoveAll(currentWeaponType);
        RefreshGemSlots();
        RefreshGemsViewport(); // Refresh to show returned gems
    }

    /// <summary>
    /// Refresh the gems viewport with gems from inventory
    /// Uses the same itemUIPrefab from InventoryController
    /// </summary>
    private void RefreshGemsViewport()
    {
        if (gemsViewportContent == null) return;

        // Get itemUIPrefab from InventoryController
        if (itemUIPrefab == null && inventoryController != null)
        {
            itemUIPrefab = inventoryController.ItemUIPrefab;
        }

        if (itemUIPrefab == null)
        {
            Debug.LogWarning("[WeaponForgeUI] itemUIPrefab is null! Make sure InventoryController has itemUIPrefab assigned.");
            return;
        }

        // Clear existing UI
        foreach (Transform child in gemsViewportContent)
        {
            Destroy(child.gameObject);
        }
        currentGemUIs.Clear();

        // Get all gems from inventory
        if (InventoryManager.Instance != null)
        {
            var allItems = InventoryManager.Instance.GetAllItems();
            foreach (var (item, amount) in allItems)
            {
                if (item != null && item.itemType == ItemType.Gems)
                {
                    GameObject gemUIObject = Instantiate(itemUIPrefab, gemsViewportContent);
                    
                    // Add GemItemUI component if not exists (for drag & drop)
                    GemItemUI gemUI = gemUIObject.GetComponent<GemItemUI>();
                    if (gemUI == null)
                    {
                        gemUI = gemUIObject.AddComponent<GemItemUI>();
                    }
                    
                    if (gemUI != null)
                    {
                        gemUI.Initialize(item, amount);
                        currentGemUIs.Add(gemUI);
                    }
                }
            }
        }
    }
}


