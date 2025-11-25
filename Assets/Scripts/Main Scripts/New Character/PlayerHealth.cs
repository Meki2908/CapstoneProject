using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private float currentHealth;

    [Header("Components")]
    private Character character;
    private Animator animator;

    [Header("Events")]
    public System.Action<float> OnHealthChanged;
    public System.Action OnPlayerDied;

    private float baseMaxHealth; // Store base health for equipment bonus calculation

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0f;

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

        Debug.Log($"[PlayerHealth] Player initialized with {maxHealth} HP");
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

        Debug.Log($"[PlayerHealth] Player took {finalDamage} damage (original: {damage}, defense: {(EquipmentManager.Instance != null ? EquipmentManager.Instance.GetTotalDefenseBonus() : 0f)})! Current HP: {currentHealth}/{maxHealth}");

        // Check if player died BEFORE triggering get hit animation
        if (currentHealth <= 0f)
        {
            Die();
            return; // Exit early - don't trigger get hit if dead
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
        Debug.Log($"[PlayerHealth] Player healed for {amount}! Current HP: {currentHealth}/{maxHealth}");
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
        Debug.Log($"[PlayerHealth] Player health reset to {maxHealth}");
    }
}

