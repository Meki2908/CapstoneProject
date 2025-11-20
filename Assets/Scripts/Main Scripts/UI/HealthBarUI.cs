using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages the player health bar UI with fill bar and delayed damage bar
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("Health Bar References")]
    [SerializeField] private Image healthFillBar; // Main health fill bar
    [SerializeField] private Image delayedDamageBar; // Delayed damage bar (follows behind)

    [Header("Delayed Damage Settings")]
    [SerializeField] private float delayedDamageSpeed = 1f; // Speed of delayed bar following
    [SerializeField] private float delayedDamageDelay = 0.5f; // Delay before delayed bar starts moving
    [SerializeField] private Color delayedDamageColor = new Color(1f, 0.5f, 0f, 1f); // Orange color for delayed damage bar

    [Header("Health Bar Colors")]
    [SerializeField] private Color healthyColor = new Color(0f, 1f, 0f, 1f); // Green color when HP > 40%
    [SerializeField] private Color lowHealthColor = new Color(1f, 0f, 0f, 1f); // Red color when HP <= 40%
    [SerializeField] private float lowHealthThreshold = 0.4f; // 40% threshold

    [Header("Auto Find Settings")]
    [SerializeField] private bool autoFindPlayerHealth = true;
    [SerializeField] private string playerTag = "Player"; // Tag to find player if auto-find is enabled

    private PlayerHealth playerHealth;
    private float targetHealthFill = 1f;
    private float currentDelayedFill = 1f;
    private Coroutine delayedDamageCoroutine;

    private void Start()
    {
        // Auto-find PlayerHealth
        if (autoFindPlayerHealth)
        {
            FindPlayerHealth();
        }

        // Initialize bars
        if (healthFillBar != null)
        {
            healthFillBar.fillAmount = 1f;
            healthFillBar.color = healthyColor; // Start with green color
        }
        if (delayedDamageBar != null)
        {
            delayedDamageBar.fillAmount = 1f;
            delayedDamageBar.color = delayedDamageColor; // Set orange color
            currentDelayedFill = 1f;
        }
    }

    private void Update()
    {
        // Update delayed damage bar
        if (delayedDamageBar != null && playerHealth != null)
        {
            UpdateDelayedDamageBar();
        }
    }

    /// <summary>
    /// Auto-find PlayerHealth component
    /// </summary>
    private void FindPlayerHealth()
    {
        // Try to find by tag first
        if (!string.IsNullOrEmpty(playerTag))
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
            {
                playerHealth = playerObject.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    SubscribeToHealthEvents();
                    return;
                }
            }
        }

        // Try FindObjectOfType as fallback
        playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            SubscribeToHealthEvents();
        }
        else
        {
            Debug.LogWarning("[HealthBarUI] PlayerHealth not found! Make sure PlayerHealth component exists in the scene.");
        }
    }

    /// <summary>
    /// Subscribe to PlayerHealth events
    /// </summary>
    private void SubscribeToHealthEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += OnHealthChanged;
            playerHealth.OnPlayerDied += OnPlayerDied;

            // Initialize with current health
            // Set targetHealthFill to current health percentage
            // But DON'T start animation - just sync the values
            float currentHealthPercent = playerHealth.CurrentHealth / playerHealth.MaxHealth;
            targetHealthFill = currentHealthPercent;

            // Update health bar immediately without animation
            if (healthFillBar != null)
            {
                healthFillBar.fillAmount = targetHealthFill;
                UpdateHealthBarColor(targetHealthFill);
            }

            // Set delayed bar to match (no animation on initialization)
            if (delayedDamageBar != null)
            {
                currentDelayedFill = targetHealthFill;
                delayedDamageBar.fillAmount = currentDelayedFill;
            }
        }
    }

    /// <summary>
    /// Unsubscribe from PlayerHealth events
    /// </summary>
    private void UnsubscribeFromHealthEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= OnHealthChanged;
            playerHealth.OnPlayerDied -= OnPlayerDied;
        }
    }

    /// <summary>
    /// Called when player health changes
    /// </summary>
    private void OnHealthChanged(float currentHealth)
    {
        if (playerHealth != null)
        {
            UpdateHealthBar(currentHealth, playerHealth.MaxHealth);
        }
    }

    /// <summary>
    /// Called when player dies
    /// </summary>
    private void OnPlayerDied()
    {
        // Handle player death UI updates if needed
        Debug.Log("[HealthBarUI] Player died - health bar updated");
    }

    /// <summary>
    /// Update the health bar fill amount
    /// </summary>
    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (maxHealth <= 0f) return;

        // Calculate fill amount (0 to 1)
        float newTargetHealthFill = currentHealth / maxHealth;

        // Only start animation if health actually decreased
        // If health increased (healing), update delayed bar immediately
        if (newTargetHealthFill > targetHealthFill)
        {
            // Healing: update both bars immediately
            targetHealthFill = newTargetHealthFill;
            currentDelayedFill = newTargetHealthFill;

            if (healthFillBar != null)
            {
                healthFillBar.fillAmount = targetHealthFill;
                UpdateHealthBarColor(targetHealthFill);
            }
            if (delayedDamageBar != null)
            {
                delayedDamageBar.fillAmount = currentDelayedFill;
            }
            return;
        }

        // Damage received: update health bar immediately, delay the delayed bar
        // IMPORTANT: Save the current delayed fill BEFORE updating target
        // This ensures we start animation from the correct position
        float previousDelayedFill = currentDelayedFill;
        targetHealthFill = newTargetHealthFill;

        // Update main health fill bar immediately
        if (healthFillBar != null)
        {
            healthFillBar.fillAmount = targetHealthFill;
            // Update color based on health percentage
            UpdateHealthBarColor(targetHealthFill);
        }

        // Start delayed damage bar animation
        // Use the previous delayed fill value, not the current delayed bar value
        // This ensures it starts from where it was, not where it is now
        if (delayedDamageBar != null)
        {
            // Restore the previous value to ensure animation starts correctly
            currentDelayedFill = previousDelayedFill;
            // Make sure delayed bar is at the previous position too
            delayedDamageBar.fillAmount = currentDelayedFill;
            StartDelayedDamageAnimation();
        }
    }

    /// <summary>
    /// Update health bar color based on health percentage
    /// </summary>
    private void UpdateHealthBarColor(float healthPercentage)
    {
        if (healthFillBar == null) return;

        // Change color based on threshold
        if (healthPercentage > lowHealthThreshold)
        {
            // HP > 40%: Green
            healthFillBar.color = healthyColor;
        }
        else
        {
            // HP <= 40%: Red
            healthFillBar.color = lowHealthColor;
        }
    }

    /// <summary>
    /// Start the delayed damage bar animation
    /// </summary>
    private void StartDelayedDamageAnimation()
    {
        // Stop existing coroutine if running
        if (delayedDamageCoroutine != null)
        {
            StopCoroutine(delayedDamageCoroutine);
            delayedDamageCoroutine = null;
        }

        // Only start animation if there's actually a difference
        // currentDelayedFill should already be set correctly in UpdateHealthBar
        if (currentDelayedFill > targetHealthFill)
        {
            // Ensure delayed bar is at the starting position
            if (delayedDamageBar != null)
            {
                delayedDamageBar.fillAmount = currentDelayedFill;
            }
            // Start new delayed damage animation
            delayedDamageCoroutine = StartCoroutine(DelayedDamageCoroutine());
        }
        else
        {
            // If already at or below target, set immediately
            currentDelayedFill = targetHealthFill;
            if (delayedDamageBar != null)
            {
                delayedDamageBar.fillAmount = currentDelayedFill;
            }
        }
    }

    /// <summary>
    /// Coroutine to animate delayed damage bar
    /// </summary>
    private IEnumerator DelayedDamageCoroutine()
    {
        // Wait for delay
        yield return new WaitForSeconds(delayedDamageDelay);

        // Animate delayed bar following the main bar
        while (currentDelayedFill > targetHealthFill)
        {
            currentDelayedFill = Mathf.MoveTowards(
                currentDelayedFill,
                targetHealthFill,
                delayedDamageSpeed * Time.deltaTime
            );

            if (delayedDamageBar != null)
            {
                delayedDamageBar.fillAmount = currentDelayedFill;
            }

            yield return null;
        }

        // Ensure final value is set
        currentDelayedFill = targetHealthFill;
        if (delayedDamageBar != null)
        {
            delayedDamageBar.fillAmount = currentDelayedFill;
        }
    }

    /// <summary>
    /// Update delayed damage bar (called in Update for smooth animation)
    /// </summary>
    private void UpdateDelayedDamageBar()
    {
        // This is handled by coroutine, but kept for manual updates if needed
    }

    /// <summary>
    /// Manually set PlayerHealth reference (useful for respawn scenarios)
    /// </summary>
    public void SetPlayerHealth(PlayerHealth health)
    {
        // Unsubscribe from old health
        UnsubscribeFromHealthEvents();

        // Set new health
        playerHealth = health;

        // Subscribe to new health
        if (playerHealth != null)
        {
            SubscribeToHealthEvents();
        }
    }

    /// <summary>
    /// Refresh PlayerHealth reference (useful after respawn)
    /// </summary>
    public void RefreshPlayerHealth()
    {
        UnsubscribeFromHealthEvents();
        FindPlayerHealth();
    }

    /// <summary>
    /// Get current PlayerHealth reference
    /// </summary>
    public PlayerHealth GetPlayerHealth()
    {
        return playerHealth;
    }

    private void OnDestroy()
    {
        UnsubscribeFromHealthEvents();
    }

    private void OnDisable()
    {
        // Stop coroutine when disabled
        if (delayedDamageCoroutine != null)
        {
            StopCoroutine(delayedDamageCoroutine);
            delayedDamageCoroutine = null;
        }
    }
}

