using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fire : MonoBehaviour{
    GameObject player;
    PlayerManager playerManager;
    bool isFire;
    Damage damageStruct; 
    GameObject gameManager;
    AudioManager audioManager;
    void Start(){
        player = GameObject.Find("player");
        playerManager = player.GetComponent<PlayerManager>();
        gameManager = GameObject.Find("GameManager");
        audioManager = gameManager.GetComponent<AudioManager>();
    }
    void OnTriggerStay(){
        if (!GamePlayManager.inTeleport) {
            if (!isFire) StartCoroutine(FireDamage());
        }
    }
    IEnumerator FireDamage(){
        damageStruct = new Damage();
        int k = HeroInformation.player.gameLevel + HeroInformation.player.playerLevel + HeroInformation.player.dungeonLevel;
        damageStruct.damage = 4 * k;
        damageStruct.damageElemental = 4 * k;
        damageStruct.elementalType = 1;
        playerManager.playerHelth.PlayerDamage(damageStruct, 0);
        audioManager.SwordMagicDamage(0);
        isFire = true;
        yield return new WaitForSeconds(0.25f);
        isFire = false;
    }
}
