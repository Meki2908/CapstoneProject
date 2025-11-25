using UnityEngine;

public enum AbilityInput { None, E, R, T, Q_Ultimate }

[System.Serializable]
public class SkillEvent
{
    public GameObject vfxPrefab;
    public VfxSpawnRule spawnRule;

    // Di chuyển sau khi spawn (projectile / chém bay)
    public bool moveAfterSpawn = false;
    public float moveSpeed = 12f;         // tốc độ di chuyển (m/s)
    public float moveLifetime = 0f;       // 0 = dùng vfxDuration từ AbilitySO/Skill script
    public bool alignToDirection = true;  // xoay VFX theo hướng bay khi launch
}

[CreateAssetMenu(fileName = "AbilitySO", menuName = "Scriptable Objects/AbilitySO")]
public class AbilitySO : ScriptableObject
{
    [Header("Identity")]
    public string abilityName = "Skill";
    [TextArea] public string description;
    public Sprite abilityIcon; // Icon for GUI display

    [Header("Binding")]
    public AbilityInput input = AbilityInput.E;
    public float cooldown = 6f;
    public float vfxDuration = 0.5f; // how long the VFX lasts (for pooling)

    [Header("VFX (spawned via Animation Events)")]
    public GameObject hitVfx;    // optional: spawn at hit frame event

    [Header("VFX Behavior")]
    [Tooltip("VFX follows player movement (like shield) vs world-space (like projectiles)")]
    public bool isFollowPlayer = false;

    [Header("Multi-hit Skill (optional, for weapons like Axe)")]
    [Tooltip("Define multiple damage/VFX windows for a single skill.")]
    public SkillEvent[] skillEvents; // if provided, AxeSkill can run data-driven windows

    /// <summary>
    /// Get modified cooldown based on equipped gems: CD = baseCD - (baseCD × %)
    /// </summary>
    public float GetModifiedCooldown(WeaponType weaponType)
    {
        if (WeaponGemManager.Instance == null)
        {
            return cooldown; // No gems, return base cooldown
        }

        // Get cooldown multiplier from gems (returns 1.0 - total %)
        float cdMultiplier = WeaponGemManager.Instance.GetCooldownMultiplier(weaponType);

        // Calculate: baseCD - (baseCD × %)
        // cdMultiplier = 1.0 - totalPercent, so we need to extract the percent part
        float cdPercent = 1f - cdMultiplier; // Extract the % part (e.g., 0.85 -> 0.15)

        // Calculate: baseCD - (baseCD × %)
        float modifiedCooldown = cooldown - (cooldown * cdPercent);

        // Ensure cooldown is not negative or too low
        return Mathf.Max(0.1f, modifiedCooldown);
    }
}