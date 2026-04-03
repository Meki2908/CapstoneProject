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
    [Header("Ultimate VFX Pool")]
    [SerializeField] private bool enableUltimateVfxPool = true;
    [SerializeField] private int ultimatePoolPrewarm = 2;
    [SerializeField] private int ultimatePoolMaxPerPrefab = 8;

    private Character character;
    private SkillLock skillLock;
    private GameObject currentWeapon;
    private readonly Dictionary<AbilityInput, AbilitySO> abilityMap = new();
    private readonly Dictionary<int, float> lastVfxSpawnTime = new();
    private readonly Dictionary<int, int> lastVfxSpawnFrame = new();
    private readonly Dictionary<GameObject, Queue<GameObject>> ultimateVfxPools = new();
    private readonly Dictionary<GameObject, Coroutine> pooledReleaseRoutines = new();

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

        // Cancel ultimate timeline if playing (e.g. scene transition)
        CancelSkill();

        foreach (var kv in pooledReleaseRoutines)
        {
            if (kv.Value != null) StopCoroutine(kv.Value);
        }
        pooledReleaseRoutines.Clear();
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
        var abilityIconManager = FindFirstObjectByType<AbilityIconManager>();
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
            PrewarmUltimateVfxPool(ability);
            // Lock skill immediately and start timeline
            skillLock?.BeginSkillRootMotion(animator, true);
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
        bool usePool = ShouldUseUltimatePool(input, ev);

        // Spawn VFX
        var v = usePool ? SpawnFromUltimatePool(prefab as GameObject, pos, rot) : Instantiate(prefab as GameObject, pos, rot);
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
            // ShieldActivate trên prefab tự quản lý NavMeshObstacle + chặn damage

            // Destroy sau duration (ShieldActivate.OnDestroy sẽ reset IsShieldActive)
            float life = ability.vfxDuration > 0 ? ability.vfxDuration : vfxDuration;
            if (usePool) ScheduleReturnToUltimatePool(prefab as GameObject, v, life);
            else Destroy(v, life);
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

            float lifeTime = ability.vfxDuration > 0 ? ability.vfxDuration : vfxDuration;
            if (usePool) ScheduleReturnToUltimatePool(prefab as GameObject, v, lifeTime);
            else Destroy(v, lifeTime);
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
            // isSheathing = false; // Variable removed

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
        // isSheathing = true; // Variable removed
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

    private bool ShouldUseUltimatePool(AbilityInput input, SkillEvent ev)
    {
        return enableUltimateVfxPool && input == AbilityInput.Q_Ultimate && !ev.moveAfterSpawn;
    }

    private void PrewarmUltimateVfxPool(AbilitySO ability)
    {
        if (!enableUltimateVfxPool || ability == null || ability.skillEvents == null) return;
        int prewarmCount = Mathf.Max(1, ultimatePoolPrewarm);

        for (int i = 0; i < ability.skillEvents.Length; i++)
        {
            var prefab = ability.skillEvents[i].vfxPrefab != null ? ability.skillEvents[i].vfxPrefab : ability.hitVfx;
            if (prefab == null) continue;

            GameObject prefabGo = prefab as GameObject;
            if (prefabGo == null) continue;

            if (!ultimateVfxPools.TryGetValue(prefabGo, out var pool))
            {
                pool = new Queue<GameObject>();
                ultimateVfxPools[prefabGo] = pool;
            }

            while (pool.Count < prewarmCount)
            {
                var instance = Instantiate(prefabGo, transform.position, Quaternion.identity);
                instance.SetActive(false);
                pool.Enqueue(instance);
            }
        }
    }

    private GameObject SpawnFromUltimatePool(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!ultimateVfxPools.TryGetValue(prefab, out var pool))
        {
            pool = new Queue<GameObject>();
            ultimateVfxPools[prefab] = pool;
        }

        GameObject instance = null;
        while (pool.Count > 0 && instance == null)
        {
            instance = pool.Dequeue();
        }

        if (instance == null)
        {
            instance = Instantiate(prefab, pos, rot);
        }
        else
        {
            instance.transform.SetPositionAndRotation(pos, rot);
            instance.SetActive(true);
        }

        if (pooledReleaseRoutines.TryGetValue(instance, out var runningRoutine) && runningRoutine != null)
        {
            StopCoroutine(runningRoutine);
            pooledReleaseRoutines.Remove(instance);
        }

        var particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            ps.Clear(true);
            ps.Play(true);
        }

        return instance;
    }

    private void ScheduleReturnToUltimatePool(GameObject prefab, GameObject instance, float lifeTime)
    {
        if (instance == null) return;

        if (pooledReleaseRoutines.TryGetValue(instance, out var runningRoutine) && runningRoutine != null)
        {
            StopCoroutine(runningRoutine);
        }

        pooledReleaseRoutines[instance] = StartCoroutine(ReturnToUltimatePoolAfter(prefab, instance, lifeTime));
    }

    private System.Collections.IEnumerator ReturnToUltimatePoolAfter(GameObject prefab, GameObject instance, float lifeTime)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, lifeTime));
        if (instance == null) yield break;

        if (!ultimateVfxPools.TryGetValue(prefab, out var pool))
        {
            pool = new Queue<GameObject>();
            ultimateVfxPools[prefab] = pool;
        }

        pooledReleaseRoutines.Remove(instance);

        int maxPoolSize = Mathf.Max(1, ultimatePoolMaxPerPrefab);
        if (pool.Count >= maxPoolSize)
        {
            Destroy(instance);
            yield break;
        }

        instance.SetActive(false);
        pool.Enqueue(instance);
    }
}