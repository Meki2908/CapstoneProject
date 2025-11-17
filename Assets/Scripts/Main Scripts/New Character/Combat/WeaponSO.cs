using UnityEngine;

public enum WeaponType { None = 0, Sword = 1, Axe = 2, Mage = 3 }

[System.Serializable]
public struct VfxSpawnRule
{
    [Tooltip("Local offset relative to anchor (hand or character).")]
    public Vector3 localOffset;
    [Tooltip("Yaw in degrees around world Y to rotate the slash direction.")]
    public float yawOffset;
    [Tooltip("Pitch in degrees around local right.")]
    public float pitchOffset;
    [Tooltip("Roll in degrees around forward (useful for some slashes).")]
    public float rollOffset;
    [Tooltip("Uniform scale multiplier for spawned VFX.")]
    public float scale;
    [Tooltip("Extra Euler offset applied after base rotation (final fine-tune).")]
    public Vector3 extraEulerOffset;
}

[System.Serializable]
public struct HitTiming
{
    [Tooltip("Seconds from hit start when damage becomes active.")]
    public float windowStart;
    [Tooltip("Seconds from hit start to play VFX.")]
    public float vfxTime;
    [Tooltip("Seconds from hit start when damage stops.")]
    public float windowEnd;

    [Header("Dynamic VFX placement")]
    public VfxSpawnRule spawnRule;
}

[System.Serializable]
public struct SocketOffset
{
    public Vector3 localPosition; // vị trí tương đối so với holder
    public Vector3 localEuler;    // xoay tương đối (degrees)
    public Vector3 localScale;    // scale tương đối (mặc định 1,1,1)
}

public class WeaponSO : ScriptableObject
{
    public enum VfxSpawnMode { Script = 0, AnimationEvent = 1 }
    [Header("Weapon Identity")]
    public WeaponType weaponType = WeaponType.Sword;
    public string weaponName = "Sword";

    [Header("Model/Prefab")]
    public GameObject weaponPrefab;

    [Header("Abilities Table")]
    public AbilitySO[] abilities;

    [Header("Normal Attack Chain")]
    [Tooltip("Per-hit timing windows in seconds (size = number of hits in the chain).")]
    public HitTiming[] hitTimings;

    [Tooltip("Per-hit VFX (size should match hitTimings). Leave null to skip VFX for that hit.")]
    public GameObject[] normalHitVfx;

    [Header("VFX Spawn Mode")]
    [Tooltip("How to spawn normal-attack VFX: by script timing or via Animation Events.")]
    public VfxSpawnMode normalVfxSpawnMode = VfxSpawnMode.Script;

    [Header("Sockets/Offsets")]
    public SocketOffset handSocket = new SocketOffset { localScale = Vector3.one };
    public SocketOffset sheathSocket = new SocketOffset { localScale = Vector3.one };

    [Header("Mage-Specific Settings")]
    [Tooltip("For Mage: weapon summon position (where weapon flies from)")]
    public Vector3 summonPosition = new Vector3(0, 2, -5);
    [Tooltip("For Mage: weapon summon speed")]
    public float summonSpeed = 10f;
    [Tooltip("For Mage: weapon sheath speed (thrown away)")]
    public float sheathSpeed = 8f;

    [Header("Mastery System")]
    [Tooltip("Animation Curve for EXP requirement per level. X-axis = level (1-100), Y-axis = EXP required for that level.")]
    public AnimationCurve expRequirementCurve = AnimationCurve.Linear(1f, 100f, 100f, 10000f);

    /// <summary>
    /// Get the EXP required to reach the specified level.
    /// </summary>
    public float GetExpRequiredForLevel(int level)
    {
        if (expRequirementCurve == null || expRequirementCurve.length == 0)
        {
            // Default linear curve if not set
            return 100f * level;
        }
        return expRequirementCurve.Evaluate(Mathf.Clamp(level, 1, 100));
    }

    /// <summary>
    /// Get the EXP required to level up from current level to next level.
    /// </summary>
    public float GetExpRequiredForNextLevel(int currentLevel)
    {
        return GetExpRequiredForLevel(currentLevel + 1);
    }
}