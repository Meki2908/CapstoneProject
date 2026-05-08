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
    private EnemyDetection enemyDetection;
    public float maxSkillLockSeconds = 3f;
    private float skillLockExpireAt = 0f;

    private readonly Dictionary<AbilityInput, AbilitySO> abilityMap = new();
    // Debounce VFX
    private readonly Dictionary<int, float> lastVfxSpawnTime = new();
    private readonly Dictionary<int, int> lastVfxSpawnFrame = new();
    [SerializeField] private float vfxMinInterval = 0.03f;

    private void Awake()
    {
        character = GetComponentInParent<Character>();
        Debug.Log($"<color=green>[AxeSkill]</color> Character: {character}");
        if (!animator) animator = GetComponentInChildren<Animator>();
        Debug.Log($"<color=green>[AxeSkill]</color> Animator: {animator}");
        if (!equipment) equipment = GetComponentInChildren<EquipmentSystem>();
        Debug.Log($"<color=green>[AxeSkill]</color> Equipment: {equipment}");
        skillLock = GetComponentInChildren<SkillLock>();
        Debug.Log($"<color=green>[AxeSkill]</color> SkillLock: {skillLock}");
        enemyDetection = character != null ? character.GetComponentInChildren<EnemyDetection>(true) : null;
    }

    public void SetForwardAnchor(Transform t) { forwardAnchor = t; }
    public void SetDefaultVfxSpawn(Transform t) { defaultVfxSpawn = t; }

    private void Start()
    {
        var wc = GetComponent<WeaponController>();
        if (wc != null)
        {
            // Lắng nghe sự kiện đổi vũ khí
            wc.OnWeaponChanged += OnWeaponChangedHandler;
        }

        RebuildAbilityMap();
        RefreshActiveForCurrentWeapon();
    }

    private void OnDestroy()
    {
        // Gỡ lắng nghe khi script bị hủy
        var wc = GetComponent<WeaponController>();
        if (wc != null) wc.OnWeaponChanged -= OnWeaponChangedHandler;

        CancelSkill();
    }

    private void OnWeaponChangedHandler(WeaponSO so)
    {
        RebuildAbilityMap();
        RefreshActiveForCurrentWeapon();
    }

    private void RefreshActiveForCurrentWeapon()
    {
        var w = equipment != null ? equipment.GetCurrentWeapon() : null;
        // FIX: Không tự disable script nữa để hàm Update (Failsafe) luôn được chạy!
        // enabled = (w != null && w.weaponType == WeaponType.Axe);
    }
    public void RebuildAbilityMap()
    {
        abilityMap.Clear();
        var wc = GetComponent<WeaponController>();
        var weapon = wc != null ? wc.GetCurrentWeapon() : null;
        
        if (weapon == null || weapon.abilities == null) return;

        foreach (var ab in weapon.abilities)
        {
            if (ab == null || ab.input == AbilityInput.None) continue;
            abilityMap[ab.input] = ab;
        }
    }



    private void Update()
    {
        // CH? L�M NHI?M V? C?U H?, TUY?T �?I KH�NG NH?N INPUT ? ��Y
        if (skillLock != null && skillLock.isPerformingSkill)
        {
            if (Time.time > skillLockExpireAt)
            {
                Debug.LogWarning($"<color=red>[Failsafe]</color> Ph� v? kh�a Skill do k?t qu� {maxSkillLockSeconds} gi�y!");
                skillLock.EndSkillRootMotion(animator);
                
                if (ultimateDirector != null && ultimateDirector.state == PlayState.Playing)
                {
                    ultimateDirector.Stop();
                }
            }
        }
    }

    public void TryUse(AbilityInput input)
    {
        if (abilityMap.Count == 0) RebuildAbilityMap();

        var wc = GetComponent<WeaponController>();
        var weapon = wc != null ? wc.GetCurrentWeapon() : null;
        bool drawn = character != null && character.isWeaponDrawn;
        
        if (weapon == null || weapon.weaponType != WeaponType.Axe || !drawn)
        {
            Debug.Log($"<color=orange>[AxeSkill]</color> X?t: Sai vu kh� ho?c chua r�t R�u!");
            return;
        }

        if (!abilityMap.TryGetValue(input, out var ability)) 
        {
            Debug.Log($"<color=red>[AxeSkill]</color> X?t: Kh�ng t�m th?y Data c?a n�t {input} trong WeaponSO!");
            return;
        }

        if (WeaponMasteryManager.Instance != null && !WeaponMasteryManager.Instance.IsSkillUnlocked(WeaponType.Axe, input))
        {
            Debug.Log($"<color=yellow>[AxeSkill]</color> Skill {input} b? kh�a do chua d? Mastery!");
            return;
        }

        if (AbilityIconManager.Instance != null && AbilityIconManager.Instance.IsOnCooldown(input))
        {
            Debug.Log($"<color=grey>[AxeSkill]</color> Skill {input} dang trong th?i gian h?i chi�u!");
            return;
        }

        if (enemyDetection == null && character != null)
            enemyDetection = character.GetComponentInChildren<EnemyDetection>(true);
        Vector2 moveInp = character != null ? character.currentInput.movementInput : Vector2.zero;
        CombatAimHelper.SnapToTarget(character, equipment, enemyDetection, moveInp);

        int idx = input switch { AbilityInput.E => 0, AbilityInput.R => 1, AbilityInput.T => 2, AbilityInput.Q_Ultimate => 3, _ => 0 };
        animator.SetInteger(skillIndexParam, idx);
        animator.SetTrigger(skillTriggerParam);
        
        Debug.Log($"<color=cyan>[AxeSkill]</color> K�ch ho?t TH�NH C�NG Skill {input}!");

        // Cooldown should not depend on Animation Events (AE can be skipped in chaotic combat / networking).
        if (AbilityIconManager.Instance != null)
        {
            AbilityIconManager.Instance.TriggerCooldown(input);
        }

        if (skillLock != null) skillLock.BeginSkillRootMotion(animator);
        skillLockExpireAt = Time.time + maxSkillLockSeconds;

        if (input == AbilityInput.Q_Ultimate && ultimateDirector != null)
        {
            if (character != null && character.HasInputAuthority)
            {
                TimelineMainCameraBinder.BindToMainCameraBrain(ultimateDirector, warnOnce: true);
            }
            ultimateDirector.time = 0;
            ultimateDirector.Play();
        }

        TutorialTextDisplay.NotifySkillActivatedFromGameplay(input);
    }

    public void AE_PlaySkillVFXByEvent(int eventIndex)
    {
        // Guard-1: đúng weapon type
        var wc = GetComponent<WeaponController>();
        var weapon = wc != null ? wc.GetCurrentWeapon() : null;
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
        if (AbilityIconManager.Instance != null) 
        {
            AbilityIconManager.Instance.AE_TriggerCooldown(inputIndex);
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















