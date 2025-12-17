using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class MageSkills : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EquipmentSystem equipment;
    [SerializeField] private Transform defaultVfxSpawn;     // vị trí spawn VFX mặc định (hand)
    [SerializeField] private Animator animator;
    [SerializeField] private PlayableDirector ultimateDirector;  // Timeline for Q (optional)

    [Header("Animator Parameters")]
    [SerializeField] private string skillTriggerParam = "mageSkill";
    [SerializeField] private string skillIndexParam = "skillIndex";
    [SerializeField] private int mageLayerIndex = 3;
    [SerializeField] private string skillStateTag = "MageSkill";

    [Header("Mage-Specific")]
    [SerializeField] private Transform weaponSummonPoint;    // điểm summon weapon từ xa
    [SerializeField] private Transform weaponSheathPoint;    // điểm ném weapon ra sau
    [SerializeField] private MageNormalAttack normalAttack;  // normal attack system
    [SerializeField] private Transform forwardAnchor;
    [SerializeField] private bool useInputDirection = false;
    [SerializeField] private float vfxMinInterval = 0.03f;
    [SerializeField] private float vfxDuration = 3f;

    private Character character;
    private SkillLock skillLock;
    private GameObject currentWeapon;
    private bool isSheathing = false;
    private readonly Dictionary<AbilityInput, AbilitySO> abilityMap = new();
    private readonly Dictionary<int, float> lastVfxSpawnTime = new();
    private readonly Dictionary<int, int> lastVfxSpawnFrame = new();

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

        RefreshActiveForCurrentWeapon(); // NEW: tự bật/tắt theo weapon hiện tại
    }

    private void OnDisable()
    {
        var wc = GetComponent<WeaponController>();
        if (wc != null) wc.OnWeaponChanged -= OnWeaponChangedHandler;
    }

    private void OnWeaponChangedHandler(WeaponSO so)
    {
        RebuildAbilityMap();
        RefreshActiveForCurrentWeapon(); // NEW
    }

    // NEW: chỉ bật script khi đang cầm vũ khí Mage
    private void RefreshActiveForCurrentWeapon()
    {
        var w = equipment != null ? equipment.GetCurrentWeapon() : null;
        enabled = (w != null && w.weaponType == WeaponType.Mage);
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

        // Mage không cần drawn (Wand do WeaponController quản lý)
        if (weapon == null || weapon.weaponType != WeaponType.Mage || !inCombat)
            return;

        if (skillLock != null && skillLock.isPerformingSkill) return;
        if (!abilityMap.TryGetValue(input, out var ability) || ability == null)
            return;

        // Check if skill is unlocked
        if (WeaponMasteryManager.Instance != null)
        {
            if (!WeaponMasteryManager.Instance.IsSkillUnlocked(WeaponType.Mage, input))
            {
                Debug.Log($"[MageSkills.TryUse] {input} is locked! Mastery level required.");
                return;
            }
        }

        // Check cooldown
        var abilityIconManager = UnityEngine.Object.FindFirstObjectByType<AbilityIconManager>();
        if (abilityIconManager != null && abilityIconManager.IsOnCooldown(input))
        {
            Debug.Log($"[MageSkills.TryUse] {input} is on cooldown!");
            return;
        }

        int idx = input switch { AbilityInput.E => 0, AbilityInput.R => 1, AbilityInput.T => 2, AbilityInput.Q_Ultimate => 3, _ => 0 };
        animator.SetInteger(skillIndexParam, idx);
        animator.SetTrigger(skillTriggerParam);

        if (input == AbilityInput.Q_Ultimate && ultimateDirector != null)
        {
            ultimateDirector.time = 0;
            ultimateDirector.Play();
        }
    }
    // Mage giờ dùng logic VFX như Sword/Axe - loại bỏ projectile methods thừa

    // ===================== Animation Events =====================

    public void AE_StartDamage() => equipment?.StartDealDamage();
    public void AE_EndDamage() => equipment?.EndDealDamage();

    // CHUẨN HÓA: chỉ dùng AE_PlaySkillVFXByEvent để spawn VFX
    public void AE_PlaySkillVFXByEvent(int eventIndex)
    {
        // Guard-1: đúng weapon type
        var weapon = equipment != null ? equipment.GetCurrentWeapon() : null;
        if (weapon == null || weapon.weaponType != WeaponType.Mage) return;
        Debug.Log($"[MageSkills.AE_PlaySkillVFXByEvent] weapon={weapon.weaponName}, typeOK={(weapon != null && weapon.weaponType == WeaponType.Mage)}");

        // Guard-2: đúng animator state/tag/layer của Mage
        if (!IsInSkillState()) return;
        Debug.Log($"[MageSkills.AE_PlaySkillVFXByEvent] isInSkillState={IsInSkillState()}");
        if (!animator) return;
        int skillIdx = animator.GetInteger(skillIndexParam);
        var input = (AbilityInput)(skillIdx == 0 ? AbilityInput.E :
                                   skillIdx == 1 ? AbilityInput.R :
                                   skillIdx == 2 ? AbilityInput.T :
                                                     AbilityInput.Q_Ultimate);
        if (!abilityMap.TryGetValue(input, out var ability) || ability == null) return;
        var events = ability.skillEvents;
        if (events == null || eventIndex < 0 || eventIndex >= events.Length) return;

        // Debounce (per event)
        int lastFrame = lastVfxSpawnFrame.TryGetValue(eventIndex, out var lf) ? lf : -999999;
        if (lastFrame == Time.frameCount) return;
        lastVfxSpawnFrame[eventIndex] = Time.frameCount;
        float lastTime = lastVfxSpawnTime.TryGetValue(eventIndex, out var t) ? t : -999f;
        if (Time.time - lastTime < vfxMinInterval) return;
        lastVfxSpawnTime[eventIndex] = Time.time;

        var ev = events[eventIndex];
        var prefab = ev.vfxPrefab != null ? ev.vfxPrefab : ability.hitVfx;
        if (prefab == null) return;

        var (pos, rot, scl) = BuildSpawnTransform(ev.spawnRule);

        // Spawn VFX
        var v = Instantiate(prefab as GameObject, pos, rot);
        if (ev.spawnRule.extraEulerOffset != Vector3.zero)
        {
            v.transform.rotation *= Quaternion.Euler(ev.spawnRule.extraEulerOffset);
        }
        v.transform.localScale = Vector3.Scale(v.transform.localScale, scl);

        // Handle follow vs world space based on ability flag
        if (ability.isFollowPlayer)
        {
            // Attach FollowPlayer component để follow player
            var follow = v.GetComponent<FollowPlayer>();
            if (follow == null) follow = v.AddComponent<FollowPlayer>();
            follow.offset = ev.spawnRule.localOffset; // Use spawn offset as follow offset
            // Don't destroy - let FollowPlayer manage lifetime
        }
        else
        {
            // World space VFX - normal destroy
            if (ev.moveAfterSpawn)
            {
                var mover = v.GetComponent<VfxMover>();
                if (!mover) mover = v.AddComponent<VfxMover>();
                float life = ev.moveLifetime > 0f ? ev.moveLifetime : (ability.vfxDuration > 0f ? ability.vfxDuration : vfxDuration);
                mover.Launch(rot * Vector3.forward, ev.moveSpeed, life, ev.alignToDirection);
            }

            Destroy(v, ability.vfxDuration > 0 ? ability.vfxDuration : vfxDuration);
        }
    }

    // NEW: overload không tham số – an toàn cho clip cũ gọi AE_PlaySkillVFXByEvent()
    public void AE_PlaySkillVFXByEvent()
    {
        AE_PlaySkillVFXByEvent(0);
    }

    // NEW: Check state theo layer/tag để AE chỉ chạy đúng lúc
    private bool IsInSkillState()
    {
        if (!animator) return false;
        var st = animator.GetCurrentAnimatorStateInfo(mageLayerIndex);
        return st.IsTag(skillStateTag);
    }

    // ===== Helpers cho VFX theo spawnRule =====
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

    // ===================== Mage-Specific Logic (public để WeaponController gọi) =====================

    // THÊM: Overload với callback cho async complete
    public void SummonWeapon(System.Action<GameObject> onComplete = null)
    {
        var weapon = equipment?.GetCurrentWeapon();
        if (weapon == null || weapon.weaponPrefab == null)
        {
            onComplete?.Invoke(null);
            return;
        }

        Vector3 summonPos = weaponSummonPoint != null ? weaponSummonPoint.position :
                           transform.position + weapon.summonPosition;

        currentWeapon = Instantiate(weapon.weaponPrefab, summonPos, Quaternion.identity);
        SetWeaponVisible(currentWeapon, true);

        // WeaponMover removed - use simple transform movement
        Transform handTarget = defaultVfxSpawn != null ? defaultVfxSpawn : transform;
        StartCoroutine(MoveWeaponToTarget(currentWeapon, handTarget.position, weapon.summonSpeed, () =>
        {
            currentWeapon.transform.SetParent(handTarget);
            ApplySocket(currentWeapon.transform, weapon.handSocket);
            isSheathing = false;

            onComplete?.Invoke(currentWeapon); // THÊM: Callback return instance
        }));
    }

    // THÊM: Overload với callback cho async complete
    public void SheathWeapon(System.Action onComplete = null)
    {
        if (currentWeapon == null)
        {
            onComplete?.Invoke();
            return;
        }

        currentWeapon.transform.SetParent(null);

        var weapon = equipment?.GetCurrentWeapon();
        Vector3 sheathPos;
        if (weaponSheathPoint != null)
        {
            sheathPos = weaponSheathPoint.position;
        }
        else
        {
            Vector3 baseSheath = transform.TransformPoint(weapon != null ? weapon.sheathSocket.localPosition : Vector3.zero);
            var cam = Camera.main ? Camera.main.transform : null;
            Vector3 camOffset = (cam != null ? -cam.forward : -transform.forward) * 2f + (cam != null ? cam.up : Vector3.up) * 0.5f;
            sheathPos = baseSheath + camOffset;
        }
        float speed = weapon?.sheathSpeed ?? 8f;

        // WeaponMover removed - use simple transform movement
        isSheathing = true;
        SetWeaponVisible(currentWeapon, false);
        StartCoroutine(MoveWeaponToTarget(currentWeapon, sheathPos, speed, () =>
        {
            Destroy(currentWeapon);
            currentWeapon = null;
            onComplete?.Invoke(); // THÊM: Callback khi hoàn tất
        }));
    }

    private System.Collections.IEnumerator MoveWeaponToTarget(GameObject weapon, Vector3 targetPos, float speed, System.Action onComplete)
    {
        Vector3 startPos = weapon.transform.position;
        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / speed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            weapon.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        weapon.transform.position = targetPos;
        onComplete?.Invoke();
    }

    private void ApplySocket(Transform target, SocketOffset socket)
    {
        target.localPosition = socket.localPosition;
        target.localRotation = Quaternion.Euler(socket.localEuler);
        target.localScale = socket.localScale;
    }

    private void SetWeaponVisible(GameObject go, bool visible)
    {
        if (go == null) return;
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers) r.enabled = visible;
        var particles = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var p in particles)
        {
            if (visible)
            {
                p.Play();
            }
            else
            {
                p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    // ===================== Cooldown System =====================

    // Animation Event: Trigger cooldown for specific ability
    public void AE_TriggerCooldown(int inputIndex)
    {
        var abilityIconManager = UnityEngine.Object.FindFirstObjectByType<AbilityIconManager>();
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
}