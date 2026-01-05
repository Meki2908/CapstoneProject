using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class AxeSkill : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EquipmentSystem equipment;     // chỉ dùng nếu KHÔNG dùng VFX-collision cho damage
    [SerializeField] private Transform defaultVfxSpawn;     // vị trí spawn VFX mặc định (ví dụ tip weapon)
    [SerializeField] private Animator animator;
    [SerializeField] private PlayableDirector ultimateDirector;  // Timeline for Q (optional)
    [Header("Animator Parameters")]
    [SerializeField] private float vfxDuration = 0.5f;
    [SerializeField] private string skillTriggerParam = "axeSkill";
    [SerializeField] private string skillIndexParam = "skillIndex"; // 0=E,1=R,2=T,3=Q
    [SerializeField] private int axeLayerIndex = 2;                // layer Axe trong Animator
    [SerializeField] private string skillStateTag = "AxeSkill";    // tag cho các state skill của Axe

    [Header("Behavior")]
    [SerializeField] private bool useInputDirection = false;
    [SerializeField] private Transform forwardAnchor;

    private Character character;
    private SkillLock skillLock;

    private readonly Dictionary<AbilityInput, AbilitySO> abilityMap = new();
    // Debounce VFX
    private readonly Dictionary<int, float> lastVfxSpawnTime = new();
    private readonly Dictionary<int, int> lastVfxSpawnFrame = new();
    [SerializeField] private float vfxMinInterval = 0.03f;

    private void Awake()
    {
        character = GetComponent<Character>();
        if (!animator) animator = GetComponent<Animator>();
        if (!equipment) equipment = GetComponent<EquipmentSystem>();
        skillLock = GetComponent<SkillLock>();
    }

    public void SetForwardAnchor(Transform t) { forwardAnchor = t; }
    public void SetDefaultVfxSpawn(Transform t) { defaultVfxSpawn = t; }

    private void OnEnable()
    {
        RebuildAbilityMap();

        var wc = GetComponent<WeaponController>();
        if (wc != null)
        {
            wc.OnWeaponChanged -= OnWeaponChangedHandler;
            wc.OnWeaponChanged += OnWeaponChangedHandler;
        }

        RefreshActiveForCurrentWeapon(); // NEW
    }

    private void OnWeaponChangedHandler(WeaponSO so)
    {
        RebuildAbilityMap();
        RefreshActiveForCurrentWeapon(); // NEW
    }

    private void RefreshActiveForCurrentWeapon() // NEW
    {
        var w = equipment != null ? equipment.GetCurrentWeapon() : null;
        // Bật script chỉ khi đang cầm Axe
        enabled = (w != null && w.weaponType == WeaponType.Axe);
    }

    private void OnDisable()
    {
        var wc = GetComponent<WeaponController>();
        if (wc != null) wc.OnWeaponChanged -= OnWeaponChangedHandler;
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
        Debug.Log($"[AxeSkill.TryUse] key={input} pressed");

        var weapon = equipment != null ? equipment.GetCurrentWeapon() : null;

        bool inCombat = character != null && character.movementSM != null
                        && character.movementSM.currentState == character.combatMove;
        bool drawn = character != null && character.isWeaponDrawn;
        string weaponName = weapon != null ? weapon.weaponName : "None";
        Debug.Log($"[AxeSkill.TryUse] weapon={weaponName}, typeOK={(weapon != null && weapon.weaponType == WeaponType.Axe)}, inCombat={inCombat}, drawn={drawn}, isLock={skillLock != null && skillLock.isPerformingSkill}");

        if (weapon == null || weapon.weaponType != WeaponType.Axe || !inCombat || !drawn)
            return;

        if (skillLock != null && skillLock.isPerformingSkill) return;
        if (!abilityMap.TryGetValue(input, out var ability) || ability == null)
            return;

        // Check if skill is unlocked
        if (WeaponMasteryManager.Instance != null)
        {
            if (!WeaponMasteryManager.Instance.IsSkillUnlocked(WeaponType.Axe, input))
            {
                Debug.Log($"[AxeSkill.TryUse] {input} is locked! Mastery level required.");
                return;
            }
        }

        // Check cooldown
        var abilityIconManager = FindFirstObjectByType<AbilityIconManager>();
        if (abilityIconManager != null && abilityIconManager.IsOnCooldown(input))
        {
            Debug.Log($"[AxeSkill.TryUse] {input} is on cooldown!");
            return;
        }

        int idx = InputToIndex(input);
        animator.SetInteger(skillIndexParam, idx);
        animator.SetTrigger(skillTriggerParam);

        // if (skillLock == null)
        // {
        //     Debug.LogWarning("[AxeSkill] SkillLock is null -> cannot lock movement/root motion");
        // }
        // else
        // {
        //     Debug.Log("[AxeSkill] BeginSkillRootMotion");
        //     skillLock.BeginSkillRootMotion(animator, enableRootMotionDuringSkill);
        // }
        // skillLockExpireAt = Time.time + maxSkillLockSeconds;

        if (input == AbilityInput.Q_Ultimate && ultimateDirector != null)
        {
            // Lock skill immediately and start timeline
            skillLock?.BeginSkillRootMotion(animator, true);
            ultimateDirector.time = 0;
            ultimateDirector.Play();
        }
    }

    // ===================== Animation Events (AE-driven) =====================

    public void AE_StartDamage() => equipment?.StartDealDamage();
    public void AE_EndDamage() => equipment?.EndDealDamage();

    // Chỉ dùng AE_PlaySkillVFXByEvent
    public void AE_PlaySkillVFXByEvent(int eventIndex)
    {
        // Guard-1: đúng weapon type
        var weapon = equipment != null ? equipment.GetCurrentWeapon() : null;
        if (weapon == null || weapon.weaponType != WeaponType.Axe) return;

        // Guard-2: đúng animator state/tag/layer của Axe
        if (!IsInSkillState()) return;

        if (!animator) return;
        int skillIdx = animator.GetInteger(skillIndexParam);
        var input = (AbilityInput)(skillIdx == 0 ? AbilityInput.E :
                                   skillIdx == 1 ? AbilityInput.R :
                                   skillIdx == 2 ? AbilityInput.T :
                                                     AbilityInput.Q_Ultimate);

        if (!abilityMap.TryGetValue(input, out var ability) || ability == null) return;

        var events = ability.skillEvents;
        if (events == null || eventIndex < 0 || eventIndex >= events.Length) return;

        // Debounce theo frame/time tránh nhân đôi
        int lastFrame = lastVfxSpawnFrame.TryGetValue(eventIndex, out var lf) ? lf : -999999;
        if (lastFrame == Time.frameCount) return;
        lastVfxSpawnFrame[eventIndex] = Time.frameCount;
        float lastTime = lastVfxSpawnTime.TryGetValue(eventIndex, out var t) ? t : -999f;
        if (Time.time - lastTime < vfxMinInterval) return;
        lastVfxSpawnTime[eventIndex] = Time.time;

        var ev = events[eventIndex];
        var prefab = ev.vfxPrefab != null ? ev.vfxPrefab : ability.hitVfx;
        if (!prefab) return;

        var (pos, rot, scl) = BuildSpawnTransform(ev.spawnRule);
        var v = Instantiate(prefab, pos, rot); // world-space
        if (ev.spawnRule.extraEulerOffset != Vector3.zero)
        {
            v.transform.rotation *= Quaternion.Euler(ev.spawnRule.extraEulerOffset);
        }
        v.transform.localScale = Vector3.Scale(v.transform.localScale, scl);

        // Move (optional)
        if (ev.moveAfterSpawn)
        {
            var mover = v.GetComponent<VfxMover>();
            if (!mover) mover = v.AddComponent<VfxMover>();
            float life = ev.moveLifetime > 0f
                ? ev.moveLifetime
                : (ability.vfxDuration > 0f ? ability.vfxDuration : vfxDuration);
            mover.Launch(rot * Vector3.forward, ev.moveSpeed, life, ev.alignToDirection);
        }

        Destroy(v, ability.vfxDuration > 0 ? ability.vfxDuration : vfxDuration);
    }

    // NEW: Check state theo layer/tag để AE chỉ chạy đúng lúc
    private bool IsInSkillState()
    {
        if (!animator) return false;
        var st = animator.GetCurrentAnimatorStateInfo(axeLayerIndex);
        return st.IsTag(skillStateTag);
    }

    private int InputToIndex(AbilityInput input)
    {
        switch (input)
        {
            case AbilityInput.E: return 0;
            case AbilityInput.R: return 1;
            case AbilityInput.T: return 2;
            case AbilityInput.Q_Ultimate: return 3;
            default: return 0;
        }
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
        if (forwardAnchor != null)
        {
            Vector3 f = forwardAnchor.forward; f.y = 0f;
            return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
        }

        if (!useInputDirection)
        {
            Vector3 f = (character != null ? character.transform.forward : transform.forward);
            f.y = 0f; return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
        }

        var cam = Camera.main ? Camera.main.transform : null;
        Vector3 fwd = cam ? cam.forward : (character ? character.transform.forward : transform.forward);
        fwd.y = 0f;
        return fwd.normalized;
    }

    // ===================== Cooldown System =====================

    // Animation Event: Trigger cooldown for specific ability
    public void AE_TriggerCooldown(int inputIndex)
    {
        var abilityIconManager = FindFirstObjectByType<AbilityIconManager>();
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
    // Cancel currently playing ultimate/timeline and unlock skill if active
    public void CancelSkill()
    {
        if (ultimateDirector != null && ultimateDirector.state == PlayState.Playing)
        {
            ultimateDirector.Stop();
        }
        skillLock?.EndSkillRootMotion(animator);
    }
}