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
    /// Reset cache khi enemy được bật lại (pooling/scene transition)
    /// </summary>
    private void OnEnable()
    {
        cachedBridge = null;
        hasSearchedBridge = false;
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
            // Luôn tìm lại bridge nếu chưa có
            if (cachedBridge == null) {
                FindAndCacheBridge();
            }
            
            if (cachedBridge != null) {
                // Tính distance trực tiếp thay vì dùng cached value (có thể stale sau scene transition)
                float actualDistance = 999f;
                if (enemyScript.target != null) {
                    actualDistance = Vector3.Distance(enemyScript.target.position, transform.position);
                }
                
                // Nếu animation đánh đang chạy = enemy đủ gần → gây damage
                // Dùng attackDistance * 1.5 để cho phép sai số
                float maxDamageRange = enemyScript.attackDistance * 1.5f;
                if (actualDistance <= maxDamageRange) {
                    if (attackEffects != null) attackEffects.PlayBowAttack();
                    
                    // === ENEMY ATTACK SOUND ===
                    var eSrc = GetComponent<AudioSource>();
                    if (enemyScript.isBoss)
                        SoundManager.PlaySound(SoundType.Boss_Attack, eSrc, 0.8f);
                    else
                        SoundManager.PlaySound(SoundType.Enemy_Attack, eSrc, 0.7f);
                    
                    damageStruct = D();
                    cachedBridge.PlayerDamage(damageStruct, hit);
                    return;
                } else {
                    Debug.Log($"[EnemyAttack] DamageToPlayer: Too far! dist={actualDistance:F1}, maxRange={maxDamageRange:F1}");
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

        // === BOSS AoE SKILL SOUND ===
        var bossSkill = GetComponent<BossMultiSkill>();
        if (bossSkill == null) bossSkill = GetComponentInParent<BossMultiSkill>();
        if (bossSkill != null)
        {
            SoundManager.PlaySound(bossSkill.GetBossAoeSound(), null, 0.9f);
        }
        else
        {
            // Sub-boss không có BossMultiSkill → dùng specificEnemyType xác định âm skill
            SoundType skillSound = enemyScript.specificEnemyType switch
            {
                EnemyScript.SpecificEnemyType.Stoneogre => SoundType.Boss_Stoneogre_EarthSlam,
                EnemyScript.SpecificEnemyType.Golem     => SoundType.Boss_Golem_WaterBlast,
                EnemyScript.SpecificEnemyType.Minotaur  => SoundType.Boss_Minotaur_EarthBlast,
                EnemyScript.SpecificEnemyType.Lich      => SoundType.Boss_Lich_WindAoe,
                EnemyScript.SpecificEnemyType.Ifrit     => SoundType.Boss_Ifrit_FireAoe,
                EnemyScript.SpecificEnemyType.Demon     => SoundType.Boss_Demon_FireBlast,
                _ => SoundType.Boss_Attack
            };
            SoundManager.PlaySound(skillSound, null, 0.9f);
        }

        // Spawn Boss Skill VFX nếu có
        SpawnSkillVFX();

        // Check if using DungeonManiaPlayerBridge (user's player system)
        if (enemyScript.playerManager == null) {
            // Luôn tìm lại bridge nếu chưa có
            if (cachedBridge == null) {
                FindAndCacheBridge();
            }
            
            if (cachedBridge != null) {
                // Gọi skill effects trước khi gây damage
                if (attackEffects != null) attackEffects.PlaySkillAttack();
                
                // Nếu có VFX skill → damage do BossSkillDamage xử lý (trigger collider)
                // Không gây damage trực tiếp ở đây nữa
                if (enemyScript.skillVfxPrefab != null) {
                    Debug.Log("[EnemyAttack] PowerDamage: VFX skill spawned, damage handled by BossSkillDamage trigger");
                    return;
                }
                
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
        if(enemyScript == null) return;

        // === BOSS SKILL 1 SOUND ===
        var bossSkill = GetComponent<BossMultiSkill>();
        if (bossSkill == null) bossSkill = GetComponentInParent<BossMultiSkill>();
        if (bossSkill != null)
        {
            SoundManager.PlaySound(bossSkill.GetBossAoeSound(), null, 0.9f);
        }
        else if (enemyScript.isBoss)
        {
            // Sub-boss không có BossMultiSkill → dùng specificEnemyType xác định âm skill
            SoundType skillSound = enemyScript.specificEnemyType switch
            {
                EnemyScript.SpecificEnemyType.Stoneogre => SoundType.Boss_Stoneogre_EarthSlam,
                EnemyScript.SpecificEnemyType.Golem     => SoundType.Boss_Golem_WaterBlast,
                EnemyScript.SpecificEnemyType.Minotaur  => SoundType.Boss_Minotaur_EarthBlast,
                EnemyScript.SpecificEnemyType.Lich      => SoundType.Boss_Lich_WindAoe,
                EnemyScript.SpecificEnemyType.Ifrit     => SoundType.Boss_Ifrit_FireAoe,
                EnemyScript.SpecificEnemyType.Demon     => SoundType.Boss_Demon_FireBlast,
                _ => SoundType.Boss_Attack
            };
            SoundManager.PlaySound(skillSound, null, 0.9f);
        }

        // Gọi skill particle effects
        if (attackEffects != null) attackEffects.PlaySkillAttack();

        // Spawn Boss Skill VFX nếu có (thay thế skill cũ)
        if (enemyScript.skillVfxPrefab != null) {
            SpawnSkillVFX();
            Debug.Log("[EnemyAttack] Skill: VFX skill spawned, damage handled by BossSkillDamage trigger");
            return;
        }

        // Fallback: dùng skill cũ (Bow + particle collision) nếu không có VFX mới
        if (enemyScript.skillScript == null) return;
        HitSkill();
        damageStruct = D();
        enemyScript.skillScript.DamageBow(damageStruct, hit);
    }
    void HitSkill(){
        if(enemyScript.enemyManager != null && 
           enemyScript.enemyManager.generalEffects != null && 
           enemyScript.enemyManager.generalEffects.Length > 3 &&
           enemyScript.enemyManager.generalEffects[3]){
            // Spawn skill effect tại vị trí PLAYER thay vì enemy
            Vector3 targetPos = enemyScript.target != null ? 
                enemyScript.target.position : transform.position;
            enemyScript.enemyManager.generalEffects[3].transform.position = 
                new Vector3(targetPos.x, targetPos.y + 0.5f, targetPos.z);
            enemyScript.enemyManager.generalEffects[3].Play();
        }
    }
    /// <summary>
    /// Spawn VFX prefab từ PixPlays tại vị trí boss
    /// Hỗ trợ 2 chế độ: AoE (360°) và Directional (cone phía trước)
    /// </summary>
    void SpawnSkillVFX(){
        if (enemyScript == null || enemyScript.skillVfxPrefab == null) return;
        
        Vector3 spawnPos;
        Quaternion spawnRot;
        
        if (enemyScript.skillIsDirectional) {
            // Directional: spawn VFX phía trước boss
            spawnPos = transform.position + transform.forward * 1.5f;
            spawnPos.y = transform.position.y + 0.2f;
            spawnRot = transform.rotation;
        } else {
            // AoE: spawn VFX tại vị trí boss
            spawnPos = transform.position;
            spawnPos.y += 0.2f;
            spawnRot = Quaternion.identity;
        }
        
        GameObject vfx = Object.Instantiate(enemyScript.skillVfxPrefab, spawnPos, spawnRot);
        
        // Setup BossSkillDamage component
        BossSkillDamage skillDmg = vfx.GetComponent<BossSkillDamage>();
        if (skillDmg == null) {
            skillDmg = vfx.AddComponent<BossSkillDamage>();
        }
        
        // Tính damage cho skill — dùng skillDamageMultiplier
        damageStruct = D();
        int skillDamage = (int)(damageStruct.damage * enemyScript.skillDamageMultiplier);
        int skillMagic = damageStruct.damageElemental + enemyScript.enemy.magicValue;
        
        if (enemyScript.skillIsDirectional) {
            // Directional: truyền hướng boss để check góc
            skillDmg.SetupDirectional(
                skillDamage, skillMagic, damageStruct.crit,
                damageStruct.elementalType,
                enemyScript.skillVfxRadius, 4f,
                transform.position, transform.forward,
                enemyScript.skillAngle
            );
        } else {
            // AoE: damage 360°
            skillDmg.Setup(
                skillDamage, skillMagic, damageStruct.crit,
                damageStruct.elementalType,
                enemyScript.skillVfxRadius, 4f
            );
        }
        
        Debug.Log($"[EnemyAttack] SpawnSkillVFX: {enemyScript.skillVfxPrefab.name} at {spawnPos}, directional={enemyScript.skillIsDirectional}, damage={skillDamage}+{skillMagic}");
    }
    public Damage D () {
        if (enemyScript == null || enemyScript.enemy == null) {
            return new Damage { damage = 1, damageElemental = 0, crit = 0, isBow = false, elementalType = 0 };
        }

        int crit = 0;
        bool isBow = false;
        // Kiểm tra null cho bow
        if (enemyScript.bow != null && enemyScript.bow.Length != 0) isBow = true;

        // Luôn dùng attackDamage từ Inspector (enemy.attack.value)
        int damage = enemyScript.enemy.attack.value;
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
