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
    [SerializeField] private int swordLayerIndex = 1;
    [SerializeField] private string skillStateTag = "SwordSkill";

    private float skillLockExpireAt = 0f;
    private Animator animator;
    private Character character;
    private SkillLock skillLock;
    private EnemyDetection enemyDetection;

    private readonly Dictionary<AbilityInput, AbilitySO> abilityMap = new();

    private readonly Dictionary<int, float> lastVfxSpawnTime = new();
    [SerializeField] private float vfxMinInterval = 0.05f;

    [Header("Spawn Rule")]
    [SerializeField] private bool useInputDirection = false;
    [SerializeField] private Transform forwardAnchor;

    public void SetForwardAnchor(Transform t) => forwardAnchor = t;
    public void SetDefaultVfxSpawn(Transform t) => defaultVfxSpawn = t;

    public void AE_TriggerCooldown(int inputIndex)
    {
        if (AbilityIconManager.Instance != null) 
        {
            AbilityIconManager.Instance.AE_TriggerCooldown(inputIndex);
        }
    }

    public void AE_TriggerECooldown() => AE_TriggerCooldown((int)AbilityInput.E);
    public void AE_TriggerRCooldown() => AE_TriggerCooldown((int)AbilityInput.R);
    public void AE_TriggerTCooldown() => AE_TriggerCooldown((int)AbilityInput.T);
    public void AE_TriggerUltimateCooldown() => AE_TriggerCooldown((int)AbilityInput.Q_Ultimate);

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        character = GetComponentInParent<Character>();
        skillLock = GetComponentInChildren<SkillLock>();
        equipment = GetComponentInChildren<EquipmentSystem>();
        enemyDetection = character != null ? character.GetComponentInChildren<EnemyDetection>(true) : null;
    }

    private void Start()
    {
        // Lắng nghe sự kiện đổi vũ khí từ khi bắt đầu game
        var wc = GetComponent<WeaponController>();
        if (wc != null)
        {
            wc.OnWeaponChanged += OnWeaponChangedHandler;
        }

        // Cập nhật lại bản đồ kỹ năng lần đầu
        RebuildAbilityMap();
        RefreshActiveForCurrentWeapon(); // Thêm hàm này để đồng bộ với cơ chế mới
    }

    private void OnDestroy()
    {
        // Chỉ hủy lắng nghe khi cục Player này bị xóa
        var wc = GetComponent<WeaponController>();
        if (wc != null) wc.OnWeaponChanged -= OnWeaponChangedHandler;
        
        // Hủy cutscene nếu có
        CancelSkill();
    }

    private void OnWeaponChangedHandler(WeaponSO so)
    {
        RebuildAbilityMap();
        RefreshActiveForCurrentWeapon();
    }

    // Hàm tự bật/tắt script theo vũ khí đang cầm
    private void RefreshActiveForCurrentWeapon()
    {
        var w = equipment != null ? equipment.GetCurrentWeapon() : null;
        // FIX: Không tự disable script nữa để hàm Update (Failsafe) luôn được chạy!
        // enabled = (w != null && w.weaponType == WeaponType.Sword);
    }
    
    public void CancelSkill()
    {
        if (ultimateDirector != null && ultimateDirector.state == UnityEngine.Playables.PlayState.Playing)
            ultimateDirector.Stop();
            
        if (skillLock != null) skillLock.EndSkillRootMotion(animator);
    }

    /// <summary>Invoked by <see cref="Character.RPC_PlayPlayerUltimate"/> on every peer.</summary>
    public void PlayUltimateFromNetworkRpc()
    {
        if (ultimateDirector == null) return;
        ultimateDirector.Stop();
        ultimateDirector.time = 0f;
        ultimateDirector.Play();
        if (character != null && character.HasInputAuthority)
            TimelineMainCameraBinder.BindToMainCameraBrain(ultimateDirector, warnOnce: true);
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
        // CHỈ LÀM NHIỆM VỤ CỨU HỘ, TUYỆT ĐỐI KHÔNG NHẬN INPUT Ở ĐÂY
        if (skillLock != null && skillLock.isPerformingSkill)
        {
            if (Time.time > skillLockExpireAt)
            {
                Debug.LogWarning($"<color=red>[Failsafe]</color> Phá vỡ khóa Skill do kẹt quá {maxSkillLockSeconds} giây!");
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
        
        if (weapon == null || weapon.weaponType != WeaponType.Sword || !drawn)
            return;

        if (!abilityMap.TryGetValue(input, out var ability))
            return;

        if (WeaponMasteryManager.Instance != null && !WeaponMasteryManager.Instance.IsSkillUnlocked(WeaponType.Sword, input))
            return;

        if (AbilityIconManager.Instance != null && AbilityIconManager.Instance.IsOnCooldown(input))
            return;

        if (enemyDetection == null && character != null)
            enemyDetection = character.GetComponentInChildren<EnemyDetection>(true);
        Vector2 moveInp = character != null ? character.currentInput.movementInput : Vector2.zero;
        CombatAimHelper.SnapToTarget(character, equipment, enemyDetection, moveInp);

        int idx = input switch { AbilityInput.E => 0, AbilityInput.R => 1, AbilityInput.T => 2, AbilityInput.Q_Ultimate => 3, _ => 0 };
        animator.SetInteger(skillIndexParam, idx);
        if (character != null)
            character.SetTriggerSafe(skillTriggerParam);
        else
            animator.SetTrigger(skillTriggerParam);

        // Cooldown should not depend on Animation Events (AE can be skipped in chaotic combat / networking).
        if (AbilityIconManager.Instance != null)
        {
            AbilityIconManager.Instance.TriggerCooldown(input);
        }

        if (skillLock != null) skillLock.BeginSkillRootMotion(animator);
        skillLockExpireAt = Time.time + maxSkillLockSeconds;

        if (input == AbilityInput.Q_Ultimate && ultimateDirector != null)
        {
            bool useNet = character != null && character.Object != null && character.Object.IsValid
                && character.Runner != null && character.Runner.IsRunning;
            if (useNet && character.HasInputAuthority)
                character.RPC_PlayPlayerUltimate(Character.PlayerUltimateVisualKind.Sword);
            else
            {
                if (character != null && character.HasInputAuthority)
                    TimelineMainCameraBinder.BindToMainCameraBrain(ultimateDirector, warnOnce: true);
                ultimateDirector.time = 0;
                ultimateDirector.Play();
            }
        }

        TutorialTextDisplay.NotifySkillActivatedFromGameplay(input);
    }

    public void AE_PlaySkillVFXByEvent(int eventIndex)
    {
        var wc = GetComponent<WeaponController>();
        var weapon = wc != null ? wc.GetCurrentWeapon() : null;
        if (weapon == null || weapon.weaponType != WeaponType.Sword) return;
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
        var v = Instantiate(prefab, pos, rot);
        
        if (ev.spawnRule.extraEulerOffset != Vector3.zero) v.transform.rotation *= Quaternion.Euler(ev.spawnRule.extraEulerOffset);
        v.transform.localScale = Vector3.Scale(v.transform.localScale, scl);

        if (ev.moveAfterSpawn)
        {
            var mover = v.GetComponent<VfxMover>() ?? v.AddComponent<VfxMover>();
            float life = ev.moveLifetime > 0f ? ev.moveLifetime : (ability.vfxDuration > 0f ? ability.vfxDuration : vfxDuration);
            mover.Launch(rot * Vector3.forward, ev.moveSpeed, life, ev.alignToDirection);
        }

        Destroy(v, ability.vfxDuration > 0 ? ability.vfxDuration : vfxDuration);
    }

    public void AE_StartDamage() => equipment?.StartDealDamage();
    public void AE_EndDamage() => equipment?.EndDealDamage();

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

