using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Bow : MonoBehaviour {
    ParticleSystem ps;
    Damage damage;
    public bool isSkill;
    Transform player;
    // Sử dụng bridge thay vì PlayerHelth trực tiếp
    DungeonManiaPlayerBridge playerBridge;
    int hit;
    private bool hasLoggedCollision = false;
    
    // Flag để lazy init - Awake có thể chạy trước khi DungeonManiaPlayerBridge được setup
    private bool hasInitializedBridge = false;

    private bool TryResolveBridgeFromTransform(Transform t, out DungeonManiaPlayerBridge bridge, out Transform owner)
    {
        bridge = null;
        owner = null;
        if (t == null) return false;

        bridge = t.GetComponent<DungeonManiaPlayerBridge>();
        if (bridge == null) bridge = t.GetComponentInChildren<DungeonManiaPlayerBridge>(true);
        if (bridge == null) bridge = t.GetComponentInParent<DungeonManiaPlayerBridge>();
        if (bridge == null) return false;
        if (!bridge.IsAlive()) return false;

        owner = bridge.transform;
        return true;
    }

    private bool RefreshTargetFromEnemyContext()
    {
        EnemyScript enemyScript = GetComponentInParent<EnemyScript>();
        if (enemyScript == null) return false;
        if (enemyScript.target == null) return false;

        if (TryResolveBridgeFromTransform(enemyScript.target, out DungeonManiaPlayerBridge targetBridge, out Transform targetOwner))
        {
            playerBridge = targetBridge;
            player = targetOwner;
            hasInitializedBridge = true;
            return true;
        }

        return false;
    }
    
	void Awake () {
        // Tìm particle trên object này
        ps = GetComponent<ParticleSystem>();
        
        // Nếu không có, tìm trong children
        if (ps == null)
        {
            ParticleSystem[] children = GetComponentsInChildren<ParticleSystem>();
            if (children.Length > 0)
            {
                ps = children[0];
                Debug.Log($"[Bow] Found particle system in children: {ps.gameObject.name}");
            }
        }
        else
        {
            Debug.Log($"[Bow] Found particle system on same object: {ps.gameObject.name}");
        }
        
        if (ps == null)
        {
            Debug.LogWarning("[Bow] No ParticleSystem found! Make sure ParticleSystem is a child of this object.");
        }
        
        // Thử tìm bridge ngay, nhưng không báo lỗi nếu chưa tìm thấy (lazy init sau)
        TrySetupPlayerReference();
	}
    
    /// <summary>
    /// Reset cache khi enemy được bật lại (pooling/scene transition)
    /// </summary>
    private void OnEnable()
    {
        hasInitializedBridge = false;
        playerBridge = null;
        player = null;
        TrySetupPlayerReference();
    }
    
    /// <summary>
    /// Thử tìm player và bridge. Gọi được nhiều lần (lazy init).
    /// Trả về true nếu đã tìm thấy bridge.
    /// </summary>
    private bool TrySetupPlayerReference() {
        if (RefreshTargetFromEnemyContext()) return true;
        if (hasInitializedBridge && playerBridge != null && playerBridge.IsAlive()) return true;

        // fallback: pick any alive player bridge in scene
        DungeonManiaPlayerBridge[] bridges = Object.FindObjectsByType<DungeonManiaPlayerBridge>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        float bestSqr = float.MaxValue;
        DungeonManiaPlayerBridge best = null;
        Vector3 from = transform.position;
        for (int i = 0; i < bridges.Length; i++)
        {
            var b = bridges[i];
            if (b == null || !b.IsAlive()) continue;
            float sqr = (b.transform.position - from).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = b;
            }
        }

        if (best != null)
        {
            playerBridge = best;
            player = best.transform;
            hasInitializedBridge = true;
            Debug.Log($"[Bow] Fallback bridge target -> {playerBridge.gameObject.name}");
            return true;
        }

        return false;
    }
    
    private void OnParticleCollision(GameObject go){
        if (!hasLoggedCollision) {
            Debug.Log($"[Bow] OnParticleCollision triggered! hit={go.name}, layer={LayerMask.LayerToName(go.layer)}, tag={go.tag}, damage.damage={damage.damage}");
            hasLoggedCollision = true;
        }
        
        DungeonManiaPlayerBridge hitBridge = null;
        Transform hitOwner = null;
        if (go != null)
            TryResolveBridgeFromTransform(go.transform, out hitBridge, out hitOwner);

        if (hitBridge == null)
        {
            TrySetupPlayerReference();
            hitBridge = playerBridge;
        }

        // Sử dụng bridge đúng object va chạm để gây damage
        if (hitBridge != null) {
            // Chuyển đổi Damage struct sang Damage struct của DungeonMania
            Damage bowDamage = new Damage();
            bowDamage.damage = damage.damage;
            bowDamage.elementalType = damage.elementalType;
            bowDamage.damageElemental = damage.damageElemental;
            bowDamage.crit = damage.crit;
            bowDamage.isBow = damage.isBow;
            bowDamage.isSpell = damage.isSpell;
            bowDamage.spellID = damage.spellID;
            hitBridge.PlayerDamage(bowDamage, hit);
        } else {
            Debug.LogWarning("[Bow] PlayerBridge still not found during collision! Cannot deal damage.");
        }
    }
    
    public void DamageBow(Damage d, int h){
        damage = d;
        hit = h;
        hasLoggedCollision = false;
        
        // Refresh target every shot, avoid stale dead target cache.
        if (!TrySetupPlayerReference())
            player = null;
        
        Debug.Log($"[Bow] DamageBow called: damage={d.damage}, isSkill={isSkill}, ps={ps?.name ?? "NULL"}");
        
        if (isSkill && player != null) {
            // Skill → spawn tại vị trí player, cao hơn 5m để thấy toàn bộ hiệu ứng
            transform.position = new Vector3(player.position.x, player.position.y + 5f, player.position.z);
            Debug.Log($"[Bow] Skill positioned at player: {transform.position}");
        }
        else if (!isSkill && player != null) {
            // Bow thường → XOAY về hướng player để đạn bay trúng
            // Nhắm vào giữa thân player (y + 1m) thay vì bắn thẳng → tránh bắn qua đầu
            Vector3 targetPos = player.position + Vector3.up * 1f;
            Vector3 direction = targetPos - transform.position;
            if (direction.sqrMagnitude > 0.01f) {
                transform.rotation = Quaternion.LookRotation(direction);
            }
            Debug.Log($"[Bow] Bow aimed at player center: {targetPos}");
        }
        
        if (ps != null) {
            ps.Play();
        } else {
            Debug.LogWarning("[Bow] ParticleSystem is null!");
        }
    }
}
