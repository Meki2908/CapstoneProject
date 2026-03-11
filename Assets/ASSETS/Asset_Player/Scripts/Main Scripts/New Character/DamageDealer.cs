using System.Collections.Generic;
using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [SerializeField] float weaponLength = 2f;       // độ dài chém
    [SerializeField] float weaponDamage = 10f;      // dmg
    [SerializeField] float hitRadius = 0.3f;        // bán kính SphereCast
    [SerializeField] LayerMask targetLayer;         // kẻ địch nằm layer nào
    [Header("Audio")]
    [SerializeField] private AudioSource hitAudioSource;
    [Header("Debug")]
    [SerializeField] bool debugDamage = false;

    bool canDealDamage;
    bool hasPlayedHitSfxInCurrentSwing;
    List<GameObject> hasDealtDamage;

    void Start()
    {
        canDealDamage = false;
        hasPlayedHitSfxInCurrentSwing = false;
        hasDealtDamage = new List<GameObject>();

        if (hitAudioSource == null)
        {
            hitAudioSource = GetComponentInParent<AudioSource>();
        }
    }

    void Update()
    {
        if (canDealDamage)
        {
            // Primary detection: spherecast along forward (useful for swings/projectiles)
            RaycastHit[] hits = Physics.SphereCastAll(
                transform.position,
                hitRadius,
                transform.forward,
                weaponLength,
                targetLayer
            );

            HashSet<GameObject> processed = new HashSet<GameObject>();

            foreach (var hit in hits)
            {
                if (hit.transform == null) continue;
                var go = hit.transform.gameObject;
                if (processed.Contains(go) || hasDealtDamage.Contains(go)) continue;
                ProcessHitTransform(hit.transform, hit.point);
                processed.Add(go);
            }

            // Secondary detection: overlap capsule from base to top of weapon (covers vertical spikes)
            Vector3 capsuleStart = transform.position;
            Vector3 capsuleEnd = transform.position + Vector3.up * Mathf.Max(0.01f, weaponLength);
            Collider[] overlaps = Physics.OverlapCapsule(capsuleStart, capsuleEnd, Mathf.Max(0.01f, hitRadius), targetLayer);
            foreach (var col in overlaps)
            {
                if (col == null || col.transform == null) continue;
                var go = col.transform.gameObject;
                if (processed.Contains(go) || hasDealtDamage.Contains(go)) continue;
                Vector3 hitPoint = col.ClosestPoint(transform.position);
                ProcessHitTransform(col.transform, hitPoint);
                processed.Add(go);
            }
        }
    }

    public void StartDealDamage()
    {
        canDealDamage = true;
        hasPlayedHitSfxInCurrentSwing = false;
        hasDealtDamage.Clear(); // reset list cho mỗi cú vung
    }

    public void EndDealDamage()
    {
        canDealDamage = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 direction = transform.forward;

        // Vẽ sphere ở đầu ray để debug
        Gizmos.DrawWireSphere(transform.position, hitRadius);
        Gizmos.DrawLine(transform.position, transform.position + direction * weaponLength);
        Gizmos.DrawWireSphere(transform.position + direction * weaponLength, hitRadius);
    }

    private void ProcessHitTransform(Transform targetTransform, Vector3 hitPoint)
    {
        if (targetTransform == null) return;

        GameObject rootGo = targetTransform.gameObject;
        if (debugDamage) Debug.Log($"[DamageDealer] Processing hit: target={rootGo.name}, layer={LayerMask.LayerToName(rootGo.layer)}, damage={weaponDamage:F2}");

        // 1) IDamageable on self or parent
        var dmgable = targetTransform.GetComponentInParent<IDamageable>();
        if (dmgable != null)
        {
            int intDamage = Mathf.RoundToInt(weaponDamage);
            if (debugDamage) Debug.Log($"[DamageDealer] Found IDamageable on {dmgable.GetTransform().name}, applying {intDamage} damage");
            dmgable.TakeDamage(intDamage, hitPoint);
            hasDealtDamage.Add(rootGo);
            return;
        }

        // 2) PlayerHealth on self or parent
        var playerHealth = targetTransform.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            float finalDamage = weaponDamage;
            var wc = GetComponentInParent<WeaponController>();
            if (wc != null && wc.GetCurrentWeapon() != null && WeaponGemManager.Instance != null)
            {
                float dmgMult = WeaponGemManager.Instance.GetDamageMultiplier(wc.GetCurrentWeapon().weaponType);
                finalDamage *= dmgMult;
            }
            if (debugDamage) Debug.Log($"[DamageDealer] Hitting PlayerHealth on {playerHealth.gameObject.name} for {finalDamage:F2} at {hitPoint}");
            playerHealth.TakeDamage(finalDamage, hitPoint);
            hasDealtDamage.Add(rootGo);
            return;
        }

        // 3) Enemy TakeDamageTest (fallback)
        var enemy = targetTransform.GetComponentInParent<TakeDamageTest>();
        if (enemy != null)
        {
            float finalDamage = weaponDamage;
            var wc = GetComponentInParent<WeaponController>();
            if (wc != null && wc.GetCurrentWeapon() != null && WeaponGemManager.Instance != null)
            {
                float dmgMult = WeaponGemManager.Instance.GetDamageMultiplier(wc.GetCurrentWeapon().weaponType);
                finalDamage *= dmgMult;
            }

            // Crit calculation
            bool isCrit = false;
            const float BASE_CRIT_MULTIPLIER = 1.5f;
            float critDamageMultiplier = 1f;
            if (EquipmentManager.Instance != null)
            {
                float critRate = EquipmentManager.Instance.GetTotalCritRateBonus();
                if (Random.Range(0f, 1f) < critRate)
                {
                    isCrit = true;
                    critDamageMultiplier = BASE_CRIT_MULTIPLIER;
                    float equipmentCritBonus = EquipmentManager.Instance.GetTotalCritDamageMultiplier();
                    float equipmentBonus = equipmentCritBonus - 1f;
                    critDamageMultiplier = BASE_CRIT_MULTIPLIER + equipmentBonus;
                    finalDamage *= critDamageMultiplier;
                }
            }

            WeaponType currentWeaponType = WeaponType.None;
            var wc2 = GetComponentInParent<WeaponController>();
            if (wc2 != null && wc2.GetCurrentWeapon() != null) currentWeaponType = wc2.GetCurrentWeapon().weaponType;

            enemy.TakeDamage(finalDamage, currentWeaponType, isCrit);
            TryPlayWeaponHitSfx(currentWeaponType);
            hasDealtDamage.Add(rootGo);
            if (isCrit) Debug.Log($"[DamageDealer] Critical hit! Damage: {finalDamage} (multiplier: {critDamageMultiplier:F2}x)");
        }
    }

    private void TryPlayWeaponHitSfx(WeaponType weaponType)
    {
        if (hasPlayedHitSfxInCurrentSwing)
        {
            return;
        }

        if (weaponType != WeaponType.Sword && weaponType != WeaponType.Axe)
        {
            return;
        }

        SoundManager.PlayMeleeHit(weaponType, hitAudioSource, 1f);
        hasPlayedHitSfxInCurrentSwing = true;
    }
}
