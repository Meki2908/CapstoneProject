using UnityEngine;
using GAP_ParticleSystemController;

/// <summary>
/// Normal attack system cho Mage.
/// </summary>
public class MageNormalAttack : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EquipmentSystem equipment;
    [SerializeField] private Transform defaultVfxSpawn;
    [SerializeField] private Animator animator;
    [SerializeField] private Character character;
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private EnemyDetection enemyDetection;

    [Header("Auto-Aim Settings")]
    [SerializeField] private LayerMask enemyLayerMask = -1;
    [SerializeField] private float autoAimRange = 15f;

    [Header("Spawn tuning")]
    [SerializeField] private float spawnForwardClearance = 0.38f;
    [SerializeField] private float spawnHeightBias = 0.12f;
    [SerializeField] private float ownerIgnoreCollisionSeconds = 0.14f;

    // === Smart target lock (fix "khựng chuỗi" + "đánh xác chết") ===
    private Transform currentLockedTarget;
    private readonly Collider[] hitCollidersCache = new Collider[64]; // NonAlloc buffer

    private void Awake()
    {
        if (!equipment) equipment = GetComponent<EquipmentSystem>();
        if (!animator) animator = GetComponent<Animator>();
        if (!character) character = GetComponentInParent<Character>();
        if (!weaponController) weaponController = character != null ? character.GetComponent<WeaponController>() : GetComponentInParent<WeaponController>();
        if (!enemyDetection) enemyDetection = GetComponentInParent<EnemyDetection>();
    }

    /// <summary>
    /// ĐƯỢC GỌI TRỰC TIẾP TỪ ATTACKSTATE KHI ĐẾN ĐÚNG GIÂY (vfxTime)
    /// </summary>
    public void FireProjectileFSM(int comboIndex)
    {
        if (!TryGetCurrentMageWeapon(out WeaponSO currentWeapon)) return;

        SpawnHitVFX(currentWeapon, comboIndex);
    }

    public bool TryGetPredictedSpawn(int comboIndex, out Vector3 spawnPos, out Quaternion spawnRot)
    {
        spawnPos = Vector3.zero;
        spawnRot = Quaternion.identity;

        if (!TryGetCurrentMageWeapon(out WeaponSO weapon)) return false;
        if (comboIndex < 0 || comboIndex >= weapon.hitTimings.Length) return false;

        Transform target = GetOrFindTarget(weapon, comboIndex);
        Transform anchor = ResolveSpawnAnchor();
        HitTiming timing = weapon.hitTimings[comboIndex];
        VfxSpawnRule rule = timing.spawnRule;

        Vector3 baseWorldPos = anchor.position + anchor.TransformDirection(rule.localOffset);
        Quaternion orientWorld = RotationFromSpawnRule(anchor.rotation, rule);

        Vector3 aimPoint = AimPointForTarget(target);
        Vector3 dir;
        if (target != null)
        {
            dir = aimPoint - baseWorldPos;
            if (dir.sqrMagnitude < 1e-6f) dir = orientWorld * Vector3.forward;
            else dir.Normalize();
        }
        else
        {
            dir = PlanarCharacterForward();
        }

        spawnPos = baseWorldPos + dir * spawnForwardClearance;
        spawnPos.y += spawnHeightBias;
        spawnRot = Quaternion.LookRotation(dir, Vector3.up);
        return true;
    }

    public void SpawnVFXDirect(int comboIndex, Vector3 spawnPos, Quaternion spawnRot)
    {
        if (!TryGetCurrentMageWeapon(out WeaponSO weapon)) return;
        GameObject vfxPrefab = GetHitVfxPrefab(weapon, comboIndex);
        if (vfxPrefab == null) return;
        float scale = 1f;
        if (comboIndex >= 0 && comboIndex < weapon.hitTimings.Length)
            scale = weapon.hitTimings[comboIndex].spawnRule.scale;
        SpawnProjectileVisual(vfxPrefab, comboIndex, spawnPos, spawnRot, null, scale);
    }

    private Transform ResolveSpawnAnchor()
    {
        if (defaultVfxSpawn != null) return defaultVfxSpawn;
        if (weaponController != null)
        {
            Transform anchor = weaponController.GetHeldWeaponSpawnAnchor();
            if (anchor != null) return anchor;
        }
        if (character != null) return character.transform;
        return transform;
    }

    private static Quaternion RotationFromSpawnRule(Quaternion anchorWorld, VfxSpawnRule r)
    {
        Quaternion q = anchorWorld;
        q *= Quaternion.AngleAxis(r.yawOffset, Vector3.up);
        Vector3 right = q * Vector3.right;
        q *= Quaternion.AngleAxis(r.pitchOffset, right);
        Vector3 fwd = q * Vector3.forward;
        q *= Quaternion.AngleAxis(r.rollOffset, fwd);
        q *= Quaternion.Euler(r.extraEulerOffset);
        return q;
    }

    private static Vector3 AimPointForTarget(Transform target)
    {
        if (target == null) return Vector3.zero;
        
        // Some enemies have multiple colliders (even "sideways" capsules) that can pull bounds.center too low.
        // Pick the collider with the highest top (bounds.max.y), then aim near its head area.
        Collider[] cols = target.GetComponentsInChildren<Collider>(true);
        Bounds best = default;
        bool hasBest = false;
        float bestTopY = float.NegativeInfinity;
        
        foreach (var c in cols)
        {
            if (c == null) continue;
            var b = c.bounds;
            if (b.size.sqrMagnitude < 1e-6f) continue;
            
            if (!hasBest || b.max.y > bestTopY)
            {
                hasBest = true;
                best = b;
                bestTopY = b.max.y;
            }
        }
        
        if (hasBest)
        {
            float headOffsetDown = Mathf.Clamp(best.size.y * 0.15f, 0.15f, 0.45f);
            Vector3 p = new Vector3(best.center.x, best.max.y - headOffsetDown, best.center.z);
            p.y = Mathf.Max(p.y, best.center.y + 0.6f);
            return p;
        }
        
        return target.position + Vector3.up * 1.2f;
    }

    // ĐÃ FIX: Lấy hướng mặt của Player, KHÔNG lấy hướng Camera nữa
    private Vector3 PlanarCharacterForward()
    {
        Vector3 f = character != null ? character.transform.forward : transform.forward;
        f.y = 0f;
        return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
    }

    private void SpawnHitVFX(WeaponSO weapon, int comboIndex)
    {
        if (weapon == null || comboIndex < 0 || comboIndex >= weapon.hitTimings.Length) return;

        Transform target = GetOrFindTarget(weapon, comboIndex);
        if (!TryGetPredictedSpawn(comboIndex, out Vector3 spawnPos, out Quaternion spawnRot))
            return;
        GameObject vfxPrefab = GetHitVfxPrefab(weapon, comboIndex);
        if (vfxPrefab == null) return;
        float scale = weapon.hitTimings[comboIndex].spawnRule.scale;
        SpawnProjectileVisual(vfxPrefab, comboIndex, spawnPos, spawnRot, target, scale);
    }

    private bool TryGetCurrentMageWeapon(out WeaponSO weapon)
    {
        weapon = equipment?.GetCurrentWeapon();
        return weapon != null && weapon.weaponType == WeaponType.Mage;
    }

    private static GameObject GetHitVfxPrefab(WeaponSO weapon, int comboIndex)
    {
        if (weapon == null || comboIndex < 0) return null;
        if (weapon.normalHitVfx == null || comboIndex >= weapon.normalHitVfx.Length) return null;
        return weapon.normalHitVfx[comboIndex];
    }

    private void SpawnProjectileVisual(GameObject vfxPrefab, int comboIndex, Vector3 spawnPos, Quaternion spawnRot, Transform target, float scale)
    {
        var vfx = Instantiate(vfxPrefab, spawnPos, spawnRot);
        if (scale > 0f)
            vfx.transform.localScale *= scale;

        if (character != null)
            BaseEffectScript.WireSpellOwnership(character, vfx);

        GameObject ownerRoot = character != null ? character.gameObject : gameObject;
        if (ownerIgnoreCollisionSeconds > 0f)
            MageProjectileCollisionIgnorer.Attach(vfx, ownerRoot, ownerIgnoreCollisionSeconds);

        var projectileScript = vfx.GetComponent<ProjectileMoveScript>();
        if (projectileScript != null)
        {
            if (target != null)
            {
                var rotateToMouse = vfx.GetComponent<RotateToMouseScript>();
                if (rotateToMouse == null) rotateToMouse = vfx.AddComponent<RotateToMouseScript>();
                projectileScript.SetTarget(target.gameObject, rotateToMouse);
            }
            projectileScript.accuracy = 100f;
        }

        var particleController = vfx.GetComponent<ParticleSystemController>();
        if (particleController != null)
        {
            particleController.size = 1f + (comboIndex * 0.2f);
            particleController.speed = 1f + (comboIndex * 0.1f);
            particleController.duration = 3f;
            particleController.UpdateParticleSystem();
        }

        Destroy(vfx, 5f);
    }

    private float GetAutoAimRange(WeaponSO weapon)
    {
        // Option A: keep Mage auto-aim range identical to the weapon attack range used by EnemyDetection.
        if (enemyDetection != null) return enemyDetection.MageAttackRange;
        return autoAimRange;
    }

    private Transform GetOrFindTarget(WeaponSO weapon, int comboIndex)
    {
        // Only rescan when starting a new combo hit, or when the previous target is no longer valid.
        if (comboIndex == 0 || !IsValidTarget(currentLockedTarget, weapon))
        {
            currentLockedTarget = FindNearestAliveEnemyOptimized(weapon);
        }
        return currentLockedTarget;
    }

    private bool IsValidTarget(Transform target, WeaponSO weapon)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;

        // Skip dead enemies (colliders can linger after death)
        TakeDamageTest hp = target.GetComponentInParent<TakeDamageTest>();
        if (hp != null && !hp.IsAlive()) return false;

        float range = GetAutoAimRange(weapon);
        float rangeSqr = range * range;
        float hysteresisRangeSqr = rangeSqr * 1.4f; // allow some slack to keep combo stable

        Transform selfRoot = character != null ? character.transform : transform;
        Vector3 to = target.position - selfRoot.position;
        to.y = 0f;
        return to.sqrMagnitude <= hysteresisRangeSqr;
    }

    private Transform FindNearestAliveEnemyOptimized(WeaponSO weapon)
    {
        Transform selfRoot = character != null ? character.transform : transform;
        Vector3 searchOrigin = selfRoot.position;

        float range = GetAutoAimRange(weapon);
        int count = Physics.OverlapSphereNonAlloc(searchOrigin, range, hitCollidersCache, enemyLayerMask);

        Transform nearest = null;
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider col = hitCollidersCache[i];
            if (col == null) continue;

            // Don't aim at self
            if (col.transform.root == selfRoot.root) continue;

            // Resolve to enemy root via HP component (preferred), otherwise use collider root.
            TakeDamageTest hp = col.GetComponentInParent<TakeDamageTest>();
            if (hp != null && !hp.IsAlive()) continue;

            Transform candidate = hp != null ? hp.transform : col.transform.root;
            if (candidate == null || !candidate.gameObject.activeInHierarchy) continue;

            Vector3 to = candidate.position - searchOrigin;
            to.y = 0f;
            float distSqr = to.sqrMagnitude;
            if (distSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distSqr;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, autoAimRange);
    }
}