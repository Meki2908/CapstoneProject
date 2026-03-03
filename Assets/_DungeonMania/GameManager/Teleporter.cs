using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class Teleporter : MonoBehaviour{
    public float timeDelay;
    public bool last;
    public Transform[] targetTeleport;
    public Transform player;
    PlayerManager playerManager;
    public Transform cameraSystem;
    public ParticleSystem teleFX;
    GameObject gameManager;
    AudioManager audioManager;

    void Start(){
        playerManager = player.GetComponent<PlayerManager>();
        gameManager = GameObject.Find("GameManager");
        audioManager = gameManager.GetComponent<AudioManager>();
        //gameObject.SetActive(false);
        teleFX.Stop();
    }

    IEnumerator OnTriggerEnter(Collider col){
        if (GamePlayManager.level.isSweep) {
            GamePlayManager.inTeleport = true;
            audioManager.audioSource.clip = null;
            audioManager.audioSource.loop = false;
            audioManager.audioSource.Stop();
            audioManager.CommonSceneAudio(0);
            yield return new WaitForSeconds(timeDelay);
            teleFX.Stop();
            playerManager.animator.Play("in");
            EnemyEvent.DoorClose();
            EnemyEvent.ChestClose(true);
            GamePlayManager.Ini();
            EnemyEvent.EnemyEventSystem(3);
            if (last) {
                PlayerPrefs.SetInt("QUEST_5", 2);
                player.position = targetTeleport[0].position;
                cameraSystem.position = targetTeleport[0].position;
                HeroInformation.player.gameLevel++;
                HeroInformation.player.dungeonLevel = 0;
            } else {
                HeroInformation.player.dungeonLevel++;
                int i;
                if (HeroInformation.player.gameLevel == 1) {
                    if (PlayerPrefs.GetInt("QUEST_COUNT") > 0 & PlayerPrefs.GetInt("QUEST_COUNT") < 5) {
                        //int q = PlayerPrefs.GetInt("QUEST_COUNT");
                        //q++;
                        //PlayerPrefs.SetInt("QUEST_COUNT", q);
                        PlayerPrefs.SetInt("QUEST_" + PlayerPrefs.GetInt("QUEST_COUNT").ToString(), 2);
                    }
                } else if (HeroInformation.player.dungeonLevel == 4) HeroInformation.player.dungeonLevel = 0;
                if (PlayerPrefs.GetInt("QUEST_COUNT") == 5) i = 1;
                else i = 0;
                player.position = targetTeleport[i].position;
                cameraSystem.position = targetTeleport[i].position;
            }
            SetStars.Set();
            GameController.PlayerSave();
            yield return new WaitForSeconds(1);
            GamePlayManager.inTeleport = false;
            GamePlayManager.SetNpcIndicator();
            //gameObject.SetActive(false);
        } else {
            Damage damageStruct = new Damage();
            damageStruct.damage = 4000;
            damageStruct.damageElemental = 4000;
            damageStruct.elementalType = 1;
            playerManager.playerHelth.PlayerDamage(damageStruct, 0);
        }
    }

    public void EnableTeleport(){
        teleFX.Play();
        audioManager.audioSource.clip = audioManager.commonSceneAudio[1];
        audioManager.audioSource.Play();
        audioManager.audioSource.loop = true;
    }
}
