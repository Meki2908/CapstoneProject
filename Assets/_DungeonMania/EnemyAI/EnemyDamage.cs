using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EnemyDamage : MonoBehaviour{
    int number;
    EnemyScript enemyScript;
    string animName;
    GameObject gameManager;
    GamePlayManager gamePlayManager;
    ParticleSystem pS;
    private void Start(){
        enemyScript = GetComponent<EnemyScript>();
        number = transform.GetSiblingIndex();

        // Sửa: Kiểm tra null khi tìm GameManager
        gameManager = GameObject.Find("GameManager");
        if (gameManager != null) {
            gamePlayManager = gameManager.GetComponent<GamePlayManager>();
        } else {
            Debug.LogWarning("[EnemyDamage] GameManager not found!");
            gamePlayManager = null;
        }

        // Khởi tạo particle system
        if (GetComponent<ParticleSystem>() != null) {
            pS = GetComponent<ParticleSystem>();
        }
    }
    public void Damage(Damage d) {
        if(enemyScript == null || !enemyScript.alive) return;
        
        // Shield check — boss bất tử khi có shield
        if (enemyScript.bossMultiSkill != null && enemyScript.bossMultiSkill.ShouldBlockDamage())
        {
            return; // Block toàn bộ damage
        }
        
        Hit(d);
    }
    void  Hit ( Damage damage ) {
        if (enemyScript == null || enemyScript.enemy == null) return;

        damage.damage -= enemyScript.enemy.armor.value;
        int damageFull = damage.damage + damage.damageElemental + damage.crit;
        if (damageFull < 1) damageFull = 1;
        
        // === HIT STAGGER ===
        if (damage.isSpell)
        {
            // Phép → knock cho TẤT CẢ (kể cả boss)
            enemyScript.animator.SetBool("knock", true);
            StartCoroutine(HitStagger(0.6f));
        }
        else if (!enemyScript.isBoss)
        {
            // Đánh thường → chỉ enemy thường bị hit, BOSS KHÔNG bị
            enemyScript.animator.SetBool("hit", true);
            StartCoroutine(HitStagger(0.4f));
        }

        // Kiểm tra null và bounds cho particle arrays
        if (enemyScript.enemyManager != null) {
            if (damage.spellID > 0 && enemyScript.enemyManager.spellHit != null && damage.spellID - 1 < enemyScript.enemyManager.spellHit.Length){
                pS = enemyScript.enemyManager.spellHit[damage.spellID - 1];
            }else{
                if ( damage.damageElemental <= 0 ){
                    if (enemyScript.enemyManager.hitParticle != null && enemyScript.enemyManager.hitParticle.Length > 0)
                        pS = enemyScript.enemyManager.hitParticle[0];
                }
                else{
                    int i = damage.elementalType;
                    if (enemyScript.enemyManager.magicHit != null && i >= 0 && i < enemyScript.enemyManager.magicHit.Length)
                        pS = enemyScript.enemyManager.magicHit[i];
                    if (enemyScript.audioManager != null) enemyScript.audioManager.SwordMagicDamage(i);
                }
            }
        }

        if (pS != null) {
            pS.transform.position = transform.position + (Vector3.up * 1.5f);
            pS.Play();
        }

        if (enemyScript.audioManager != null) enemyScript.audioManager.EnemyDamage();

        enemyScript.enemy.helth.value -= damageFull;
        if (GamePlayManager.level.levelType == Level.LevelType.bossLevel) {
            if (enemyScript.enemyType == EnemyScript.EnemyType.boss && gamePlayManager != null)
                StartCoroutine(GamePlayManager.UpdateValue((float)enemyScript.enemy.helth.value / (float)enemyScript.enemy.mainHelth));
        }
        else if (GamePlayManager.level.levelType == Level.LevelType.demonLevel) {
            if (enemyScript.enemyType == EnemyScript.EnemyType.demon && gamePlayManager != null)
                StartCoroutine(GamePlayManager.UpdateValue((float)enemyScript.enemy.helth.value / (float)enemyScript.enemy.mainHelth));
        }
        if ( enemyScript.enemy.helth.value <= 0 ){
            if (enemyScript.playerManager != null && enemyScript.playerManager.playerBar != null)
                enemyScript.playerManager.playerBar.CheckExperience(enemyScript.enemy.experiance);
            if (GamePlayManager.level.levelType == Level.LevelType.arena && gamePlayManager != null)
                StartCoroutine(GamePlayManager.UpdateValue((float)(GamePlayManager.enemysOfWave - GamePlayManager.leftEnemiesArena) / (float)GamePlayManager.enemysOfWave));
            StartCoroutine(Death());
        }
    }
    
    /// <summary>
    /// Auto-reset hit/knock sau thời gian stagger — tránh enemy bị đơ vĩnh viễn
    /// </summary>
    IEnumerator HitStagger(float duration)
    {
        yield return new WaitForSeconds(duration);
        
        if (enemyScript == null || !enemyScript.alive) yield break;
        
        // Reset animation bools
        enemyScript.animator.SetBool("hit", false);
        enemyScript.animator.SetBool("knock", false);
        enemyScript.hit = false;
        
        // Resume di chuyển
        if (enemyScript.navMeshAgent != null && enemyScript.navMeshAgent.isOnNavMesh)
        {
            enemyScript.navMeshAgent.isStopped = false;
        }
    }
    
    private bool isDying = false; // Tránh gọi Death() 2 lần
    
    public IEnumerator Death(){
        if (enemyScript == null) yield break;
        if (isDying) yield break; // Đã đang chết rồi
        isDying = true;

        enemyScript.alive = false;
        
        // === DỪNG di chuyển ngay ===
        if (enemyScript.navMeshAgent != null && enemyScript.navMeshAgent.isOnNavMesh)
        {
            enemyScript.navMeshAgent.isStopped = true;
            enemyScript.navMeshAgent.velocity = Vector3.zero;
        }
        
        // === CHẠY DEATH ANIMATION cho TẤT CẢ enemy ===
        if (enemyScript.animator != null)
        {
            enemyScript.animator.SetBool("hit", false);
            enemyScript.animator.SetBool("knock", false);
            enemyScript.animator.SetBool("run", false);
            enemyScript.animator.SetBool("attack", false);
            enemyScript.animator.SetTrigger("dead");
        }
        
        // === DEATH SOUND ===
        if (enemyScript.isBoss)
            SoundManager.PlaySound(SoundType.Boss_Die, null, 1f);
        else
            SoundManager.PlaySound(SoundType.Enemy_Die, null, 0.8f);
        
        switch((int)enemyScript.enemyType){
            case 0: // Skelet
                Gold();
                yield return new WaitForSeconds(5f);
                SkullBoneFX();
            break;
            case 1: // Archer
                GamePlayManager.archers --;
                Gold();
                yield return new WaitForSeconds(5f);
                SkullBoneFX();
            break;
            case 2: // Monster
                GamePlayManager.monsteres --;
                Gold();
                yield return new WaitForSeconds(5f);
                SkullBoneFX();
                Chest(0);
            break;
            case 3: // Lich
                GamePlayManager.lich --;
                Gold();
                yield return new WaitForSeconds(5f);
                SkullBoneFX();
                Chest(0);
            break;
            case 4: // Boss
            GamePlayManager.boss --;
                if (GamePlayManager.level.levelType == Level.LevelType.bossLevel) {
                if (PlayerPrefs.GetInt("QUEST_COUNT") < 5) {
                    GamePlayManager.level.isSweep = true;
                    DemonBonus(2);
                    if (gamePlayManager != null) gamePlayManager.ActivateTeleport(0);
                }
                enemyScript.RunWinAudio(1);
                if (gamePlayManager != null) gamePlayManager.SetBossSlider(false, 0);
            }
                // Sửa: Kiểm tra bounds cho bossExpl
                int i = number - 6;
                if (enemyScript.enemyManager != null && enemyScript.enemyManager.bossExpl != null &&
                    i >= 0 && i < enemyScript.enemyManager.bossExpl.Length) {
                    yield return new WaitForSeconds(4f);
                    enemyScript.enemyManager.bossExpl[i].transform.position = transform.position;
                    enemyScript.enemyManager.bossExpl[i].Play();
                    yield return new WaitForSeconds(1f);
                } else {
                    yield return new WaitForSeconds(5f);
                }
                Chest(1);
            break;
            case 5: // Demon
                DemonBonus(3);
                GamePlayManager.demon --;
                GamePlayManager.level.isSweep = true;
                enemyScript.RunWinAudio(2);
                if (gamePlayManager != null) gamePlayManager.ActivateTeleport(1);
                if (enemyScript.enemyManager != null && enemyScript.enemyManager.bossExpl != null && enemyScript.enemyManager.bossExpl.Length > 3) {
                    yield return new WaitForSeconds(4f);
                    enemyScript.enemyManager.bossExpl[3].transform.position = transform.position;
                    enemyScript.enemyManager.bossExpl[3].Play();
                    yield return new WaitForSeconds(1f);
                } else {
                    yield return new WaitForSeconds(5f);
                }
                if (gamePlayManager != null) gamePlayManager.SetBossSlider(false, 0);
            break;
            default: // Stoneogre, Golem, Minotaur, Ifrit — boss phụ
                Gold();
                yield return new WaitForSeconds(5f);
                SkullBoneFX();
                Chest(0);
            break;
        }
        gameObject.SetActive(false);

        // Kiểm tra null trước khi gọi
        if (gamePlayManager == null) yield break;

        switch (GamePlayManager.level.levelType) {
            case Level.LevelType.arena:
            GamePlayManager.leftEnemiesArena++;
            if (GamePlayManager.leftEnemiesArena >= GamePlayManager.enemysOfWave) {
                gamePlayManager.EndOfArenaWave();
            } else {
                if(GamePlayManager.checkAreneEnemys < GamePlayManager.enemysOfWave && enemyScript.randomEnemyScript != null)
                    enemyScript.randomEnemyScript.Enable();
            }
            break;
            case Level.LevelType.commonLevel:
            GamePlayManager.leftEnemiesForDoor--;
            if (GamePlayManager.leftEnemiesForDoor <= 0) GamePlayManager.level.isSweep = true;
            break;
            case Level.LevelType.bossLevel:
            if (PlayerPrefs.GetInt("QUEST_COUNT") >= 5) {
                GamePlayManager.leftEnemiesForDoor--;
                if (GamePlayManager.leftEnemiesForDoor <= 0) {
                    gamePlayManager.ActivateTeleport(0);
                    GamePlayManager.level.isSweep = true;
                }
            }
            break;
            default:
            if (!GamePlayManager.level.isSweep && enemyScript.randomEnemyScript != null)
                enemyScript.randomEnemyScript.Enable();
            break;
        }
    }
    void CopyTransformsRecurse(Transform scr, Transform dst){
        if (scr == null || dst == null) return;
        dst.position = scr.position;
        dst.rotation = scr.rotation;
        foreach (Transform child in dst){
            Transform curSrc = scr.Find(child.name);
            if (curSrc) CopyTransformsRecurse(curSrc, child);
        }
    }
    void Chest(int i){
        if (GamePlayManager.level.levelType != Level.LevelType.arena && enemyScript.enemyManager != null) {
            if (enemyScript.enemyManager.chestBoss != null && i >= 0 && i < enemyScript.enemyManager.chestBoss.Length) {
                enemyScript.enemyManager.chestBoss[i].SetActive(true);
                enemyScript.enemyManager.chestBoss[i].transform.position = transform.position;
            }
        }
    }
    void Gold(){
        if (enemyScript == null || enemyScript.enemy == null || HeroInformation.player == null) return;

        int gold;
        int score;
        int randGold = Random.Range(1, 101);
        if (randGold <= (50 + (HeroInformation.player.lucky.value * 2))){
            gold = enemyScript.enemy.gold + HeroInformation.player.playerLevel;
            gold += ((gold * HeroInformation.player.miner.value) / 100) + HeroInformation.player.lucky.value;

            if (enemyScript.enemyManager != null && enemyScript.enemyManager.generalEffects != null &&
                enemyScript.enemyManager.generalEffects.Length > 0 && enemyScript.enemyManager.generalEffects[0] != null) {
                enemyScript.enemyManager.generalEffects[0].transform.position = transform.position + (Vector3.up * 2);
                enemyScript.enemyManager.generalEffects[0].Play();
            }
            if (enemyScript.audioManager != null) enemyScript.audioManager.CommonEnemySound(0);
        } else gold = 0;
        score = enemyScript.enemy.score + (enemyScript.enemy.score * HeroInformation.player.medal.value/100);
        if (enemyScript.heroInformation != null)
            StartCoroutine(enemyScript.heroInformation.UpdateStat(gold, score));
    }
    void SkullBoneFX(){
        if (enemyScript == null || enemyScript.enemyManager == null || enemyScript.enemyManager.generalEffects == null) return;

        if (enemyScript.enemyManager.generalEffects.Length > 1 && enemyScript.enemyManager.generalEffects[1] != null)
            enemyScript.enemyManager.generalEffects[1].transform.position = transform.position + (Vector3.up * 2f);
        if (enemyScript.enemyManager.generalEffects.Length > 2 && enemyScript.enemyManager.generalEffects[2] != null)
            enemyScript.enemyManager.generalEffects[2].transform.position = transform.position + (Vector3.up * 1.5f);

        if (enemyScript.enemyManager.generalEffects.Length > 1 && enemyScript.enemyManager.generalEffects[1] != null)
            enemyScript.enemyManager.generalEffects[1].Play();
        if (enemyScript.enemyManager.generalEffects.Length > 2 && enemyScript.enemyManager.generalEffects[2] != null)
            enemyScript.enemyManager.generalEffects[2].Play();
    }
    void DemonBonus(int i){
        if (enemyScript == null || enemyScript.playerManager == null) return;
        enemyScript.playerManager.PlayerWin(i);
    }
}
