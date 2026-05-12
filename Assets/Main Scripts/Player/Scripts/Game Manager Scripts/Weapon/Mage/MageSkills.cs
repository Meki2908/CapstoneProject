using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class MageSkills : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EquipmentSystem equipment;
    [SerializeField] private Transform defaultVfxSpawn;     // v? tr� spawn VFX m?c d?nh (hand)
    [SerializeField] private Animator animator;
    [SerializeField] private PlayableDirector ultimateDirector;  // Timeline for Q (optional)

    [Header("Animator Parameters")]
    [SerializeField] private string skillTriggerParam = "mageSkill";
    [SerializeField] private string skillIndexParam = "skillIndex";
    [SerializeField] private int mageLayerIndex = 3;
    [SerializeField] private string skillStateTag = "MageSkill";

    [Header("Mage-Specific")]
    [SerializeField] private Transform weaponSummonPoint;    // di?m summon weapon t? xa
    [SerializeField] private Transform weaponSheathPoint;    // di?m n�m weapon ra sau
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
    private EnemyDetection enemyDetection;
    public float maxSkillLockSeconds = 3f;
    private float skillLockExpireAt = 0f;
    private GameObject currentWeapon;
    private readonly Dictionary<AbilityInput, AbilitySO> abilityMap = new();
    private readonly Dictionary<int, float> lastVfxSpawnTime = new();
    private readonly Dictionary<int, int> lastVfxSpawnFrame = new();
    private readonly Dictionary<GameObject, Queue<GameObject>> ultimateVfxPools = new();
    private readonly Dictionary<GameObject, Coroutine> pooledReleaseRoutines = new();

    private void Awake()
    {
        character = GetComponentInParent<Character>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!equipment) equipment = GetComponentInChildren<EquipmentSystem>();
        skillLock = GetComponentInChildren<SkillLock>();
        enemyDetection = character != null ? character.GetComponentInChildren<EnemyDetection>(true) : null;
    }

    private void Start()
    {
        // L?ng nghe s? ki?n d?i vu kh� t? khi b?t d?u game
        var wc = GetComponent<WeaponController>();
        if (wc != null)
        {
            wc.OnWeaponChanged += OnWeaponChangedHandler;
        }

        // C?p nh?t tr?ng th�i b?t/t?t l?n d?u ti�n
        RefreshActiveForCurrentWeapon();
    }

    private void OnDestroy()
    {
        // Ch? h?y l?ng nghe khi c?c Player n�y ho�n to�n b? x�a kh?i map
        var wc = GetComponent<WeaponController>();
        if (wc != null) wc.OnWeaponChanged -= OnWeaponChangedHandler;
    }

    public void SetForwardAnchor(Transform t) { forwardAnchor = t; }
    public void SetDefaultVfxSpawn(Transform t) { defaultVfxSpawn = t; }

    private void OnEnable()
    {
        RebuildAbilityMap();
        RefreshActiveForCurrentWeapon(); 
    }

    private void OnDisable()
    {
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

    // NEW: ch? b?t script khi dang c?m vu kh� Mage
    private void RefreshActiveForCurrentWeapon()
    {
        var w = equipment != null ? equipment.GetCurrentWeapon() : null;
        // FIX: Kh�ng t? disable script n?a d? h�m Update (Failsafe) lu�n du?c ch?y!
        // enabled = (w != null && w.weaponType == WeaponType.Mage);
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
        // CH? L?M NHI?M V? C?U H?, TUY?T ??I KH?NG NH?N INPUT ? ??Y
        if (skillLock != null && skillLock.isPerformingSkill)
        {
            if (Time.time > skillLockExpireAt)
            {
                Debug.LogWarning($"<color=red>[Failsafe]</color> Ph? v? kh?a Skill do k?t qu? {maxSkillLockSeconds} gi?y!");
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
        
        if (weapon == null || weapon.weaponType != WeaponType.Mage || !drawn)
            return;

        if (!abilityMap.TryGetValue(input, out var ability))
            return;

        if (WeaponMasteryManager.Instance != null && !WeaponMasteryManager.Instance.IsSkillUnlocked(WeaponType.Mage, input))
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
                character.RPC_PlayPlayerUltimate(Character.PlayerUltimateVisualKind.Mage);
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
        // Guard-1: d�ng weapon type
        var wc = GetComponent<WeaponController>();
        var weapon = wc != null ? wc.GetCurrentWeapon() : null;
        if (weapon == null || weapon.weaponType != WeaponType.Mage) return;
// Guard-2: d�ng animator state/tag/layer c?a Mage
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
            // Attach FollowPlayer component d? follow player
            var follow = v.GetComponent<FollowPlayer>();
            if (follow == null) follow = v.AddComponent<FollowPlayer>();
            follow.offset = ev.spawnRule.localOffset; // Use spawn offset as follow offset
            // ShieldActivate tr�n prefab t? qu?n l� NavMeshObstacle + ch?n damage

            // Destroy sau duration (ShieldActivate.OnDestroy s? reset IsShieldActive)
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

    // NEW: overload kh�ng tham s? � an to�n cho clip cu g?i AE_PlaySkillVFXByEvent()
    public void AE_PlaySkillVFXByEvent()
    {
        AE_PlaySkillVFXByEvent(0);
    }

    // NEW: Check state theo layer/tag d? AE ch? ch?y d�ng l�c
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

    // ===================== Mage-Specific Logic (public d? WeaponController g?i) =====================

    // TH�M: Overload v?i callback cho async complete
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

            onComplete?.Invoke(currentWeapon); // TH�M: Callback return instance
        }));
    }

    // TH�M: Overload v?i callback cho async complete
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
            onComplete?.Invoke(); // TH�M: Callback khi ho�n t?t
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

    /// <summary>Invoked by <see cref="Character.RPC_PlayPlayerUltimate"/> on every peer so timeline/VFX are not input-only.</summary>
    public void PlayUltimateFromNetworkRpc()
    {
        if (ultimateDirector == null) return;
        ultimateDirector.Stop();
        ultimateDirector.time = 0f;
        ultimateDirector.Play();
        if (character != null && character.HasInputAuthority)
            TimelineMainCameraBinder.BindToMainCameraBrain(ultimateDirector, warnOnce: true);
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















