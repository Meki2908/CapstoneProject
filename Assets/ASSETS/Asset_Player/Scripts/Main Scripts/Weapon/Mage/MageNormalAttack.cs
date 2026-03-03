using UnityEngine;
using GAP_ParticleSystemController;

/// <summary>
/// Normal attack system cho Mage - tận dụng HitTiming và normalHitVfx từ WeaponSO
/// </summary>
public class MageNormalAttack : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EquipmentSystem equipment;
    [SerializeField] private Transform defaultVfxSpawn;     // vị trí spawn VFX mặc định (hand)
    [SerializeField] private Animator animator;

    [Header("Auto-Aim Settings")]
    [SerializeField] private LayerMask enemyLayerMask = -1;
    [SerializeField] private float autoAimRange = 15f;

    [Header("Combo Settings")]
    [SerializeField] private string comboParam = "comboIndex";  // Animator parameter cho combo
    [SerializeField] private float comboResetTime = 1f;        // Thời gian reset combo

    private int currentCombo = 0;
    private float lastAttackTime = 0f;
    private WeaponSO currentWeapon;

    private void Awake()
    {
        if (!equipment) equipment = GetComponent<EquipmentSystem>();
        if (!animator) animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Reset combo nếu quá lâu không đánh
        if (Time.time - lastAttackTime > comboResetTime)
        {
            currentCombo = 0;
        }
    }

    /// <summary>
    /// Thực hiện normal attack (gọi từ Animation Event)
    /// </summary>
    public void PerformNormalAttack()
    {
        currentWeapon = equipment?.GetCurrentWeapon();
        if (currentWeapon == null || currentWeapon.weaponType != WeaponType.Mage) return;
        if (currentWeapon.hitTimings == null || currentWeapon.hitTimings.Length == 0) return;

        // Nếu chọn AE cho VFX -> không spawn bằng script ở đây
        if (currentWeapon.normalVfxSpawnMode == WeaponSO.VfxSpawnMode.AnimationEvent)
        {
            // Chỉ cập nhật combo index/animator, VFX sẽ do AE gọi
            currentCombo = (currentCombo + 1) % currentWeapon.hitTimings.Length;
            lastAttackTime = Time.time;
            if (animator != null) animator.SetInteger(comboParam, currentCombo);
            return;
        }

        // Tăng combo index
        currentCombo = (currentCombo + 1) % currentWeapon.hitTimings.Length;
        lastAttackTime = Time.time;

        // Set animator parameter
        if (animator != null)
        {
            animator.SetInteger(comboParam, currentCombo);
        }

        // Spawn VFX cho hit timing hiện tại (chỉ khi dùng Script mode)
        if (currentWeapon.normalVfxSpawnMode == WeaponSO.VfxSpawnMode.Script)
            SpawnHitVFX(currentCombo);
    }

    /// <summary>
    /// Spawn VFX cho combo hit
    /// </summary>
    private void SpawnHitVFX(int comboIndex)
    {
        if (comboIndex >= currentWeapon.hitTimings.Length) return;

        var hitTiming = currentWeapon.hitTimings[comboIndex];

        // Tìm enemy gần nhất để auto-aim
        Transform target = FindNearestEnemy();
        Vector3 spawnPos = defaultVfxSpawn != null ? defaultVfxSpawn.position : transform.position;
        Vector3 direction = target != null ? (target.position - spawnPos).normalized : transform.forward;

        // Spawn VFX từ normalHitVfx array
        if (comboIndex < currentWeapon.normalHitVfx.Length && currentWeapon.normalHitVfx[comboIndex] != null)
        {
            var vfxPrefab = currentWeapon.normalHitVfx[comboIndex];
            var vfx = Instantiate(vfxPrefab, spawnPos, Quaternion.LookRotation(direction));

            // Setup ProjectileMoveScript nếu có
            var projectileScript = vfx.GetComponent<ProjectileMoveScript>();
            if (projectileScript != null)
            {
                // Setup auto-aim theo target (vendor API)
                if (target != null)
                {
                    var rotateToMouse = vfx.GetComponent<RotateToMouseScript>();
                    if (rotateToMouse == null) rotateToMouse = vfx.AddComponent<RotateToMouseScript>();
                    projectileScript.SetTarget(target.gameObject, rotateToMouse);
                }

                // Setup projectile settings
                projectileScript.speed = 20f; // Có thể lấy từ WeaponSO
                projectileScript.accuracy = 100f; // Perfect accuracy cho auto-aim
            }

            // Setup ParticleSystemController nếu có
            var particleController = vfx.GetComponent<ParticleSystemController>();
            if (particleController != null)
            {
                // Có thể điều chỉnh size, speed, color theo combo
                particleController.size = 1f + (comboIndex * 0.2f); // Combo càng cao, VFX càng lớn
                particleController.speed = 1f + (comboIndex * 0.1f); // Combo càng cao, VFX càng nhanh
                // Đảm bảo tồn tại >= 3 giây (duration trong controller là hệ số nhân thời lượng)
                particleController.duration = 3f;
                particleController.UpdateParticleSystem();
            }

            // Auto-destroy sau một thời gian
            Destroy(vfx, 5f);
        }
    }

    /// <summary>
    /// Tìm enemy gần nhất để auto-aim
    /// </summary>
    private Transform FindNearestEnemy()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, autoAimRange, enemyLayerMask);
        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Reset combo (gọi từ Animation Event khi kết thúc combo)
    /// </summary>
    public void ResetCombo()
    {
        currentCombo = 0;
        if (animator != null)
        {
            animator.SetInteger(comboParam, 0);
        }
    }

    /// <summary>
    /// Lấy combo hiện tại
    /// </summary>
    public int GetCurrentCombo()
    {
        return currentCombo;
    }

    private void OnDrawGizmosSelected()
    {
        // Debug draw auto-aim range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, autoAimRange);
    }
}
