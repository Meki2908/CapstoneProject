using System.Collections;
using UnityEngine;

public class WeaponHitRunner : MonoBehaviour
{
    [Header("Bindings (set when drawing/equipping)")]
    public WeaponSO weapon;                  // Data for the current weapon
    public EquipmentSystem equipment;        // Provides StartDealDamage/EndDealDamage on held weapon
    public Transform vfxSpawn;               // Optional anchor (hand tip). Position can be overridden dynamically.
    public Transform handReference;          // Optional hand bone for better anchoring
    public Transform characterRoot;          // Usually the character transform

    [Header("Behavior")]
    [Tooltip("Use input/camera forward for slash direction instead of character forward.")]
    public bool useInputDirection = true;
    [Tooltip("Auto-destroy spawned VFX after seconds.")]
    public float vfxLifetime = 2f;

    private Coroutine hitRoutine;
    private int currentHitIndex = 0; // Track current hit index

    public void Bind(WeaponSO weaponSO, EquipmentSystem equip, Transform spawnPoint, Transform hand, Transform root)
    {
        weapon = weaponSO;
        equipment = equip;
        vfxSpawn = spawnPoint;
        handReference = hand;
        characterRoot = root;
    }

    public void StartHit(int hitIndex)
    {
        if (weapon == null || weapon.hitTimings == null) return;
        if (hitIndex < 0 || hitIndex >= weapon.hitTimings.Length) return;

        currentHitIndex = hitIndex; // Store current hit index
        Debug.Log($"StartHit: hitIndex={hitIndex}, currentHitIndex={currentHitIndex}");

        if (hitRoutine != null) StopCoroutine(hitRoutine);
        hitRoutine = StartCoroutine(RunHitRoutine(hitIndex));
    }

