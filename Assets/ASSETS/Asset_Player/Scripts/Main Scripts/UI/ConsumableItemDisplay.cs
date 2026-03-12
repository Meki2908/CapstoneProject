using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the consumable item display UI (Health Potion)
/// Displays quantity, handles usage on Z key press, and shows cooldown
/// </summary>
public class ConsumableItemDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon; // Icon of the health potion
    [SerializeField] private TextMeshProUGUI quantityText; // Text showing quantity (e.g., "x5")
    [SerializeField] private Image cooldownOverlay; // Radial fill image for cooldown (360 degrees)

    [Header("Health Potion Settings")]
    [Tooltip("ID of the Health Potion item in the Item database")]
    [SerializeField] private int healthPotionItemId = 0; // Default ID for Health Potion
    [Tooltip("Percentage of max health to restore (0.0 to 1.0)")]
    [SerializeField] [Range(0f, 1f)] private float healPercentage = 0.5f; // 50% by default
    [Tooltip("Cooldown time in seconds between uses")]
    [SerializeField] private float cooldownTime = 15f;

    [Header("Input Settings")]
    [Tooltip("Input action for using consumable (Z key). Leave null to use direct key input.")]
    [SerializeField] private InputActionReference useConsumableAction;
    [Tooltip("If useConsumableAction is null, use this key directly")]
    [SerializeField] private Key useKey = Key.Z;

    [Header("Auto-Find References")]
    [Tooltip("Auto-find PlayerHealth if not assigned")]
    [SerializeField] private bool autoFindPlayerHealth = true;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Heal VFX")]
    [Tooltip("Kéo mDAX_Heal.prefab từ FX/DAX/Magic Packs Vol1/Prefabs/+Heal - 6/Mobile/")]
    [SerializeField] private GameObject healVFXPrefab;

    // Internal state
    private float currentCooldown = 0f;
    private bool isOnCooldown = false;
    private int currentQuantity = 0;
    private Item healthPotionItem;

    // Input action for direct key input (if not using InputActionReference)
    private InputAction directKeyInput;

    private void Awake()
    {
        // Auto-find UI components if not assigned
        if (itemIcon == null)
            itemIcon = transform.Find("Item Icon")?.GetComponent<Image>();

        if (quantityText == null)
            quantityText = transform.Find("Quantity Text")?.GetComponent<TextMeshProUGUI>();

        if (cooldownOverlay == null)
            cooldownOverlay = transform.Find("Cooldown Overlay")?.GetComponent<Image>();

        // Setup cooldown overlay
        if (cooldownOverlay != null)
        {
            cooldownOverlay.type = Image.Type.Filled;
            cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
            cooldownOverlay.fillAmount = 0f; // Start at 0 (no cooldown)
        }

        // Auto-find PlayerHealth
        if (autoFindPlayerHealth && playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogWarning("[ConsumableItemDisplay] PlayerHealth not found! Auto-find failed.");
            }
        }

        // Setup direct key input if not using InputActionReference
        if (useConsumableAction == null)
        {
            directKeyInput = new InputAction(binding: $"<Keyboard>/{useKey}");
            directKeyInput.Enable();
        }
    }

    private void Start()
    {
        // Get Health Potion item from InventoryManager
        if (InventoryManager.Instance != null)
        {
            healthPotionItem = InventoryManager.Instance.GetItemById(healthPotionItemId);
            if (healthPotionItem != null && healthPotionItem.itemType == ItemType.Consumable)
            {
                // Update icon if available
                if (itemIcon != null && healthPotionItem.icon != null)
                {
                    itemIcon.sprite = healthPotionItem.icon;
                }
            }
            else
            {
                Debug.LogWarning($"[ConsumableItemDisplay] Health Potion item with ID {healthPotionItemId} not found or not a Consumable!");
            }
        }

        // Subscribe to inventory changes
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateQuantity;
        }

        // Initial quantity update
        UpdateQuantity();
    }

    private void Update()
    {
        // Handle input
        bool useInput = false;
        if (useConsumableAction != null)
        {
            useInput = useConsumableAction.action.WasPressedThisFrame();
        }
        else if (directKeyInput != null)
        {
            useInput = directKeyInput.WasPressedThisFrame();
        }

        if (useInput)
        {
            TryUseHealthPotion();
        }

        // Update cooldown
        if (isOnCooldown)
        {
            currentCooldown -= Time.deltaTime;
            if (currentCooldown <= 0f)
            {
                currentCooldown = 0f;
                isOnCooldown = false;
            }

            // Update cooldown overlay
            if (cooldownOverlay != null)
            {
                float fillAmount = currentCooldown / cooldownTime;
                cooldownOverlay.fillAmount = fillAmount;
            }
        }
        else
        {
            // No cooldown, hide overlay
            if (cooldownOverlay != null)
            {
                cooldownOverlay.fillAmount = 0f;
            }
        }
    }

    /// <summary>
    /// Try to use a health potion
    /// </summary>
    private void TryUseHealthPotion()
    {
        // Check cooldown
        if (isOnCooldown)
        {
            Debug.Log($"[ConsumableItemDisplay] Health potion is on cooldown! {currentCooldown:F1}s remaining");
            return;
        }

        // Check if player is alive
        if (playerHealth == null || !playerHealth.IsAlive)
        {
            Debug.LogWarning("[ConsumableItemDisplay] Cannot use health potion: PlayerHealth is null or player is dead!");
            return;
        }

        // Check if we have health potions
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[ConsumableItemDisplay] InventoryManager.Instance is null!");
            return;
        }

        int potionAmount = InventoryManager.Instance.GetItemAmount(healthPotionItemId);
        if (potionAmount <= 0)
        {
            Debug.Log("[ConsumableItemDisplay] No health potions in inventory!");
            return;
        }

        // Use the potion
        UseHealthPotion();
    }

    /// <summary>
    /// Use a health potion: heal player and remove from inventory
    /// </summary>
    private void UseHealthPotion()
    {
        // Calculate heal amount
        float healAmount = playerHealth.MaxHealth * healPercentage;

        // Heal the player
        playerHealth.Heal(healAmount);

        // Remove one potion from inventory
        InventoryManager.Instance.RemoveItem(healthPotionItemId, 1);

        // === HIỆU ỨNG HỒI MÁU ===
        if (playerHealth != null)
        {
            HealingVFX.Play(playerHealth.transform, healVFXPrefab);
        }

        // Start cooldown
        isOnCooldown = true;
        currentCooldown = cooldownTime;

        // Update quantity display
        UpdateQuantity();

        Debug.Log($"[ConsumableItemDisplay] Used health potion! Healed {healAmount:F1} HP ({healPercentage * 100f}% of max HP). Cooldown: {cooldownTime}s");
    }

    /// <summary>
    /// Update the quantity display from inventory
    /// </summary>
    private void UpdateQuantity()
    {
        if (InventoryManager.Instance == null)
        {
            currentQuantity = 0;
        }
        else
        {
            currentQuantity = InventoryManager.Instance.GetItemAmount(healthPotionItemId);
        }

        // Update UI
        if (quantityText != null)
        {
            quantityText.text = $"x{currentQuantity}";
        }

        // Hide icon if no potions
        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(currentQuantity > 0);
        }
    }

    /// <summary>
    /// Manually set the PlayerHealth reference (useful for respawn scenarios)
    /// </summary>
    public void SetPlayerHealth(PlayerHealth health)
    {
        playerHealth = health;
    }

    /// <summary>
    /// Refresh PlayerHealth reference (useful after respawn)
    /// </summary>
    public void RefreshPlayerHealth()
    {
        if (autoFindPlayerHealth)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateQuantity;
        }

        // Disable direct key input
        if (directKeyInput != null)
        {
            directKeyInput.Disable();
            directKeyInput.Dispose();
        }
    }
}

