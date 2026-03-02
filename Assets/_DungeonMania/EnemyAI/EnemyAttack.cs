using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EnemyAttack : MonoBehaviour {
    EnemyScript enemyScript;
    Damage damageStruct;
    EnemyAttackEffects attackEffects;
    
    // Cache bridge reference để không phải tìm lại mỗi lần attack
    private DungeonManiaPlayerBridge cachedBridge;
    private bool hasSearchedBridge = false;
    
    private void Start(){
        enemyScript = GetComponent<EnemyScript> ();
        // Tìm EnemyAttackEffects
        attackEffects = GetComponent<EnemyAttackEffects>();
        if (attackEffects == null)
            attackEffects = GetComponentInChildren<EnemyAttackEffects>();
        
        // Cache bridge reference ngay từ Start
        FindAndCacheBridge();
    }
    
    /// <summary>
    /// Tìm và cache DungeonManiaPlayerBridge
    /// </summary>
    private void FindAndCacheBridge()
    {
        if (cachedBridge != null) return;
        
        // Tìm trên target
        if (enemyScript != null && enemyScript.target != null) {
            cachedBridge = enemyScript.target.GetComponent<DungeonManiaPlayerBridge>();
            
            // Thử parent nếu target là child
            if (cachedBridge == null && enemyScript.target.parent != null) {
                cachedBridge = enemyScript.target.parent.GetComponent<DungeonManiaPlayerBridge>();
            }
        }
        
        // Fallback: tìm bằng tag
        if (cachedBridge == null) {
            GameObject playerByTag = GameObject.FindGameObjectWithTag("Player");
            if (playerByTag != null) {
                cachedBridge = playerByTag.GetComponent<DungeonManiaPlayerBridge>();
            }
        }
        
        hasSearchedBridge = true;
        
        if (cachedBridge != null) {
            Debug.Log($"[EnemyAttack] Cached DungeonManiaPlayerBridge on {cachedBridge.gameObject.name}");
        }
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

        // Check if using DungeonManiaPlayerBridge (user's player system)
        if (enemyScript.playerManager == null) {
            // Dùng cached bridge (tìm lại nếu chưa tìm)
            if (!hasSearchedBridge || cachedBridge == null) {
                FindAndCacheBridge();
            }
            
            if (cachedBridge != null) {
                if (enemyScript.enemyState.distance <= enemyScript.attackDistance) {
                    // Gọi particle effects trước khi gây damage
                    if (attackEffects != null) attackEffects.PlayBowAttack();
                    
                    damageStruct = D();
                    cachedBridge.PlayerDamage(damageStruct, hit);
                    return;
                }
            } else {
                Debug.LogWarning("[EnemyAttack] DamageToPlayer: No player manager or bridge found!");
            }
            return;
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
            // Dùng cached bridge
            if (!hasSearchedBridge || cachedBridge == null) {
                FindAndCacheBridge();
            }
            
            if (cachedBridge != null) {
                // Gọi skill effects trước khi gây damage
                if (attackEffects != null) attackEffects.PlaySkillAttack();
                
                damageStruct = D();
                damageStruct.damage += enemyScript.enemy.attack.value;
                damageStruct.damageElemental += enemyScript.enemy.magicValue;
                cachedBridge.PlayerDamage(damageStruct, hit);
                return;
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
