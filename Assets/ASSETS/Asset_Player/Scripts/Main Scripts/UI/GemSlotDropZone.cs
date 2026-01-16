using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drop zone for gem slots - accepts gem drops and equips them
/// Supports double-click to remove gem
/// </summary>
public class GemSlotDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Slot Settings")]
    [SerializeField] private int slotIndex; // 0, 1, or 2
    [SerializeField] private Color emptySlotColor = Color.black; // Color when slot is empty (default: black)
    [SerializeField] private float doubleClickTime = 0.3f; // Time window for double click (seconds)

    private WeaponForgeUI forgeUI;
    private WeaponType currentWeaponType = WeaponType.None;
    private Image slotImage; // Use this component's Image to display gem icon
    private Color originalColor; // Store original color
    
    // Double click detection
    private float lastClickTime = 0f;

    private void Awake()
    {
        slotImage = GetComponent<Image>();
        if (slotImage == null)
        {
            slotImage = gameObject.AddComponent<Image>();
        }

        // Store original color
        originalColor = slotImage.color;

        forgeUI = GetComponentInParent<WeaponForgeUI>();
    }

    public void SetWeaponType(WeaponType weaponType)
    {
        currentWeaponType = weaponType;
    }

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    public bool CanAcceptGem(Item gemItem)
    {
        if (gemItem == null || gemItem.itemType != ItemType.Gems) return false;
        if (currentWeaponType == WeaponType.None) return false;
        if (WeaponGemManager.Instance == null) return false;

        // Accept any gem type into any slot
        return true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        GemItemUI gemUI = eventData.pointerDrag?.GetComponent<GemItemUI>();
        if (gemUI == null || gemUI.GemItem == null) return;

        if (CanAcceptGem(gemUI.GemItem))
        {
            // Equip the gem into this slot
            if (WeaponGemManager.Instance != null)
            {
                WeaponGemManager.Instance.EquipGem(currentWeaponType, slotIndex, gemUI.GemItem);
            }
            // Refresh forge UI
            if (forgeUI != null)
            {
                forgeUI.RefreshAfterGemEquip();
            }
        }
    }

    /// <summary>
    /// Set the slot icon (called after gem is equipped)
    /// </summary>
    public void SetSlotIcon(Sprite icon)
    {
        if (slotImage != null)
        {
            if (icon != null)
            {
                slotImage.sprite = icon;
                slotImage.color = originalColor; // Restore original color when has icon
            }
            else
            {
                slotImage.sprite = null;
                slotImage.color = emptySlotColor; // Set to black when empty
            }
            // Ensure image is enabled
            slotImage.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[GemSlotDropZone] slotImage is null on slot {slotIndex}!");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // No highlight needed
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // No highlight needed
    }

    /// <summary>
    /// Handle double-click to remove gem from slot
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Check for double click
        float currentTime = Time.time;
        if (currentTime - lastClickTime < doubleClickTime)
        {
            // Double click detected - remove gem
            RemoveGem();
        }
        lastClickTime = currentTime;
    }

    /// <summary>
    /// Remove gem from this slot
    /// </summary>
    private void RemoveGem()
    {
        if (WeaponGemManager.Instance == null || currentWeaponType == WeaponType.None) return;

        // Check if slot has a gem
        var gem = WeaponGemManager.Instance.GetEquippedGem(currentWeaponType, slotIndex);
        if (gem == null)
        {
            Debug.Log($"[GemSlotDropZone] Slot {slotIndex} is already empty");
            return;
        }

        // Remove gem
        bool removed = WeaponGemManager.Instance.RemoveGem(currentWeaponType, slotIndex);
        if (removed)
        {
            Debug.Log($"[GemSlotDropZone] Removed gem from slot {slotIndex}");
            // Refresh forge UI
            if (forgeUI != null)
            {
                forgeUI.RefreshAfterGemEquip();
            }
        }
        else
        {
            Debug.LogWarning($"[GemSlotDropZone] Failed to remove gem from slot {slotIndex}");
        }
    }
}

