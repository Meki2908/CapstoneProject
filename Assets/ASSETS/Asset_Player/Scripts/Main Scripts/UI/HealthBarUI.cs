using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Manages the player health bar UI with fill bar and delayed damage bar
/// Tự động tìm lại PlayerHealth sau scene transition
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
    
    // Track last known health to detect changes via polling
    private float lastKnownHealth = -1f;
    private float lastKnownMaxHealth = -1f;

    private void Awake()
    {
        // Subscribe vào scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        InitializeHealthBar();
    }

    /// <summary>
    /// Khởi tạo health bar — tìm PlayerHealth và subscribe events
    /// </summary>
    private void InitializeHealthBar()
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

    /// <summary>
    /// Callback khi scene mới được load — luôn tìm lại PlayerHealth
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[HealthBarUI] Scene loaded: {scene.name}. Re-finding PlayerHealth...");
        // Chờ vài frame để mọi thứ khởi tạo xong
        StartCoroutine(ReconnectAfterSceneLoad());
    }

    private IEnumerator ReconnectAfterSceneLoad()
    {
        // Chờ 3 frame để đảm bảo PlayerHealth.Start() đã chạy xong
        yield return null;
        yield return null;
        yield return null;
        
        // Tìm lại PlayerHealth
        UnsubscribeFromHealthEvents();
        playerHealth = null;
        FindPlayerHealth();
        
        if (playerHealth != null)
        {
            // Sync health bar ngay lập tức
            float healthPercent = playerHealth.CurrentHealth / playerHealth.MaxHealth;
            targetHealthFill = healthPercent;
            currentDelayedFill = healthPercent;
            lastKnownHealth = playerHealth.CurrentHealth;
            lastKnownMaxHealth = playerHealth.MaxHealth;
            
            if (healthFillBar != null)
            {
                healthFillBar.fillAmount = healthPercent;
                UpdateHealthBarColor(healthPercent);
            }
            if (delayedDamageBar != null)
            {
                delayedDamageBar.fillAmount = healthPercent;
            }
            
            Debug.Log($"[HealthBarUI] Reconnected to PlayerHealth after scene load. HP={playerHealth.CurrentHealth}/{playerHealth.MaxHealth}");
        }
        else
        {
            Debug.LogWarning("[HealthBarUI] Could not find PlayerHealth after scene load!");
        }
    }

    private void Update()
    {
        // Update delayed damage bar
        if (delayedDamageBar != null && playerHealth != null)
        {
            UpdateDelayedDamageBar();
        }

        // === FALLBACK: Polling mỗi frame ===
        // Nếu event không hoạt động (stale reference, race condition), 
        // polling sẽ đảm bảo health bar luôn đồng bộ
        if (playerHealth != null && playerHealth.IsAlive)
        {
            float currentHP = playerHealth.CurrentHealth;
            float maxHP = playerHealth.MaxHealth;
            
            // Chỉ update khi HP thay đổi (so sánh với giá trị cũ)
            if (!Mathf.Approximately(currentHP, lastKnownHealth) || !Mathf.Approximately(maxHP, lastKnownMaxHealth))
            {
                lastKnownHealth = currentHP;
                lastKnownMaxHealth = maxHP;
                UpdateHealthBar(currentHP, maxHP);
            }
        }
        
        // Auto-reconnect nếu playerHealth bị null
        if (playerHealth == null && autoFindPlayerHealth)
        {
            FindPlayerHealth();
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
                    Debug.Log($"[HealthBarUI] Found PlayerHealth on '{playerObject.name}' by tag. HP={playerHealth.CurrentHealth}/{playerHealth.MaxHealth}");
                    return;
                }
            }
        }

        // Try FindObjectOfType as fallback
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            SubscribeToHealthEvents();
            Debug.Log($"[HealthBarUI] Found PlayerHealth on '{playerHealth.gameObject.name}' by FindFirstObjectByType. HP={playerHealth.CurrentHealth}/{playerHealth.MaxHealth}");
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
            // Unsubscribe trước để tránh duplicate
            playerHealth.OnHealthChanged -= OnHealthChanged;
            playerHealth.OnPlayerDied -= OnPlayerDied;
            
            // Subscribe mới
            playerHealth.OnHealthChanged += OnHealthChanged;
            playerHealth.OnPlayerDied += OnPlayerDied;

            // Initialize with current health
            float currentHealthPercent = playerHealth.MaxHealth > 0 ? playerHealth.CurrentHealth / playerHealth.MaxHealth : 1f;
            targetHealthFill = currentHealthPercent;
            lastKnownHealth = playerHealth.CurrentHealth;
            lastKnownMaxHealth = playerHealth.MaxHealth;

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
    /// Called when player health changes (via event)
    /// </summary>
    private void OnHealthChanged(float currentHealth)
    {
        if (playerHealth != null)
        {
            lastKnownHealth = currentHealth;
            lastKnownMaxHealth = playerHealth.MaxHealth;
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
        float previousDelayedFill = currentDelayedFill;
        targetHealthFill = newTargetHealthFill;

        // Update main health fill bar immediately
        if (healthFillBar != null)
        {
            healthFillBar.fillAmount = targetHealthFill;
            UpdateHealthBarColor(targetHealthFill);
        }

        // Start delayed damage bar animation
        if (delayedDamageBar != null)
        {
            currentDelayedFill = previousDelayedFill;
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

        if (healthPercentage > lowHealthThreshold)
        {
            healthFillBar.color = healthyColor;
        }
        else
        {
            healthFillBar.color = lowHealthColor;
        }
    }

    /// <summary>
    /// Start the delayed damage bar animation
    /// </summary>
    private void StartDelayedDamageAnimation()
    {
        if (delayedDamageCoroutine != null)
        {
            StopCoroutine(delayedDamageCoroutine);
            delayedDamageCoroutine = null;
        }

        if (currentDelayedFill > targetHealthFill)
        {
            if (delayedDamageBar != null)
            {
                delayedDamageBar.fillAmount = currentDelayedFill;
            }
            delayedDamageCoroutine = StartCoroutine(DelayedDamageCoroutine());
        }
        else
        {
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
        yield return new WaitForSeconds(delayedDamageDelay);

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
        // Handled by coroutine
    }

    /// <summary>
    /// Manually set PlayerHealth reference (useful for respawn scenarios)
    /// </summary>
    public void SetPlayerHealth(PlayerHealth health)
    {
        UnsubscribeFromHealthEvents();
        playerHealth = health;
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
        playerHealth = null;
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
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (delayedDamageCoroutine != null)
        {
            StopCoroutine(delayedDamageCoroutine);
            delayedDamageCoroutine = null;
        }
    }
}
