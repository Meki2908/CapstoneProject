using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EnemyAttack : MonoBehaviour {
    EnemyScript enemyScript;
    Damage damageStruct;
    EnemyAttackEffects attackEffects;
    
    private void Start(){
        enemyScript = GetComponent<EnemyScript> ();
        // Tìm EnemyAttackEffects
        attackEffects = GetComponent<EnemyAttackEffects>();
        if (attackEffects == null)
            attackEffects = GetComponentInChildren<EnemyAttackEffects>();
    }
    public void StartAction(string anim){
        if(HeroInformation.alive && enemyScript != null){
            if(!enemyScript.attack && !enemyScript.enemyState.isStop){
                enemyScript.attack = true;
                // Chỉ attack khi KHÔNG bị dừng (isStop = false)
                enemyScript.animator.Play(anim);
                // attack sẽ được reset trong EnemyState khi animation kết thúc
            }
        }
    }
    public void StopAttack(){
        enemyScript.attack = false;
    }
    public void DamageToPlayer (int hit) {
        if(enemyScript == null) {
            Debug.LogWarning("[EnemyAttack] DamageToPlayer: enemyScript is null!");
            return;
        }

        Debug.Log($"[EnemyAttack] DamageToPlayer called. playerManager={(enemyScript.playerManager != null ? "exists" : "null")}, target={(enemyScript.target != null ? enemyScript.target.name : "null")}");

        // Check if using DungeonManiaPlayerBridge (user's player system)
        if (enemyScript.playerManager == null) {
            // Try to find DungeonManiaPlayerBridge on the player
            if (enemyScript.target != null) {
                // enemyScript.target is likely the "player" (lowercase) child object
                // We need to find the actual parent Player object
                GameObject targetGameObj = enemyScript.target.gameObject;
                
                // If target is a child (like "player"), try to find bridge on parent or by tag
                DungeonManiaPlayerBridge bridge = targetGameObj.GetComponent<DungeonManiaPlayerBridge>();
                
                // If not found on target, try parent (actual Player)
                if (bridge == null && targetGameObj.transform.parent != null) {
                    bridge = targetGameObj.transform.parent.GetComponent<DungeonManiaPlayerBridge>();
                }
                
                // If still not found, try finding by tag
                GameObject playerByTag = null;
                if (bridge == null) {
                    playerByTag = GameObject.FindGameObjectWithTag("Player");
                    Debug.Log($"[EnemyAttack] Search by tag 'Player': {(playerByTag != null ? playerByTag.name : "NOT FOUND")}");
                    if (playerByTag != null) {
                        bridge = playerByTag.GetComponent<DungeonManiaPlayerBridge>();
                        Debug.Log($"[EnemyAttack] Bridge on tag player: {(bridge != null ? "YES" : "NO")}");
                    }
                }
                
                Debug.Log($"[EnemyAttack] Found bridge on player: {(bridge != null ? "YES" : "NO")}. Target: {targetGameObj.name}, Parent: {(targetGameObj.transform.parent != null ? targetGameObj.transform.parent.name : "null")}, TagPlayer: {(playerByTag != null ? playerByTag.name : "null")}");
                
                if (bridge != null) {
                    if (enemyScript.enemyState.distance <= enemyScript.attackDistance) {
                        // Gọi particle effects trước khi gây damage
                        if (attackEffects != null) attackEffects.PlayBowAttack();
                        
                        damageStruct = D();
                        Debug.Log($"[EnemyAttack] Calling bridge.PlayerDamage. Distance: {enemyScript.enemyState.distance}, attackDistance: {enemyScript.attackDistance}");
                        bridge.PlayerDamage(damageStruct, hit);
                        return;
                    } else {
                        Debug.Log($"[EnemyAttack] Distance too far: {enemyScript.enemyState.distance} > {enemyScript.attackDistance}");
                    }
                }
            }
            Debug.LogWarning("[EnemyAttack] DamageToPlayer: No player manager or bridge found!");
            return; // No player manager or bridge found
        }

        // Original DungeonMania player system
        if(enemyScript.playerManager.playerHelth == null) return;

        if(enemyScript.enemyState.distance <= enemyScript.attackDistance){
            damageStruct = D();
            enemyScript.playerManager.playerHelth.PlayerDamage ( damageStruct, hit);
        }
    }
    public void PowerDamage(int hit){
        if(enemyScript == null) return;

        // Check if using DungeonManiaPlayerBridge (user's player system)
        if (enemyScript.playerManager == null) {
            if (enemyScript.target != null) {
                // enemyScript.target is likely the "player" (lowercase) child object
                // We need to find the actual parent Player object
                GameObject targetGameObj = enemyScript.target.gameObject;
                
                // If not found on target, try parent (actual Player)
                DungeonManiaPlayerBridge bridge = targetGameObj.GetComponent<DungeonManiaPlayerBridge>();
                if (bridge == null && targetGameObj.transform.parent != null) {
                    bridge = targetGameObj.transform.parent.GetComponent<DungeonManiaPlayerBridge>();
                }
                
                // If still not found, try finding by tag
                if (bridge == null) {
                    GameObject playerByTag = GameObject.FindGameObjectWithTag("Player");
                    if (playerByTag != null) {
                        bridge = playerByTag.GetComponent<DungeonManiaPlayerBridge>();
                    }
                }
                
                if (bridge != null) {
                    // Gọi skill effects trước khi gây damage
                    if (attackEffects != null) attackEffects.PlaySkillAttack();
                    
                    damageStruct = D();
                    damageStruct.damage += enemyScript.enemy.attack.value;
                    damageStruct.damageElemental += enemyScript.enemy.magicValue;
                    bridge.PlayerDamage(damageStruct, hit);
                    return;
                }
            }
            return;
        }

        // Original DungeonMania player system
        if(enemyScript.playerManager.playerHelth == null) return;

        damageStruct = D();
        damageStruct.damage += enemyScript.enemy.attack.value;
        damageStruct.damageElemental += enemyScript.enemy.magicValue;
        enemyScript.playerManager.playerHelth.PlayerDamage ( damageStruct, hit);
    }
    public void Bow (int hit) {
        Debug.Log("[EnemyAttack] Bow() called!");
        
        // Kiểm tra null và bounds cho bowScript
        if(enemyScript == null) {
            Debug.LogWarning("[EnemyAttack] Bow: enemyScript is null!");
            return;
        }
        if(enemyScript.bowScript == null) {
            Debug.LogWarning("[EnemyAttack] Bow: bowScript is null! Make sure 'bow' array is assigned in EnemyScript inspector.");
            return;
        }
        if(enemyScript.enemy == null) {
            Debug.LogWarning("[EnemyAttack] Bow: enemyScript.enemy is null!");
            return;
        }

        int magicIndex = enemyScript.enemy.enemyMagic;
        Debug.Log($"[EnemyAttack] Bow: magicIndex={magicIndex}, bowScript.Length={enemyScript.bowScript.Length}");
        
        if (magicIndex < 0 || magicIndex >= enemyScript.bowScript.Length) {
            Debug.LogWarning($"[EnemyAttack] Bow: magicIndex {magicIndex} out of bounds!");
            return;
        }
        if (enemyScript.bowScript[magicIndex] == null) {
            Debug.LogWarning($"[EnemyAttack] Bow: bowScript[{magicIndex}] is null! Make sure Bow script is on the particle object.");
            return;
        }
        
        // Audio manager is optional - don't require it
        // if (enemyScript.audioManager == null) return;

        // Gọi bow particle effects
        if (attackEffects != null) attackEffects.PlayBowAttack();

        damageStruct = D();
        Debug.Log($"[EnemyAttack] Bow: Calling DamageBow with damage={damageStruct.damage}");
        enemyScript.bowScript[magicIndex].DamageBow(damageStruct, hit);
        
        if (enemyScript.audioManager != null) {
            enemyScript.audioManager.CommonEnemySound(2);
        }
    }
    public void Skill(int hit){
        if(enemyScript == null || enemyScript.skillScript == null) return;

        // Gọi skill particle effects
        if (attackEffects != null) attackEffects.PlaySkillAttack();

        HitSkill();
        damageStruct = D();
        enemyScript.skillScript.DamageBow(damageStruct, hit);
    }
    void HitSkill(){
        if(enemyScript.enemyManager.generalEffects[3]){
            enemyScript.enemyManager.generalEffects[3].transform.position = new Vector3 (transform.position.x - 0.5f, transform.position.y, transform.position.z);
            enemyScript.enemyManager.generalEffects[3].Play();
        }
    }
    public Damage D () {
        if (enemyScript == null || enemyScript.enemy == null) {
            return new Damage { damage = 1, damageElemental = 0, crit = 0, isBow = false, elementalType = 0 };
        }

        int crit = 0;
        bool isBow = false;
        // Kiểm tra null cho bow
        if (enemyScript.bow != null && enemyScript.bow.Length != 0) isBow = true;

        // ƯU TIÊN: Dùng enemy.attack.value (đã được gán từ Inspector qua ApplyInspectorValues)
        // Nếu attack.value = 0 (chưa set), thì mới dùng sword
        int damage;
        if (enemyScript.enemy.attack.value > 0)
        {
            // Dùng attackDamage từ Inspector (đã được gán vào enemy.attack.value)
            damage = enemyScript.enemy.attack.value;
        }
        else
        {
            // Fallback: dùng sword damage (giá trị mặc định từ EnemyClass)
            damage = Random.Range(enemyScript.enemy.sword.damageMin, enemyScript.enemy.sword.damageMax + 1);
        }
        int damageElemental = enemyScript.enemy.magicValue;
        int checkAccuracy = Random.Range ( 1 , 101 );
        if ( checkAccuracy > enemyScript.enemy.accuracy.value ) {
            damage -= ( damage * 50 ) / 100;
            if(damage < 0) damage = 1;
        }
        else{
            int checkCrit = Random.Range(1, 101);
            if (checkCrit <= enemyScript.enemy.crit.value) crit = enemyScript.enemy.attack.value * 5;
        }
        Damage localDamageStruct = new Damage();
        localDamageStruct.damage = damage;
        localDamageStruct.damageElemental = damageElemental;
        localDamageStruct.crit = crit/2;
        localDamageStruct.isBow = isBow;
        if(enemyScript.enemyType == EnemyScript.EnemyType.demon) localDamageStruct.elementalType = Random.Range(1, 5);
        else localDamageStruct.elementalType = enemyScript.enemy.enemyMagic;
        return localDamageStruct;
    }
}
