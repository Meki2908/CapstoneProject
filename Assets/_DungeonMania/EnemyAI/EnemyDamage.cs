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
        enemyScript.hit = true;
        Hit(d);
    }
    void  Hit ( Damage damage ) {
        if (enemyScript == null || enemyScript.enemy == null) return;

        damage.damage -= enemyScript.enemy.armor.value;
        int damageFull = damage.damage + damage.damageElemental + damage.crit;
        if (damageFull < 1) damageFull = 1;
        if (damage.isSpell) enemyScript.animator.SetBool("knock", true);
        else {
            if(enemyScript.enemyType == EnemyScript.EnemyType.skelet || enemyScript.enemyType == EnemyScript.EnemyType.archer) enemyScript.animator.SetBool("hit", true);
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
    public IEnumerator Death(){
        if (enemyScript == null) yield break;

        enemyScript.alive = false;
        switch((int)enemyScript.enemyType){
            case 0:
                Gold();
                yield return new WaitForSeconds(0.25f);
                SkullBoneFX();
            break;
            case 1:
                GamePlayManager.archers --;
                Gold();
                yield return new WaitForSeconds(0.25f);
                SkullBoneFX() ;
            break;
            case 2:
                GamePlayManager.monsteres --;
                Gold();
                yield return new WaitForSeconds(0.25f);
                SkullBoneFX();
                Chest(0);
            break;
            case 3:
                GamePlayManager.lich --;
                Gold();
                yield return new WaitForSeconds(0.25f);
                SkullBoneFX();
                Chest(0);
            break;
            case 4:
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
                if (enemyScript.animator != null) {
                    enemyScript.animator.SetBool("hit", false);
                    enemyScript.animator.SetBool("knock", false);
                    enemyScript.animator.SetBool("run", false);
                    enemyScript.animator.Play("dead");
                }
                // Sửa: Kiểm tra bounds cho bossExpl
                int i = number - 6;
                if (enemyScript.enemyManager != null && enemyScript.enemyManager.bossExpl != null &&
                    i >= 0 && i < enemyScript.enemyManager.bossExpl.Length) {
                    yield return new WaitForSeconds(4f);
                    enemyScript.enemyManager.bossExpl[i].transform.position = transform.position;
                    enemyScript.enemyManager.bossExpl[i].Play();
                } else {
                    yield return new WaitForSeconds(4f);
                }
                Chest(1);
            break;
            case 5:
                DemonBonus(3);
                GamePlayManager.demon --;
                GamePlayManager.level.isSweep = true;
                enemyScript.RunWinAudio(2);
                if (enemyScript.animator != null) {
                    enemyScript.animator.SetBool("hit", false);
                    enemyScript.animator.SetBool("knock", false);
                    enemyScript.animator.SetBool("run", false);
                    enemyScript.animator.Play("dead");
                }
                if (gamePlayManager != null) gamePlayManager.ActivateTeleport(1);
                if (enemyScript.enemyManager != null && enemyScript.enemyManager.bossExpl != null && enemyScript.enemyManager.bossExpl.Length > 3) {
                    yield return new WaitForSeconds(4f);
                    enemyScript.enemyManager.bossExpl[3].transform.position = transform.position;
                    enemyScript.enemyManager.bossExpl[3].Play();
                } else {
                    yield return new WaitForSeconds(4f);
                }
                if (gamePlayManager != null) gamePlayManager.SetBossSlider(false, 0);
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
