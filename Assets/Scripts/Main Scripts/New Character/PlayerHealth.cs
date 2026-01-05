using UnityEngine;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private float currentHealth;

    [Header("Components")]
    private Character character;
    private Animator animator;

    [Header("UI")]
    [Tooltip("Text to display current/max HP below health bar (auto-found if not assigned)")]
    [SerializeField] private TextMeshProUGUI healthText;
    [Tooltip("Auto-find health text from HealthBarUI or UI hierarchy")]
    [SerializeField] private bool autoFindHealthText = true;

    [Header("Events")]
    public System.Action<float> OnHealthChanged;
    public System.Action OnPlayerDied;

    private float baseMaxHealth; // Store base health for equipment bonus calculation

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0f;
    // Temporary invulnerability (for ultimates)
    bool isInvulnerable = false;

    public bool IsInvulnerable() => isInvulnerable;

    void Start()
    {
        baseMaxHealth = maxHealth;
        UpdateMaxHealthWithEquipment();
        currentHealth = maxHealth;
        character = GetComponent<Character>();
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogWarning("[PlayerHealth] Animator not found!");
        }

        // Subscribe to equipment changes
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged += OnEquipmentChanged;
        }

        // Auto-find health text if not assigned
        if (autoFindHealthText && healthText == null)
        {
            FindHealthText();
        }

        // Update health text
        UpdateHealthText();

        Debug.Log($"[PlayerHealth] Player initialized with {maxHealth} HP");
    }

    /// <summary>
    /// Auto-find health text from HealthBarUI or UI hierarchy
    /// </summary>
    private void FindHealthText()
    {
        // Method 1: Try to find HealthBarUI and get TextMeshProUGUI from its children
        HealthBarUI healthBarUI = UnityEngine.Object.FindFirstObjectByType<HealthBarUI>();
        if (healthBarUI != null)
        {
            // Look for TextMeshProUGUI in HealthBarUI's children
            healthText = healthBarUI.GetComponentInChildren<TextMeshProUGUI>();
            if (healthText != null)
            {
                Debug.Log("[PlayerHealth] Found health text from HealthBarUI");
                return;
            }
        }

        // Method 2: Try to find by common names/tags in Canvas
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            // Look for TextMeshProUGUI with common health text names
            TextMeshProUGUI[] texts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in texts)
            {
                string textName = text.name.ToLower();
                if (textName.Contains("health") || textName.Contains("hp") || textName.Contains("healthtext"))
                {
                    healthText = text;
                    Debug.Log($"[PlayerHealth] Found health text by name: {text.name}");
                    return;
                }
            }
        }

        // Method 3: Try to find in all TextMeshProUGUI components (last resort)
        TextMeshProUGUI[] allTexts = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
        foreach (TextMeshProUGUI text in allTexts)
        {
            // Check if it's likely a health text (has "HP" or "Health" in name, or is child of health bar)
            string textName = text.name.ToLower();
            Transform parent = text.transform.parent;
            bool isInHealthBar = false;
            while (parent != null)
            {
                if (parent.name.ToLower().Contains("health") || parent.name.ToLower().Contains("bar"))
                {
                    isInHealthBar = true;
                    break;
                }
                parent = parent.parent;
            }

            if (isInHealthBar || textName.Contains("health") || textName.Contains("hp"))
            {
                healthText = text;
                Debug.Log($"[PlayerHealth] Found health text: {text.name}");
                return;
            }
        }

        Debug.LogWarning("[PlayerHealth] Could not auto-find health text! Please assign it manually in the inspector.");
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
        float oldMaxHealth = maxHealth;
        UpdateMaxHealthWithEquipment();

        // Adjust current health proportionally
        if (oldMaxHealth > 0f)
        {
            float healthRatio = currentHealth / oldMaxHealth;
            currentHealth = maxHealth * healthRatio;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }
        else
        {
            currentHealth = maxHealth;
        }

        OnHealthChanged?.Invoke(currentHealth);
        UpdateHealthText();
    }

    /// <summary>
    /// Update max health based on equipped items: maxHealth = baseMaxHealth + totalHPBonus
    /// </summary>
    private void UpdateMaxHealthWithEquipment()
    {
        float hpBonus = 0f;
        if (EquipmentManager.Instance != null)
        {
            hpBonus = EquipmentManager.Instance.GetTotalHPBonus();
        }

        maxHealth = baseMaxHealth + hpBonus;
    }

    public void TakeDamage(float damage, Vector3 hitPosition = default)
    {
        if (!IsAlive) return; // Already dead, ignore damage

        // If invulnerable (e.g., ultimate), ignore damage
        if (isInvulnerable)
        {
            Debug.Log("[PlayerHealth] Player is invulnerable - damage ignored");
            return;
        }

        // Don't allow damage if already in DieState
        if (character != null && character.movementSM != null && character.movementSM.currentState == character.dieState)
        {
            return;
        }

        // Invincibility frame during dash - no damage received
        if (character != null && character.IsDashing)
        {
            Debug.Log($"[PlayerHealth] Dash invincibility frame active - damage ignored!");
            return;
        }

        // Apply defense reduction from equipment
        float finalDamage = damage;
        if (EquipmentManager.Instance != null)
        {
            float defense = EquipmentManager.Instance.GetTotalDefenseBonus();
            finalDamage = Mathf.Max(0f, damage - defense); // Defense reduces damage (flat reduction)
        }

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        OnHealthChanged?.Invoke(currentHealth);
        UpdateHealthText();

        Debug.Log($"[PlayerHealth] Player took {finalDamage} damage (original: {damage}, defense: {(EquipmentManager.Instance != null ? EquipmentManager.Instance.GetTotalDefenseBonus() : 0f)})! Current HP: {currentHealth}/{maxHealth}");

        // Check if player died BEFORE triggering get hit animation
        if (currentHealth <= 0f)
        {
            Die();
            return; // Exit early - don't trigger get hit if dead
        }

        // Store current state before transitioning to hit state
        if (character != null)
        {
            character.lastStateBeforeHit = character.movementSM.currentState;
        }

        // Don't trigger animation here - let GetHitState handle it based on currentLocomotionState
        // Transition to GetHitState if not already in DieState
        if (character != null && character.movementSM != null && character.movementSM.currentState != character.dieState)
        {
            character.movementSM.ChangeState(character.getHit);
        }
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] Player died!");

        // Notify listeners
        OnPlayerDied?.Invoke();

        // Change to DieState - DieState will handle animation based on currentLocomotionState
        if (character != null && character.dieState != null)
        {
            character.movementSM.ChangeState(character.dieState);
        }
    }

    public void Heal(float amount)
    {
        if (!IsAlive) return; // Can't heal if dead

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        OnHealthChanged?.Invoke(currentHealth);
        UpdateHealthText();
        Debug.Log($"[PlayerHealth] Player healed for {amount}! Current HP: {currentHealth}/{maxHealth}");
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
        UpdateHealthText();
        Debug.Log($"[PlayerHealth] Player health reset to {maxHealth}");
    }

    /// <summary>
    /// Update health text to display "Current/Max HP"
    /// </summary>
    private void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }
    }

    // Public API: Begin temporary invulnerability for duration seconds
    public void BeginInvulnerability(float duration)
    {
        if (duration <= 0f) return;
        if (isInvulnerable)
        {
            // extend timer by restarting coroutine
            StopCoroutine("InvulnerabilityCoroutine");
        }
        StartCoroutine(InvulnerabilityCoroutine(duration));
    }

    System.Collections.IEnumerator InvulnerabilityCoroutine(float duration)
    {
        isInvulnerable = true;
        Debug.Log($"[PlayerHealth] Invulnerability started for {duration} seconds");
        yield return new WaitForSeconds(duration);
        isInvulnerable = false;
        Debug.Log("[PlayerHealth] Invulnerability ended");
    }

    // Set invulnerability state directly (used for skill lock duration)
    public void SetInvulnerable(bool value)
    {
        // Stop any timed invulnerability when explicitly setting
        try { StopCoroutine("InvulnerabilityCoroutine"); } catch { }
        isInvulnerable = value;
        Debug.Log($"[PlayerHealth] SetInvulnerable -> {value}");
    }
}

