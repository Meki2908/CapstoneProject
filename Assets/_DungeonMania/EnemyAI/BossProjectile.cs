using UnityEngine;

/// <summary>
/// Gắn runtime lên Fireball VFX child khi boss spawn.
/// 2 chế độ:
///   - Self-move: tự bay về player (fallback khi không có ProjectileVfx)
///   - Collision only: chỉ detect va chạm, ProjectileVfx di chuyển transform
/// </summary>
public class BossProjectile : MonoBehaviour
{
    [HideInInspector] public int damage = 20;
    [HideInInspector] public int magicDamage = 5;
    [HideInInspector] public bool collisionOnly = false; // ProjectileVfx quản lý di chuyển
    
    // Self-move fields
    [HideInInspector] public float speed = 12f;
    [HideInInspector] public float lifetime = 5f;
    [HideInInspector] public Transform target;
    
    private bool hasDamaged = false;
    private Vector3 direction;
    private DungeonManiaPlayerBridge cachedBridge;
    
    /// <summary>
    /// Setup collision only mode — ProjectileVfx di chuyển, chỉ cần detect damage
    /// </summary>
    public void SetupCollisionOnly(int dmg, int magic, float life = 6f)
    {
        damage = dmg;
        magicDamage = magic;
        lifetime = life;
        collisionOnly = true;
        
        // Tạo trigger collider trên chính projectile child
        SphereCollider col = gameObject.GetComponent<SphereCollider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.5f; // Radius lớn hơn để dễ trúng
        
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        
        Destroy(gameObject, lifetime);
    }
    
    /// <summary>
    /// Setup self-move mode (fallback) — tự bay về player
    /// </summary>
    public void Setup(Transform playerTarget, int dmg, int magic, float projectileSpeed = 12f, float life = 5f)
    {
        target = playerTarget;
        damage = dmg;
        magicDamage = magic;
        speed = projectileSpeed;
        lifetime = life;
        collisionOnly = false;
        
        if (target != null)
        {
            direction = (target.position - transform.position).normalized;
            direction.y = 0;
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        SphereCollider col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.8f;
        
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        
        Destroy(gameObject, lifetime);
    }
    
    void Update()
    {
        if (collisionOnly || hasDamaged) return;
        transform.position += direction * speed * Time.deltaTime;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (hasDamaged) return;
        
        GameObject playerObj = null;
        if (other.CompareTag("Player"))
            playerObj = other.gameObject;
        else if (other.transform.parent != null && other.transform.parent.CompareTag("Player"))
            playerObj = other.transform.parent.gameObject;
        
        if (playerObj == null) return;
        
        hasDamaged = true;
        
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
            Damage dmg = new Damage();
            dmg.damage = damage;
            dmg.damageElemental = magicDamage;
            dmg.crit = 0;
            dmg.isBow = false;
            dmg.elementalType = 1; // fire
            cachedBridge.PlayerDamage(dmg, 1);
            Debug.Log($"[BossProjectile] COLLISION HIT! Player took {damage}+{magicDamage} damage!");
        }
    }
}
