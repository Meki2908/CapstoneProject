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

    private void Start()
    {
        Debug.Log($"[DungeonManiaPlayerBridge] Initialized on {gameObject.name}. PlayerHealth={(playerHealth != null ? "found" : "null")}");
    }

    /// <summary>
    /// Called by DungeonMania's enemy attack system
    /// </summary>
    public void PlayerDamage(Damage damageStruct, int hit)
    {
        Debug.Log($"[DungeonManiaPlayerBridge] PlayerDamage called! damage={damageStruct.damage}, elemental={damageStruct.damageElemental}, crit={damageStruct.crit}");

        if (playerHealth == null)
        {
            Debug.LogWarning("[DungeonManiaPlayerBridge] PlayerHealth is null!");
            return;
        }

        // Calculate total damage
        float totalDamage = damageStruct.damage + damageStruct.damageElemental + damageStruct.crit;
        
        // Ensure minimum damage
        if (totalDamage < 1) totalDamage = 1;

        Debug.Log($"[DungeonManiaPlayerBridge] Calling TakeDamage with {totalDamage} damage (forceHit=true)");

        // Apply damage to player - force hit even if dashing
        Vector3 hitPosition = Vector3.zero; // Could be improved to pass actual hit position
        playerHealth.TakeDamage(totalDamage, hitPosition, true); // forceHitAnimation = true

        // Trigger hit animation
        if (animator != null && hitTriggerHash != 0)
        {
            animator.SetTrigger(hitTriggerHash);
        }

        Debug.Log($"[DungeonManiaPlayerBridge] Player took {totalDamage} damage (physical: {damageStruct.damage}, elemental: {damageStruct.damageElemental}, crit: {damageStruct.crit})");
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
