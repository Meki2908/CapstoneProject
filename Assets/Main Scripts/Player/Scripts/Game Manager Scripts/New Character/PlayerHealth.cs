using UnityEngine;
using TMPro;
using Fusion;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 5f;
    [Networked] public float currentHealth { get; set; }

    private ChangeDetector _changeDetector;

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

    // Hệ thống chống stunlock — thời gian bất tử sau khi bị đánh
    [Header("Hit Cooldown (chống stunlock)")]
    [Tooltip("Thời gian bất tử sau khi bị đánh (giây) — player vẫn nhận damage nhưng không bị dừng hành động")]
    [SerializeField] private float hitCooldown = 0.8f;
    private float lastHitTime = -10f; // Thời điểm bị đánh lần cuối

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0f;
    // Temporary invulnerability (for ultimates)
    bool isInvulnerable = false;

    public bool IsInvulnerable() => isInvulnerable;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        baseMaxHealth = maxHealth;
        UpdateMaxHealthWithEquipment();
        
        if (HasStateAuthority)
        {
            currentHealth = maxHealth;
        }
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

        // Auto-find health text logic removed in favor of GameUIManager MVC architecture.
        UpdateHealthText();
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(currentHealth):
                    OnHPChanged();
                    break;
            }
        }
    }

    private void OnHPChanged()
    {
        OnHealthChanged?.Invoke(currentHealth);
        UpdateHealthText();
    }

    /// <summary>
    /// Auto-find health text from HealthBarUI or UI hierarchy
    /// </summary>
    private void FindHealthText()
    {
        // Method 1: Try to find HealthBarUI and explicitly get the text component meant for HP
        HealthBarUI healthBarUI = FindFirstObjectByType<HealthBarUI>();
        if (healthBarUI != null)
        {
            TextMeshProUGUI[] texts = healthBarUI.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                // Skip texts that belong to the consumable/potion UI
                if (txt.GetComponentInParent<ConsumableItemDisplay>() != null) continue;

                string tName = txt.name.ToLower();
                if (tName.Contains("health") || tName.Contains("hp") || tName.Contains("value") || tName.Contains("text"))
                {
                    healthText = txt;
                    return;
                }
            }
        }

        // Method 2: Fallback to finding by common names in Canvas, excluding Potion UI
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            TextMeshProUGUI[] texts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI txt in texts)
            {
                if (txt.GetComponentInParent<ConsumableItemDisplay>() != null) continue;

                string tName = txt.name.ToLower();
                if (tName == "health text" || tName == "hp text" || tName == "healthvalue" || tName == "hpvalue")
                {
                    healthText = txt;
                    return;
                }
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
        // Delegate to the main TakeDamage method with default values
        TakeDamage(damage, hitPosition, false);
    }

    /// <summary>
    /// Main TakeDamage method with extended parameters
    /// </summary>
    public void TakeDamage(float damage, Vector3 hitPosition, bool forceHitAnimation)
    {
        if (!HasStateAuthority)
        {
            return;
        }

        if (!IsAlive)
        {
            return;
        }

        // === SHIELD CHECK (+ anti-stuck) ===
        if (ShieldActivate.IsShieldActive && !forceHitAnimation)
        {
            // Safety: nếu không còn ShieldActivate nào trong scene → reset flag
            if (FindFirstObjectByType<ShieldActivate>() == null)
            {
                Debug.LogWarning("[PlayerHealth] Shield flag stuck! No ShieldActivate in scene → resetting");
                ShieldActivate.ForceReset();
            }
            else
            {
                return;
            }
        }

        // === INVULNERABLE CHECK (+ anti-stuck safety) ===
        if (isInvulnerable && !forceHitAnimation)
        {
            return;
        }

        // === DIE STATE CHECK ===
        if (character != null && character.movementSM != null && character.movementSM.currentState == character.dieState)
        {
            return;
        }

        // === DASH INVINCIBILITY CHECK ===
        if (character != null && character.IsDashing && !forceHitAnimation)
        {
            return;
        }

        // === DEFENSE REDUCTION (cap ở 80%) ===
        // Player LUÔN nhận ít nhất 20% damage gốc, bất kể defense cao bao nhiêu
        float finalDamage = damage;
        if (EquipmentManager.Instance != null)
        {
            float defense = EquipmentManager.Instance.GetTotalDefenseBonus();
            float maxReduction = damage * 0.8f; // Defense chặn TỐI ĐA 80% damage
            float reduced = Mathf.Min(defense, maxReduction);
            finalDamage = damage - reduced;
        }

        // Đảm bảo minimum damage = 1 (không bao giờ = 0 nếu có damage đầu vào)
        if (damage > 0f)
        {
            finalDamage = Mathf.Max(1f, finalDamage);
        }

        float hpBefore = currentHealth;
        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        OnHealthChanged?.Invoke(currentHealth);
        UpdateHealthText();

        // Check if player died BEFORE triggering get hit animation
        if (currentHealth <= 0f)
        {
            Die();
            return; // Exit early - don't trigger get hit if dead
        }

        // === CHỐNG STUNLOCK ===
        // Nếu trong thời gian cooldown → vẫn nhận damage nhưng KHÔNG vào GetHitState
        // Player có thể tiếp tục đánh/di chuyển bình thường
        if (Time.time - lastHitTime < hitCooldown)
        {
            return; // Đã trừ máu ở trên, nhưng không dừng hành động player
        }

        // === KHÔNG NGẮT COMBO ===
        // Nếu player đang tấn công (AttackState) → chỉ trừ máu, KHÔNG chuyển GetHitState
        // Player "tank through" damage để giữ combo mượt
        if (character != null && character.movementSM != null &&
            character.movementSM.currentState == character.attacking)
        {
            lastHitTime = Time.time; // Vẫn ghi nhận hit để cooldown hoạt động
            return;
        }
        
        lastHitTime = Time.time; // Ghi nhận thời điểm bị đánh
        
        // Store current state before transitioning to hit state
        if (character != null)
        {
            character.lastStateBeforeHit = character.movementSM.currentState;
        }

        // Chuyển sang GetHitState (chỉ khi KHÔNG đang attack)
        if (character != null && character.movementSM != null && character.movementSM.currentState != character.dieState)
        {
            character.movementSM.ChangeState(character.getHit);
        }
        else
        {
            Debug.LogWarning($"[PlayerHealth] Could not change to GetHitState! character={(character != null ? "exists" : "null")}, movementSM={(character?.movementSM != null ? "exists" : "null")}, currentState={character?.movementSM?.currentState?.GetType().Name}");
        }
    }

    /// <summary>
    /// PlayerDamage - called by DungeonMania's EnemyAttack script
    /// Converts DungeonMania's Damage struct to player's damage system
    /// </summary>
    public void PlayerDamage(Damage damageStruct, int hit)
    {
        // Calculate total damage from DungeonMania's damage struct
        int totalDamage = damageStruct.damage + damageStruct.damageElemental + damageStruct.crit;
        
        // Call main TakeDamage — forceHitAnimation = false để tôn trọng dash invincibility
        // Nếu player đang dash, sẽ không bị damage
        TakeDamage(totalDamage, Vector3.zero, false);
    }

    private void Die()
    {

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
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
        UpdateHealthText();
    }

    /// <summary>
    /// Update health text via centralized GameUIManager
    /// </summary>
    private void UpdateHealthText()
    {
        if (HasInputAuthority && GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateHP(currentHealth, maxHealth);
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
        yield return new WaitForSeconds(duration);
        isInvulnerable = false;
    }

    // Set invulnerability state directly (used for skill lock duration)
    public void SetInvulnerable(bool value)
    {
        // Stop any timed invulnerability when explicitly setting
        try { StopCoroutine("InvulnerabilityCoroutine"); } catch { }
        isInvulnerable = value;
    }
}



