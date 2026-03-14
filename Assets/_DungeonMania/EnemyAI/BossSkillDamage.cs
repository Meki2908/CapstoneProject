using UnityEngine;

/// <summary>
/// Gắn lên VFX prefab khi boss dùng skill.
/// Tạo vùng trigger damage — player bước vào = bị damage.
/// Hỗ trợ 2 chế độ:
///   - AoE (360°): damage mọi hướng (Stoneogre, Ifrit, Demon, Minotaur)
///   - Directional (cone): chỉ damage phía trước boss (Golem)
/// </summary>
public class BossSkillDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    private int damage = 10;
    private int magicDamage = 0;
    private int critDamage = 0;
    private int elementalType = 0;
    private float lifetime = 4f;
    private float damageRadius = 3f;
    
    // Directional skill
    private bool isDirectional = false;
    private float skillAngle = 90f;
    private Vector3 bossForward = Vector3.forward; // Hướng boss lúc cast
    private Vector3 bossPosition = Vector3.zero;   // Vị trí boss lúc cast
    
    private bool hasDamaged = false;
    private SphereCollider triggerCollider;
    private DungeonManiaPlayerBridge cachedBridge;
    
    /// <summary>
    /// Setup cho AoE skill (360° damage)
    /// </summary>
    public void Setup(int dmg, int magic, int crit, int eleType, float radius, float life)
    {
        damage = dmg;
        magicDamage = magic;
        critDamage = crit;
        elementalType = eleType;
        damageRadius = radius;
        lifetime = life;
        isDirectional = false;
        
        SetupCollider();
        
        Debug.Log($"[BossSkillDamage] Setup AoE: dmg={damage}, magic={magicDamage}, radius={damageRadius}");
    }
    
    /// <summary>
    /// Setup cho Directional skill (cone phía trước boss)
    /// </summary>
    public void SetupDirectional(int dmg, int magic, int crit, int eleType, float radius, float life, Vector3 bossPos, Vector3 bossFwd, float angle)
    {
        damage = dmg;
        magicDamage = magic;
        critDamage = crit;
        elementalType = eleType;
        damageRadius = radius;
        lifetime = life;
        isDirectional = true;
        bossPosition = bossPos;
        bossForward = bossFwd;
        skillAngle = angle;
        
        SetupCollider();
        
        Debug.Log($"[BossSkillDamage] Setup Directional: dmg={damage}, angle={skillAngle}°, forward={bossForward}");
    }
    
    private void SetupCollider()
    {
        // Tạo SphereCollider trigger
        triggerCollider = gameObject.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = damageRadius;
        
        // Đảm bảo có Rigidbody để trigger hoạt động
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        
        // Tự hủy sau lifetime
        Destroy(gameObject, lifetime);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (hasDamaged) return;
        
        // Tìm player
        GameObject playerObj = null;
        if (other.CompareTag("Player"))
        {
            playerObj = other.gameObject;
        }
        else if (other.transform.parent != null && other.transform.parent.CompareTag("Player"))
        {
            playerObj = other.transform.parent.gameObject;
        }
        
        if (playerObj == null) return;
        
        // Nếu directional → kiểm tra góc
        if (isDirectional)
        {
            Vector3 dirToPlayer = (playerObj.transform.position - bossPosition).normalized;
            dirToPlayer.y = 0; // Chỉ so sánh trên mặt phẳng ngang
            Vector3 forwardFlat = bossForward;
            forwardFlat.y = 0;
            forwardFlat.Normalize();
            
            float angle = Vector3.Angle(forwardFlat, dirToPlayer);
            
            if (angle > skillAngle / 2f)
            {
                // Player ở ngoài cone → KHÔNG damage
                Debug.Log($"[BossSkillDamage] Directional MISS: angle={angle:F1}° > {skillAngle/2f}°");
                return;
            }
            Debug.Log($"[BossSkillDamage] Directional HIT: angle={angle:F1}° <= {skillAngle/2f}°");
        }
        
        DealDamageToPlayer(playerObj);
    }
    
    private void DealDamageToPlayer(GameObject playerObj)
    {
        if (hasDamaged) return;
        
        // Tìm DungeonManiaPlayerBridge
        if (cachedBridge == null)
        {
            cachedBridge = playerObj.GetComponent<DungeonManiaPlayerBridge>();
            if (cachedBridge == null)
                cachedBridge = playerObj.GetComponentInParent<DungeonManiaPlayerBridge>();
            if (cachedBridge == null)
                cachedBridge = Object.FindAnyObjectByType<DungeonManiaPlayerBridge>();
        }
        
        if (cachedBridge != null)
        {
            hasDamaged = true;
            
            Damage skillDamage = new Damage();
            skillDamage.damage = damage;
            skillDamage.damageElemental = magicDamage;
            skillDamage.crit = critDamage;
            skillDamage.isBow = false;
            skillDamage.elementalType = elementalType;
            
            cachedBridge.PlayerDamage(skillDamage, 1);
            Debug.Log($"[BossSkillDamage] HIT! Player took {damage} + {magicDamage} magic damage!");
        }
        else
        {
            Debug.LogWarning("[BossSkillDamage] Cannot find DungeonManiaPlayerBridge!");
        }
    }
    
    /// <summary>
    /// Vẽ vùng damage trong Scene view (debug)
    /// </summary>
    private void OnDrawGizmos()
    {
        if (isDirectional)
        {
            Gizmos.color = Color.red;
            // Vẽ 2 đường tạo thành cone
            Vector3 leftDir = Quaternion.Euler(0, -skillAngle / 2f, 0) * bossForward;
            Vector3 rightDir = Quaternion.Euler(0, skillAngle / 2f, 0) * bossForward;
            Gizmos.DrawRay(bossPosition, leftDir * damageRadius);
            Gizmos.DrawRay(bossPosition, rightDir * damageRadius);
        }
        else
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, damageRadius);
        }
    }
}
