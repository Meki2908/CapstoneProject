using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EnemyAttack : MonoBehaviour {
    EnemyScript enemyScript;
    Damage damageStruct;
    private void Start(){
        enemyScript = GetComponent<EnemyScript> ();
    }
    public void StartAction(string anim){
        if(HeroInformation.alive){
            if(!enemyScript.attack){
                enemyScript.attack = true;
                if(enemyScript.enemyState.isStop) enemyScript.animator.Play(anim);
                enemyScript.attack = false;
            }
        }
    }
    public void StopAttack(){
        enemyScript.attack = false;
    }
    public void DamageToPlayer (int hit) {
        if(enemyScript.enemyState.distance <= enemyScript.attackDistance){
        damageStruct = D();
        enemyScript.playerManager.playerHelth.PlayerDamage ( damageStruct, hit);
        }
    }
    public void PowerDamage(int hit){
        damageStruct = D();
        damageStruct.damage += enemyScript.enemy.attack.value;
        damageStruct.damageElemental += enemyScript.enemy.magicValue;
        enemyScript.playerManager.playerHelth.PlayerDamage ( damageStruct, hit);
    }
    public void Bow (int hit) {
        damageStruct = D();
        enemyScript.bowScript[enemyScript.enemy.enemyMagic].DamageBow(damageStruct, hit);
        enemyScript.audioManager.CommonEnemySound(2);
    }
    public void Skill(int hit){
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
        int crit = 0;
        bool isBow;
        if ( enemyScript.bow.Length != 0 ) isBow = true;
        else isBow = false;
        int damage = Random.Range ( enemyScript.enemy.sword.damageMin , enemyScript.enemy.sword.damageMax + 1 );
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