    public void CancelCurrentHit()
    {
        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
            hitRoutine = null;
        }
        equipment?.EndDealDamage();
    }

    private IEnumerator RunHitRoutine(int hitIndex)
    {
        var timing = weapon.hitTimings[hitIndex];

        // Wait until damage window start
        if (timing.windowStart > 0f)
            yield return new WaitForSeconds(timing.windowStart);

        equipment?.StartDealDamage();

        // Wait until VFX moment
        float vfxDelay = Mathf.Max(0f, timing.vfxTime - timing.windowStart);
        if (vfxDelay > 0f)
            yield return new WaitForSeconds(vfxDelay);

        // Only spawn VFX if weapon is set to use Script for VFX
        if (weapon.normalVfxSpawnMode == WeaponSO.VfxSpawnMode.Script)
        {
            SpawnVfxForHit(hitIndex);
        }

        // Wait until window end
        float remain = Mathf.Max(0f, timing.windowEnd - Mathf.Max(timing.vfxTime, timing.windowStart));
        if (remain > 0f)
            yield return new WaitForSeconds(remain);

        equipment?.EndDealDamage();
        hitRoutine = null;
    }

    private void SpawnVfxForHit(int hitIndex)
    {
        if (weapon.normalHitVfx == null || hitIndex >= weapon.normalHitVfx.Length) return;
        var prefab = weapon.normalHitVfx[hitIndex];
        if (prefab == null) return;

        var timing = weapon.hitTimings[hitIndex];
        var (pos, rot, scl) = BuildSpawnTransform(timing);

        var vfx = Instantiate(prefab, pos, rot);
        // Apply extra per-hit rotation AFTER base rotation for final alignment (e.g., fix -90° issue)
        if (timing.spawnRule.extraEulerOffset != Vector3.zero)
        {
            vfx.transform.rotation = vfx.transform.rotation * Quaternion.Euler(timing.spawnRule.extraEulerOffset);
        }
        vfx.transform.localScale = Vector3.Scale(vfx.transform.localScale, scl);
        if (vfxLifetime > 0f) Destroy(vfx, vfxLifetime);
    }

    private (Vector3 pos, Quaternion rot, Vector3 scl) BuildSpawnTransform(HitTiming timing)
    {
        var rule = timing.spawnRule;

        // Base forward: input/camera or character
        Vector3 baseForward = GetBaseForward();
        if (baseForward.sqrMagnitude < 0.0001f) baseForward = Vector3.forward;

        // Yaw + pitch + roll rotations
        Quaternion yawRot = Quaternion.AngleAxis(rule.yawOffset, Vector3.up);
        Vector3 right = Vector3.Cross(Vector3.up, baseForward).normalized;
        Quaternion pitchRot = Quaternion.AngleAxis(rule.pitchOffset, right);
        Quaternion rollRot = Quaternion.AngleAxis(rule.rollOffset, baseForward);
        Quaternion finalRot = Quaternion.LookRotation(baseForward) * yawRot * pitchRot * rollRot;

        // Anchor selection (hand -> vfxSpawn -> character root -> self)
        Transform anchor =
            (handReference != null ? handReference :
            (vfxSpawn != null ? vfxSpawn :
            (characterRoot != null ? characterRoot : transform)));

        Vector3 worldOffset = finalRot * rule.localOffset;
        Vector3 pos = anchor.position + worldOffset;
        Vector3 scl = Vector3.one * (rule.scale <= 0f ? 1f : rule.scale);

        return (pos, finalRot, scl);
    }

    private Vector3 GetBaseForward()
    {
        // Always use player direction for VFX spawn
        return characterRoot != null ? characterRoot.forward : transform.forward;
    }

    private void OnEnable()
    {
        // Auto-find hand reference if not set
        if (handReference == null)
        {
            FindHandReference();
        }

        // Tự lắng nghe OnWeaponChanged để rebind khi đổi vũ khí
        var wc = GetComponent<WeaponController>();
        if (wc != null)
        {
            wc.OnWeaponChanged -= OnWeaponChangedHandler;
            wc.OnWeaponChanged += OnWeaponChangedHandler;
        }
    }

    private void OnDisable()
    {
        var wc = GetComponent<WeaponController>();
        if (wc != null) wc.OnWeaponChanged -= OnWeaponChangedHandler;
    }

    private void OnWeaponChangedHandler(WeaponSO so)
    {
        // Rebind với vũ khí mới (giữ lại equipment/vfxSpawn/handRef nếu có)
        var equip = equipment != null ? equipment : GetComponent<EquipmentSystem>();
        Transform handRef = handReference != null ? handReference : null;
        Transform spawn = vfxSpawn != null ? vfxSpawn : handRef != null ? handRef : transform;
        Bind(so, equip, spawn, handRef, transform);
    }

    private void FindHandReference()
    {
        // Try to find hand reference in common locations
        if (handReference == null)
        {
            // Look for common hand bone names
            string[] handNames = { "Hand_R", "RightHand", "R_Hand", "hand_r", "HandR", "Right_Hand" };

            foreach (string handName in handNames)
            {
                Transform found = FindChildRecursive(transform, handName);
                if (found != null)
                {
                    handReference = found;
                    break;
                }
            }
        }

        // If still not found, try to find any bone with "hand" in the name
        if (handReference == null)
        {
            Transform found = FindChildRecursive(transform, "hand");
            if (found != null)
            {
                handReference = found;
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(name.ToLower()))
                return child;

            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    // Animation Event methods - ORIGINAL SIMPLE VERSION
    public void AE_PlayNormalVFX()
    {
        // Only spawn VFX if weapon is set to use Animation Events for VFX
        if (weapon == null || weapon.normalVfxSpawnMode != WeaponSO.VfxSpawnMode.AnimationEvent) return;
        if (weapon.normalHitVfx == null || weapon.normalHitVfx.Length == 0) return;

        // Use current hit index
        if (currentHitIndex >= weapon.normalHitVfx.Length) return;

        var prefab = weapon.normalHitVfx[currentHitIndex];
        if (prefab == null) return;

        // ALWAYS use BuildSpawnTransform for proper spawn rules
        if (weapon.hitTimings != null && currentHitIndex < weapon.hitTimings.Length)
        {
            var timing = weapon.hitTimings[currentHitIndex];
            var (pos, rot, scl) = BuildSpawnTransform(timing);

            Debug.Log($"AE_PlayNormalVFX: hitIndex={currentHitIndex}, pos={pos}, rot={rot}, scl={scl}");

            var vfx = Instantiate(prefab, pos, rot);
            vfx.transform.localScale = Vector3.Scale(vfx.transform.localScale, scl);

            // Apply extra rotation if needed
            if (timing.spawnRule.extraEulerOffset != Vector3.zero)
            {
                vfx.transform.rotation = vfx.transform.rotation * Quaternion.Euler(timing.spawnRule.extraEulerOffset);
            }

            Debug.Log($"AE_PlayNormalVFX: Final VFX rotation={vfx.transform.rotation}");

            // Auto-destroy after lifetime
            if (vfxLifetime > 0f) Destroy(vfx, vfxLifetime);
        }
        else
        {
            // ALWAYS use BuildSpawnTransform even for fallback
            var (pos, rot, scl) = BuildSpawnTransform(new HitTiming
            {
                spawnRule = new VfxSpawnRule
                {
                    localOffset = Vector3.zero,
                    yawOffset = 0f,
                    pitchOffset = 0f,
                    rollOffset = 0f,
                    scale = 1f
                }
            });

            Debug.Log($"AE_PlayNormalVFX FALLBACK: pos={pos}, rot={rot}, scl={scl}");

            var vfx = Instantiate(prefab, pos, rot);
            vfx.transform.localScale = Vector3.Scale(vfx.transform.localScale, scl);
            if (vfxLifetime > 0f) Destroy(vfx, vfxLifetime);
        }
    }

    public void AE_PlayNormalVFX(int hitIndex)
    {
        // Only spawn VFX if weapon is set to use Animation Events for VFX
        if (weapon == null || weapon.normalVfxSpawnMode != WeaponSO.VfxSpawnMode.AnimationEvent) return;
        if (weapon.normalHitVfx == null || hitIndex >= weapon.normalHitVfx.Length) return;

        var prefab = weapon.normalHitVfx[hitIndex];
        if (prefab == null) return;

        // ALWAYS use BuildSpawnTransform for proper spawn rules
        if (weapon.hitTimings != null && hitIndex < weapon.hitTimings.Length)
        {
            var timing = weapon.hitTimings[hitIndex];
            var (pos, rot, scl) = BuildSpawnTransform(timing);

            Debug.Log($"AE_PlayNormalVFX({hitIndex}): pos={pos}, rot={rot}, scl={scl}");

            var vfx = Instantiate(prefab, pos, rot);
            vfx.transform.localScale = Vector3.Scale(vfx.transform.localScale, scl);

            // Apply extra rotation if needed
            if (timing.spawnRule.extraEulerOffset != Vector3.zero)
            {
                vfx.transform.rotation = vfx.transform.rotation * Quaternion.Euler(timing.spawnRule.extraEulerOffset);
            }

            Debug.Log($"AE_PlayNormalVFX({hitIndex}): Final VFX rotation={vfx.transform.rotation}");

            // Auto-destroy after lifetime
            if (vfxLifetime > 0f) Destroy(vfx, vfxLifetime);
        }
        else
        {
            // ALWAYS use BuildSpawnTransform even for fallback
            var (pos, rot, scl) = BuildSpawnTransform(new HitTiming
            {
                spawnRule = new VfxSpawnRule
                {
                    localOffset = Vector3.zero,
                    yawOffset = 0f,
                    pitchOffset = 0f,
                    rollOffset = 0f,
                    scale = 1f
                }
            });

            Debug.Log($"AE_PlayNormalVFX({hitIndex}) FALLBACK: pos={pos}, rot={rot}, scl={scl}");

            var vfx = Instantiate(prefab, pos, rot);
            vfx.transform.localScale = Vector3.Scale(vfx.transform.localScale, scl);
            if (vfxLifetime > 0f) Destroy(vfx, vfxLifetime);
        }
    }
}