using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RandomEnemy : MonoBehaviour{
    int childNumber;
    public ParticleSystem pentagram;
    int index;
    public GameObject[] enemys;
    public bool firstEnable;
    Vector3 newPos;
    private void Awake(){
        childNumber = transform.GetSiblingIndex ();
    }
    private void OnEnable(){
        EnemyEvent.EnableEvent += Enable;
        EnemyEvent.DisableEvent += Disable;
    }
    private void OnDisable(){
        EnemyEvent.EnableEvent -= Enable;
        EnemyEvent.DisableEvent -= Disable;
    }
    public void Enable (){
        if (GamePlayManager.level.levelType == Level.LevelType.arena) {
            GamePlayManager.checkAreneEnemys++;
        }
        EnemyChoose( GamePlayManager.level.enemyType[0],
            GamePlayManager.level.enemyType[1],
            GamePlayManager.level.enemyType[2],
            GamePlayManager.level.enemyType[3],
            GamePlayManager.level.enemyType[4] );
    }
    void EnemyChoose(int archers, int monsters, int lich, int boss, int demon) {
        if (GamePlayManager.demon < demon) {
            index = 10;
            GamePlayManager.demon++;
        } else if (GamePlayManager.boss < boss) {
            if (GamePlayManager.level.levelType == Level.LevelType.arena || PlayerPrefs.GetInt("QUEST_COUNT") >= 5) index = Random.Range(6, 10);
            else index = PlayerPrefs.GetInt("QUEST_COUNT") + 5; //index = HeroInformation.player.dungeonLevel + 6;
            GamePlayManager.boss++;
        } else if (GamePlayManager.lich < lich) {
            index = 5;
            GamePlayManager.lich++;
        } else if (GamePlayManager.monsteres < monsters) {
            if (GamePlayManager.level.levelType == Level.LevelType.arena) index = Random.Range(2, 5);
            else {
                switch (PlayerPrefs.GetInt("QUEST_COUNT")) { //HeroInformation.player.dungeonLevel) {
                    case 0:
                    break;
                    case 1:
                    index = 2;
                    break;
                    case 2:
                    index = Random.Range(2, 4);
                    break;
                    default:
                    index = Random.Range(2, 5);
                    break;
                }
            }
            GamePlayManager.monsteres++;
        } else if (GamePlayManager.archers < archers) {
            index = 1;
            GamePlayManager.archers++;
        } else index = 0;
        SetEnemy();
    }
    void Disable(){
        enemys[index].SetActive(false);
        //Enable();
    }
    void SetEnemy(){
        newPos = SelectEnemyPos.SelectNewPos(childNumber);
        pentagram.transform.position = newPos;
        pentagram.Play();
        enemys [index].transform.position = newPos;
        enemys[index].SetActive(true);
    }
}
