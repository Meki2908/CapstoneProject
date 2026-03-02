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
    
    // Flag để lazy init - Awake có thể chạy trước khi DungeonManiaPlayerBridge được setup
    private bool hasInitializedBridge = false;
    
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
    /// Thử tìm player và bridge. Gọi được nhiều lần (lazy init).
    /// Trả về true nếu đã tìm thấy bridge.
    /// </summary>
    private bool TrySetupPlayerReference() {
        if (hasInitializedBridge && playerBridge != null) return true;
        
        // ƯU TIÊN tìm player thật bằng tag "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) {
            playerObj = GameObject.Find("player");
        }
        
        if (playerObj != null) {
            player = playerObj.transform;
            
            // Tìm DungeonManiaPlayerBridge trên chính player
            playerBridge = playerObj.GetComponent<DungeonManiaPlayerBridge>();
            
            // Nếu không tìm thấy, thử tìm trên parent
            if (playerBridge == null && playerObj.transform.parent != null) {
                playerBridge = playerObj.transform.parent.GetComponent<DungeonManiaPlayerBridge>();
                if (playerBridge != null) {
                    player = playerObj.transform.parent;
                }
            }
            
            // Nếu vẫn không tìm thấy, thử tìm trên toàn bộ hierarchy (GetComponentInParent)
            if (playerBridge == null) {
                playerBridge = playerObj.GetComponentInParent<DungeonManiaPlayerBridge>();
                if (playerBridge != null) {
                    player = playerBridge.transform;
                }
            }
            
            // Fallback cuối: FindAnyObjectByType (tìm trong toàn scene)
            if (playerBridge == null) {
                playerBridge = Object.FindAnyObjectByType<DungeonManiaPlayerBridge>();
                if (playerBridge != null) {
                    player = playerBridge.transform;
                    Debug.Log($"[Bow] Found DungeonManiaPlayerBridge via FindAnyObjectByType on {playerBridge.gameObject.name}");
                }
            }
            
            if (playerBridge != null) {
                hasInitializedBridge = true;
                Debug.Log($"[Bow] Successfully found DungeonManiaPlayerBridge on {playerBridge.gameObject.name}");
                return true;
            }
        }
        
        return false;
    }
    
    private void OnParticleCollision(GameObject go){
        // Lazy init: thử tìm bridge nếu chưa có
        if (playerBridge == null) {
            TrySetupPlayerReference();
        }
        
        // Sử dụng bridge để gây damage
        if (playerBridge != null) {
            // Chuyển đổi Damage struct sang Damage struct của DungeonMania
            Damage bowDamage = new Damage();
            bowDamage.damage = damage.damage;
            bowDamage.elementalType = damage.elementalType;
            bowDamage.damageElemental = damage.damageElemental;
            bowDamage.crit = damage.crit;
            bowDamage.isBow = damage.isBow;
            bowDamage.isSpell = damage.isSpell;
            bowDamage.spellID = damage.spellID;
            playerBridge.PlayerDamage(bowDamage, hit);
        } else {
            Debug.LogWarning("[Bow] PlayerBridge still not found during collision! Cannot deal damage.");
        }
    }
    
    public void DamageBow(Damage d, int h){
        damage = d;
        hit = h;
        //if(isSkill)transform.position = new Vector3(player.position.x, 6f, player.position.z);
        ps.Play();
    }
}
