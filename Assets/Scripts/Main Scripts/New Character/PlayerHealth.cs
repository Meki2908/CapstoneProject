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

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0f;

    void Start()
    {
        currentHealth = maxHealth;
        character = GetComponent<Character>();
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogWarning("[PlayerHealth] Animator not found!");
        }

        Debug.Log($"[PlayerHealth] Player initialized with {maxHealth} HP");
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

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log($"[PlayerHealth] Player took {damage} damage! Current HP: {currentHealth}/{maxHealth}");

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

