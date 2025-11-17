using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.Playables;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class SwordSkills : MonoBehaviour
{
    [Header("Refs")]
    public EquipmentSystem equipment;
    public Transform defaultVfxSpawn;
    public PlayableDirector ultimateDirector;

    [Header("Animator Parameters")]
    public string skillTriggerParam = "swordSkill";
    public string skillIndexParam = "skillIndex";
    public float vfxDuration = 0.5f;
    public float maxSkillLockSeconds = 3f;
    [SerializeField] private int swordLayerIndex = 1;      // NEW: layer index cho Sword
    [SerializeField] private string skillStateTag = "SwordSkill"; // NEW: tag state skill của Sword

    private float skillLockExpireAt = 0f;
    private Animator animator;
    private Character character;
    private SkillLock skillLock;

    private readonly Dictionary<AbilityInput, AbilitySO> abilityMap = new();

    // Debounce
    private readonly Dictionary<int, float> lastVfxSpawnTime = new();
    [SerializeField] private float vfxMinInterval = 0.05f;

    [Header("Spawn Rule")]
    [SerializeField] private bool useInputDirection = false;
    [SerializeField] private Transform forwardAnchor;

    public void SetForwardAnchor(Transform t) => forwardAnchor = t;
    public void SetDefaultVfxSpawn(Transform t) => defaultVfxSpawn = t;

    // Animation Event: Trigger cooldown for specific ability
    public void AE_TriggerCooldown(int inputIndex)
    {
        var abilityIconManager = FindObjectOfType<AbilityIconManager>();
        if (abilityIconManager != null)
        {
            abilityIconManager.AE_TriggerCooldown(inputIndex);
        }
    }

    // Specific AE methods for each ability (easier to use in animations)
    public void AE_TriggerECooldown() => AE_TriggerCooldown((int)AbilityInput.E);
    public void AE_TriggerRCooldown() => AE_TriggerCooldown((int)AbilityInput.R);
    public void AE_TriggerTCooldown() => AE_TriggerCooldown((int)AbilityInput.T);
    public void AE_TriggerUltimateCooldown() => AE_TriggerCooldown((int)AbilityInput.Q_Ultimate);

    private void Awake()
    {
        animator = GetComponent<Animator>();
        character = GetComponent<Character>();
        skillLock = GetComponent<SkillLock>();
        if (equipment == null) equipment = GetComponent<EquipmentSystem>();
    }

    private void OnEnable()
    {
        RebuildAbilityMap();
        var wc = GetComponent<WeaponController>();
        if (wc != null)
        {
            wc.OnWeaponChanged -= OnWeaponChangedHandler;
            wc.OnWeaponChanged += OnWeaponChangedHandler;
        }

        // Don't auto-refresh here, let Animation Events control it
        Debug.Log("[SwordSkills] OnEnable - Script enabled");
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        var wc = GetComponent<WeaponController>();
        if (wc != null)
        {
            wc.OnWeaponChanged -= OnWeaponChangedHandler;
        }
        Debug.Log("[SwordSkills] OnDisable - Script disabled");
    }
    private void OnWeaponChangedHandler(WeaponSO so)
    {
        RebuildAbilityMap();
        // Don't auto-refresh here, let Animation Events control it
    }



    public void RebuildAbilityMap()
    {
        abilityMap.Clear();
        var weapon = equipment != null ? equipment.GetCurrentWeapon() : null;
        if (weapon == null || weapon.abilities == null) return;

        foreach (var ab in weapon.abilities)
        {
            if (ab == null || ab.input == AbilityInput.None) continue;
            abilityMap[ab.input] = ab;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame) TryUse(AbilityInput.E);
        if (Keyboard.current.rKey.wasPressedThisFrame) TryUse(AbilityInput.R);
        if (Keyboard.current.tKey.wasPressedThisFrame) TryUse(AbilityInput.T);
        if (Keyboard.current.qKey.wasPressedThisFrame) TryUse(AbilityInput.Q_Ultimate);
    }

    public void TryUse(AbilityInput input)
    {
        var weapon = equipment != null ? equipment.GetCurrentWeapon() : null;
        bool inCombat = character != null && character.movementSM != null
                                          && character.movementSM.currentState == character.combatMove;
        bool drawn = character != null && character.isWeaponDrawn;
        if (weapon == null || weapon.weaponType != WeaponType.Sword || !inCombat || !drawn)
            return;
        if (!abilityMap.TryGetValue(input, out var ability)) return;

        // Check if skill is unlocked
        if (WeaponMasteryManager.Instance != null)
        {
            if (!WeaponMasteryManager.Instance.IsSkillUnlocked(WeaponType.Sword, input))
            {
                Debug.Log($"[SwordSkills] {input} is locked! Mastery level required.");
                return;
            }
        }

        // Check cooldown
        var abilityIconManager = FindObjectOfType<AbilityIconManager>();
        if (abilityIconManager != null && abilityIconManager.IsOnCooldown(input))
        {
            Debug.Log($"[SwordSkills] {input} is on cooldown!");
            return;
        }

        int idx = input switch { AbilityInput.E => 0, AbilityInput.R => 1, AbilityInput.T => 2, AbilityInput.Q_Ultimate => 3, _ => 0 };
        animator.SetInteger(skillIndexParam, idx);
        animator.SetTrigger(skillTriggerParam);

        // Cooldown will be triggered by Animation Event in the skill animation

        if (skillLock != null) skillLock.BeginSkillRootMotion(animator);
        skillLockExpireAt = Time.time + maxSkillLockSeconds;

        if (input == AbilityInput.Q_Ultimate && ultimateDirector != null)
        {
            ultimateDirector.time = 0;
            ultimateDirector.Play();
        }
    }

    // AE-only spawn
    public void AE_PlaySkillVFXByEvent(int eventIndex)
    {
        // Guard: chỉ xử lý nếu đang cầm Sword
        var weapon = equipment != null ? equipment.GetCurrentWeapon() : null;
        if (weapon == null || weapon.weaponType != WeaponType.Sword) return;

        // Guard-2: đúng animator state/tag/layer của Sword
        if (!IsInSkillState()) return;

        if (!animator) return;

        int skillIdx = animator.GetInteger(skillIndexParam);
        var input = skillIdx switch { 0 => AbilityInput.E, 1 => AbilityInput.R, 2 => AbilityInput.T, 3 => AbilityInput.Q_Ultimate, _ => AbilityInput.E };
        if (!abilityMap.TryGetValue(input, out var ability) || ability == null) return;

        var events = ability.skillEvents;
        if (events == null || eventIndex < 0 || eventIndex >= events.Length) return;

        float lastTime = lastVfxSpawnTime.TryGetValue(eventIndex, out var t) ? t : -999f;
        if (Time.time - lastTime < vfxMinInterval) return;
        lastVfxSpawnTime[eventIndex] = Time.time;

        var ev = events[eventIndex];
        var prefab = ev.vfxPrefab != null ? ev.vfxPrefab : ability.hitVfx;
        if (prefab == null) return;

        var (pos, rot, scl) = BuildSpawnTransform(ev.spawnRule);
        var v = Instantiate(prefab, pos, rot); // world-space
        // ProjectileDamage no longer needs Initialize - effects handled by separate scripts
        if (ev.spawnRule.extraEulerOffset != Vector3.zero) v.transform.rotation *= Quaternion.Euler(ev.spawnRule.extraEulerOffset);
        v.transform.localScale = Vector3.Scale(v.transform.localScale, scl);

        if (ev.moveAfterSpawn)
        {
            var mover = v.GetComponent<VfxMover>() ?? v.AddComponent<VfxMover>();
            float life = ev.moveLifetime > 0f ? ev.moveLifetime : (ability.vfxDuration > 0f ? ability.vfxDuration : vfxDuration);
            mover.Launch(rot * Vector3.forward, ev.moveSpeed, life, ev.alignToDirection);
        }

        Destroy(v, ability.vfxDuration > 0 ? ability.vfxDuration : vfxDuration);
        // Effects now handled by separate effect scripts attached to VFX prefabs
    }

    // DelayedStartEffect method removed - effects now handled by separate scripts

    // Damage AEs
    public void AE_StartDamage() => equipment?.StartDealDamage();
    public void AE_EndDamage() => equipment?.EndDealDamage();

    // NEW: Check state theo layer/tag để AE chỉ chạy đúng lúc
    private bool IsInSkillState()
    {
        if (!animator) return false;
        var st = animator.GetCurrentAnimatorStateInfo(swordLayerIndex);
        return st.IsTag(skillStateTag);
    }

    private (Vector3 pos, Quaternion rot, Vector3 scl) BuildSpawnTransform(VfxSpawnRule rule)
    {
        Vector3 baseForward = GetBaseForward();
        if (baseForward.sqrMagnitude < 0.0001f) baseForward = Vector3.forward;

        Quaternion yawRot = Quaternion.AngleAxis(rule.yawOffset, Vector3.up);
        Vector3 right = Vector3.Cross(Vector3.up, baseForward).normalized;
        Quaternion pitchRot = Quaternion.AngleAxis(rule.pitchOffset, right);
        Quaternion rollRot = Quaternion.AngleAxis(rule.rollOffset, baseForward);
        Quaternion finalRot = Quaternion.LookRotation(baseForward) * yawRot * pitchRot * rollRot;

        Transform anchor = defaultVfxSpawn != null ? defaultVfxSpawn : transform;
        Vector3 worldOffset = finalRot * rule.localOffset;
        Vector3 pos = anchor.position + worldOffset;
        Vector3 scl = Vector3.one * (rule.scale <= 0f ? 1f : rule.scale);
        return (pos, finalRot, scl);
    }

    private Vector3 GetBaseForward()
    {
        if (forwardAnchor != null) { Vector3 f = forwardAnchor.forward; f.y = 0f; return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward; }
        if (!useInputDirection) { Vector3 f = (character != null ? character.transform.forward : transform.forward); f.y = 0f; return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward; }
        var cam = Camera.main ? Camera.main.transform : null; Vector3 fwd = cam ? cam.forward : (character ? character.transform.forward : transform.forward); fwd.y = 0f; return fwd.normalized;
    }
}