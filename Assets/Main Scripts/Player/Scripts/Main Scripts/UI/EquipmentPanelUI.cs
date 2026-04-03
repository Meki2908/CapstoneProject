using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Equipment Panel UI: hiện 4 slot trang bị (chỉ xem)
/// Trang bị / gỡ equipment chỉ dùng ở NPC Thợ Rèn (BlacksmithUI)
/// </summary>
public class EquipmentPanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject equipmentPanel;

    [Header("Equipment Slots")]
    [SerializeField] private EquipmentSlotDropZone[] equipmentSlotDropZones = new EquipmentSlotDropZone[4];

    private void Awake()
    {
        // Setup equipment slot drop zones (by index)
        for (int i = 0; i < equipmentSlotDropZones.Length && i < 4; i++)
        {
            if (equipmentSlotDropZones[i] != null)
            {
                equipmentSlotDropZones[i].SetSlotIndex(i);
            }
        }

        // Subscribe to equipment changes
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged += OnEquipmentChanged;
        }
    }

    private void Start()
    {
        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        RefreshEquipmentSlots();
    }

    private void OnDestroy()
    {
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged -= OnEquipmentChanged;
        }
    }

    private void OnEquipmentChanged()
    {
        RefreshEquipmentSlots();
    }

    /// <summary>
    /// Open the equipment panel
    /// </summary>
    public void OpenPanel()
    {
        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(true);
        }
        RefreshEquipmentSlots();
    }

    /// <summary>
    /// Close the equipment panel
    /// </summary>
    public void ClosePanel()
    {
        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Refresh UI after equipment equip/remove (called from BlacksmithUI)
    /// </summary>
    public void RefreshAfterEquip()
    {
        StartCoroutine(RefreshAfterDelay());
    }

    private IEnumerator RefreshAfterDelay()
    {
        yield return new WaitForEndOfFrame();
        RefreshEquipmentSlots();
    }

    private void RefreshEquipmentSlots()
    {
        if (equipmentSlotDropZones == null || equipmentSlotDropZones.Length < 4)
        {
            Debug.LogWarning("[EquipmentPanelUI] equipmentSlotDropZones is null or length < 4!");
            return;
        }
        
        if (EquipmentManager.Instance == null)
        {
            for (int i = 0; i < equipmentSlotDropZones.Length; i++)
            {
                if (equipmentSlotDropZones[i] != null)
                {
                    equipmentSlotDropZones[i].SetSlotIcon(null);
                }
            }
            return;
        }

        // Refresh slots by index
        for (int i = 0; i < 4; i++)
        {
            var item = EquipmentManager.Instance.GetEquippedItemByIndex(i);
            if (equipmentSlotDropZones[i] != null)
            {
                Sprite itemIcon = item != null && item.icon != null ? item.icon : null;
                equipmentSlotDropZones[i].SetSlotIcon(itemIcon);
            }
        }
    }

    // Equip Best, Remove All, Equipment Viewport đã chuyển sang NPC Thợ Rèn (BlacksmithUI)
}
