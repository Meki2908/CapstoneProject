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

    [Header("Auto-Aim Settings")]
    [SerializeField] private LayerMask enemyLayerMask = -1;
    [SerializeField] private float autoAimRange = 15f;

    [Header("Spawn tuning")]
    [SerializeField] private float spawnForwardClearance = 0.38f;
    [SerializeField] private float spawnHeightBias = 0.12f;
    [SerializeField] private float ownerIgnoreCollisionSeconds = 0.14f;

    private void Awake()
    {
        if (!equipment) equipment = GetComponent<EquipmentSystem>();
        if (!animator) animator = GetComponent<Animator>();
        if (!character) character = GetComponentInParent<Character>();
        if (!weaponController) weaponController = character != null ? character.GetComponent<WeaponController>() : GetComponentInParent<WeaponController>();
    }

    /// <summary>
    /// ĐƯỢC GỌI TRỰC TIẾP TỪ ATTACKSTATE KHI ĐẾN ĐÚNG GIÂY (vfxTime)
    /// </summary>
    public void FireProjectileFSM(int comboIndex)
    {
        WeaponSO currentWeapon = equipment?.GetCurrentWeapon();
        if (currentWeapon == null || currentWeapon.weaponType != WeaponType.Mage) return;

        SpawnHitVFX(currentWeapon, comboIndex);
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
        var col = target.GetComponentInChildren<Collider>();
        if (col != null) return col.bounds.center;
        return target.position + Vector3.up * 0.9f;
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

        Transform target = FindNearestEnemy();
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
            dir = PlanarCharacterForward(); // Sử dụng hướng mặt nhân vật
        }

        Vector3 spawnPos = baseWorldPos + dir * spawnForwardClearance;
        spawnPos.y += spawnHeightBias;
        Quaternion spawnRot = Quaternion.LookRotation(dir, Vector3.up);

        if (weapon.normalHitVfx == null || comboIndex >= weapon.normalHitVfx.Length || weapon.normalHitVfx[comboIndex] == null)
            return;

        GameObject vfxPrefab = weapon.normalHitVfx[comboIndex];
        var vfx = Instantiate(vfxPrefab, spawnPos, spawnRot);

        if (rule.scale > 0f) vfx.transform.localScale *= rule.scale;

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

    private Transform FindNearestEnemy()
    {
        Transform selfRoot = character != null ? character.transform : transform;
        Vector3 searchOrigin = selfRoot.position;
        Collider[] enemies = Physics.OverlapSphere(searchOrigin, autoAimRange, enemyLayerMask);
        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.transform.root == selfRoot.root) continue;

            float distance = Vector3.Distance(searchOrigin, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = enemy.transform;
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