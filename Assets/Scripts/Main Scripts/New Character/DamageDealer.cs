using System.Collections.Generic;
using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [SerializeField] float weaponLength = 2f;       // độ dài chém
    [SerializeField] float weaponDamage = 10f;      // dmg
    [SerializeField] float hitRadius = 0.3f;        // bán kính SphereCast
    [SerializeField] LayerMask targetLayer;         // kẻ địch nằm layer nào

    bool canDealDamage;
    List<GameObject> hasDealtDamage;

    void Start()
    {
        canDealDamage = false;
        hasDealtDamage = new List<GameObject>();
    }

    void Update()
    {
        if (canDealDamage)
        {
            RaycastHit[] hits = Physics.SphereCastAll(
                transform.position,         // điểm bắt đầu
                hitRadius,                  // bán kính
                transform.forward,          // hướng chém
                weaponLength,               // tầm chém
                targetLayer                 // chỉ chém trúng enemy layer
            );

            foreach (var hit in hits)
            {
                if (hit.transform.TryGetComponent(out TakeDamageTest enemy)
                    && !hasDealtDamage.Contains(hit.transform.gameObject))
                {
                    float finalDamage = weaponDamage;

                    // Apply damage multiplier from equipped gems (based on current weapon)
                    var wc = GetComponentInParent<WeaponController>();
                    if (wc != null && wc.GetCurrentWeapon() != null && WeaponGemManager.Instance != null)
                    {
                        float dmgMult = WeaponGemManager.Instance.GetDamageMultiplier(wc.GetCurrentWeapon().weaponType);
                        finalDamage *= dmgMult;
                    }

                    // Check for critical hit from equipment
                    bool isCrit = false;
                    float critDamageMultiplier = 1f;
                    if (EquipmentManager.Instance != null)
                    {
                        float critRate = EquipmentManager.Instance.GetTotalCritRateBonus();
                        float randomValue = Random.Range(0f, 1f);
                        isCrit = randomValue < critRate;

                        if (isCrit)
                        {
                            critDamageMultiplier = EquipmentManager.Instance.GetTotalCritDamageMultiplier();
                            finalDamage *= critDamageMultiplier;
                        }
                    }

                    enemy.TakeDamage(finalDamage);
                    hasDealtDamage.Add(hit.transform.gameObject);

                    if (isCrit)
                    {
                        Debug.Log($"[DamageDealer] Critical hit! Damage: {finalDamage} (multiplier: {critDamageMultiplier:F2}x)");
                    }
                }
            }
        }
    }

    public void StartDealDamage()
    {
        canDealDamage = true;
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
}
