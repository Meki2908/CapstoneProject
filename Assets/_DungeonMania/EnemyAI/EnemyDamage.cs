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
        gameManager = GameObject.Find("GameManager");
        gamePlayManager = gameManager.GetComponent<GamePlayManager>();
        pS = new ParticleSystem();
    }
    public void Damage(Damage d) {
        if(enemyScript.alive){
            enemyScript.hit = true;
            Hit(d);
        }
    }
    void  Hit ( Damage damage ) {
        damage.damage -= enemyScript.enemy.armor.value;
        int damageFull = damage.damage + damage.damageElemental + damage.crit;
        if (damageFull < 1) damageFull = 1;
        if (damage.isSpell) enemyScript.animator.SetBool("knock", true);
        else {
            if(enemyScript.enemyType == EnemyScript.EnemyType.skelet || enemyScript.enemyType == EnemyScript.EnemyType.archer) enemyScript.animator.SetBool("hit", true);
        } 
        if (damage.spellID > 0){
            pS = enemyScript.enemyManager.spellHit[damage.spellID - 1];
        }else{
            if ( damage.damageElemental <= 0 ){
                pS = enemyScript.enemyManager.hitParticle[0];
            }
            else{
                int i = damage.elementalType;
                pS = enemyScript.enemyManager.magicHit[i];
                enemyScript.audioManager.SwordMagicDamage(i);
            }
        }
        pS.transform.position = transform.position + (Vector3.up * 1.5f);
        pS.Play();
        enemyScript.audioManager.EnemyDamage();
        enemyScript.enemy.helth.value -= damageFull;
        if (GamePlayManager.level.levelType == Level.LevelType.bossLevel) {
            if (enemyScript.enemyType == EnemyScript.EnemyType.boss)
                StartCoroutine(GamePlayManager.UpdateValue((float)enemyScript.enemy.helth.value / (float)enemyScript.enemy.mainHelth));
        }
        else if (GamePlayManager.level.levelType == Level.LevelType.demonLevel) {
            if (enemyScript.enemyType == EnemyScript.EnemyType.demon)
                StartCoroutine(GamePlayManager.UpdateValue((float)enemyScript.enemy.helth.value / (float)enemyScript.enemy.mainHelth));
        }
        if ( enemyScript.enemy.helth.value <= 0 ){
            enemyScript.playerManager.playerBar.CheckExperience(enemyScript.enemy.experiance);
            if (GamePlayManager.level.levelType == Level.LevelType.arena) StartCoroutine(GamePlayManager.UpdateValue((float)(GamePlayManager.enemysOfWave - GamePlayManager.leftEnemiesArena) / (float)GamePlayManager.enemysOfWave));
            StartCoroutine(Death());
        } 
    }
    public IEnumerator Death(){
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
                    gamePlayManager.ActivateTeleport(0);
                } 
                enemyScript.RunWinAudio(1);
                gamePlayManager.SetBossSlider(false, 0);
            }
                enemyScript.animator.SetBool("hit", false);
                enemyScript.animator.SetBool("knock", false);
                enemyScript.animator.SetBool("run", false);
                enemyScript.animator.Play("dead");
                int i = number - 6;
                yield return new WaitForSeconds(4f);
                enemyScript.enemyManager.bossExpl[i].transform.position = transform.position;
                enemyScript.enemyManager.bossExpl[i].Play();
                Chest(1);
            break;
            case 5:
                DemonBonus(3);
                GamePlayManager.demon --;
                GamePlayManager.level.isSweep = true;
                enemyScript.RunWinAudio(2);
                enemyScript.animator.SetBool("hit", false);
                enemyScript.animator.SetBool("knock", false);
                enemyScript.animator.SetBool("run", false);
                enemyScript.animator.Play("dead");
                gamePlayManager.ActivateTeleport(1);
                yield return new WaitForSeconds(4f);
                enemyScript.enemyManager.bossExpl[3].transform.position = transform.position;
                enemyScript.enemyManager.bossExpl[3].Play();
            gamePlayManager.SetBossSlider(false, 0);
            break;
        }
        gameObject.SetActive(false);
        switch (GamePlayManager.level.levelType) {
            case Level.LevelType.arena:
            GamePlayManager.leftEnemiesArena++;
            //gamePlayManager.UpdateArenaSlider();
            if (GamePlayManager.leftEnemiesArena >= GamePlayManager.enemysOfWave) {
                //GamePlayManager.Arenalevel();
                //волна закончилась выводим сообщение ждем реакции игрока
                gamePlayManager.EndOfArenaWave();
            } else {
                if(GamePlayManager.checkAreneEnemys < GamePlayManager.enemysOfWave) enemyScript.randomEnemyScript.Enable();
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
            if (!GamePlayManager.level.isSweep) enemyScript.randomEnemyScript.Enable();
            break;
        }
        //if (!GamePlayManager.level.isSweep) enemyScript.randomEnemyScript.Enable();
    }
    void CopyTransformsRecurse(Transform scr, Transform dst){
        dst.position = scr.position;
        dst.rotation = scr.rotation;
        foreach (Transform child in dst){
            Transform curSrc = scr.Find(child.name);
            if (curSrc) CopyTransformsRecurse(curSrc, child);
        }
    }
    void Chest(int i){
        if (GamePlayManager.level.levelType != Level.LevelType.arena) {
            enemyScript.enemyManager.chestBoss[i].SetActive(true);
            enemyScript.enemyManager.chestBoss[i].transform.position = transform.position;
        }
    }
    void Gold(){
        int gold;
        int score;
        int randGold = Random.Range(1, 101);
        if (randGold <= (50 + (HeroInformation.player.lucky.value * 2))){
            gold = enemyScript.enemy.gold + HeroInformation.player.playerLevel;
            gold += ((gold * HeroInformation.player.miner.value) / 100) + HeroInformation.player.lucky.value;
            //if (GamePlayManager.level.levelType == Level.LevelType.arena) gold *= 10;
            enemyScript.enemyManager.generalEffects[0].transform.position = transform.position + (Vector3.up * 2);
            enemyScript.enemyManager.generalEffects[0].Play();
            enemyScript.audioManager.CommonEnemySound(0);
        } else gold = 0;
        score = enemyScript.enemy.score + (enemyScript.enemy.score * HeroInformation.player.medal.value/100);
        StartCoroutine(enemyScript.heroInformation.UpdateStat(gold, score));
    }
    void SkullBoneFX(){
         enemyScript.enemyManager.generalEffects[1].transform.position = transform.position + (Vector3.up * 2f);
         enemyScript.enemyManager.generalEffects[2].transform.position = transform.position + (Vector3.up * 1.5f);
         enemyScript.enemyManager.generalEffects[1].Play();
         enemyScript.enemyManager.generalEffects[2].Play();
    }
    void DemonBonus(int i){
        enemyScript.playerManager.PlayerWin(i);
    }
}
