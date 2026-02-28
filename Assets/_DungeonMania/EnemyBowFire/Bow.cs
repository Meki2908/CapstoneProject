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
        
        SetupPlayerReference();
	}
    
    private void SetupPlayerReference() {
        // Tìm player object (chữ thường) hoặc object có tag Player
        GameObject playerObj = GameObject.Find("player");
        if (playerObj == null) {
            playerObj = GameObject.FindGameObjectWithTag("Player");
        }
        
        if (playerObj != null) {
            player = playerObj.transform;
            // Tìm DungeonManiaPlayerBridge thay vì PlayerHelth
            playerBridge = playerObj.GetComponent<DungeonManiaPlayerBridge>();
            if (playerBridge == null) {
                // Fallback: thử tìm PlayerHealth và tạo bridge tạm
                PlayerHealth playerHealth = playerObj.GetComponent<PlayerHealth>();
                if (playerHealth != null) {
                    Debug.LogWarning("[Bow] PlayerHealth found but no DungeonManiaPlayerBridge! Please add DungeonManiaPlayerBridge to player.");
                }
            }
        } else {
            Debug.LogWarning("[Bow] Player not found!");
        }
    }
    
    private void OnParticleCollision(GameObject go){
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
            Debug.LogWarning("[Bow] PlayerBridge not found! Cannot deal damage.");
        }
    }
    
    public void DamageBow(Damage d, int h){
        damage = d;
        hit = h;
        //if(isSkill)transform.position = new Vector3(player.position.x, 6f, player.position.z);
        ps.Play();
    }
}
