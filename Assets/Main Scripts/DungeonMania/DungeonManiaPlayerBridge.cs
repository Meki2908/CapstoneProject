using UnityEngine;

/// <summary>
/// Bridge component to connect DungeonMania's enemy attack system with the user's PlayerHealth system.
/// Add this component to your player GameObject.
/// </summary>
public class DungeonManiaPlayerBridge : MonoBehaviour
{
    [Header("Player Health Reference")]
    [Tooltip("Reference to the PlayerHealth component on this player")]
    public PlayerHealth playerHealth;

    [Header("Animation")]
    [Tooltip("Name of the trigger parameter for hit animation")]
    public string hitTriggerName = "Hit";

    private Animator animator;
    private int hitTriggerHash;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        
        // Auto-find PlayerHealth if not assigned
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogWarning("[DungeonManiaPlayerBridge] PlayerHealth not found! Please assign it in Inspector.");
        }

        // Cache the trigger hash
        if (!string.IsNullOrEmpty(hitTriggerName))
        {
            hitTriggerHash = Animator.StringToHash(hitTriggerName);
        }
    }

    /// <summary>
    /// Re-find PlayerHealth mỗi khi object được bật (scene transition, pooling)
    /// </summary>
    private void OnEnable()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = GetComponentInChildren<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = GetComponentInParent<PlayerHealth>();
                
            if (playerHealth != null)
                Debug.Log($"[DungeonManiaPlayerBridge] OnEnable: Re-found PlayerHealth on {playerHealth.gameObject.name}");
            else
                Debug.LogError("[DungeonManiaPlayerBridge] OnEnable: PlayerHealth STILL NULL! Player sẽ không nhận damage!");
        }
    }

    private void Start()
    {
        Debug.Log($"[DungeonManiaPlayerBridge] Initialized on {gameObject.name}. PlayerHealth={(playerHealth != null ? $"found (HP={playerHealth.CurrentHealth}/{playerHealth.MaxHealth})" : "NULL")}");
    }

    /// <summary>
    /// Called by DungeonMania's enemy attack system
    /// </summary>
    public void PlayerDamage(Damage damageStruct, int hit)
    {
        Debug.Log($"[DungeonManiaPlayerBridge] PlayerDamage called! damage={damageStruct.damage}, elemental={damageStruct.damageElemental}, crit={damageStruct.crit}");

        // Tự tìm lại PlayerHealth nếu bị null (stale reference sau scene transition)
        if (playerHealth == null)
        {
            Debug.LogWarning("[DungeonManiaPlayerBridge] PlayerHealth is null! Trying to re-find...");
            playerHealth = GetComponent<PlayerHealth>();
            if (playerHealth == null) playerHealth = GetComponentInChildren<PlayerHealth>();
            if (playerHealth == null) playerHealth = GetComponentInParent<PlayerHealth>();
            
            if (playerHealth == null)
            {
                Debug.LogError("[DungeonManiaPlayerBridge] PlayerHealth is STILL NULL after re-search! Cannot apply damage.");
                return;
            }
            Debug.Log($"[DungeonManiaPlayerBridge] Re-found PlayerHealth on {playerHealth.gameObject.name}");
        }

        // Calculate total damage
        float totalDamage = damageStruct.damage + damageStruct.damageElemental + damageStruct.crit;
        
        // Ensure minimum damage
        if (totalDamage < 1) totalDamage = 1;

        float hpBefore = playerHealth.CurrentHealth;

        // Apply damage to player
        Vector3 hitPosition = Vector3.zero;
        playerHealth.TakeDamage(totalDamage, hitPosition, false);
        
        float hpAfter = playerHealth.CurrentHealth;
        float actualDamage = hpBefore - hpAfter;
        Debug.Log($"[DungeonManiaPlayerBridge] Damage applied: {actualDamage} (raw:{totalDamage}, phys:{damageStruct.damage} elem:{damageStruct.damageElemental} crit:{damageStruct.crit}) | HP: {hpBefore} → {hpAfter}");
    }

    /// <summary>
    /// Check if player is alive
    /// </summary>
    public bool IsAlive()
    {
        return playerHealth != null && playerHealth.IsAlive;
    }
}

// Note: Damage struct is defined in DungeonManiaStubs.cs
