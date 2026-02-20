using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class SetEnemyRoom : MonoBehaviour{
    [HideInInspector]public Transform enemies;
    [HideInInspector]public Transform [] enemyPosition;
    Transform crystall;
    Vector3 [] crystallPosition;
    Transform chestes;
    Transform [] chestesTransform;
    public Level thisLevel;
    public delegate void Item(Vector3[] v, Transform[] v1);
    public static event Item itemPositionEvent;
    public delegate void Boss(bool b, int i);
    public static event Boss BossSlider;
    void  Awake(){
        enemies = transform.GetChild(0);
        enemyPosition = new Transform[enemies.childCount];
        thisLevel.index = transform.GetSiblingIndex();
        for (int i = 0; i < enemies.childCount; i++){
            enemyPosition[i] = enemies.GetChild(i);
        }
        if(thisLevel.index != 0 & thisLevel.levelType == Level.LevelType.commonLevel ) {
            crystall = transform.GetChild(1);
            crystallPosition = new Vector3[crystall.childCount];
            for(int i = 0; i < crystall.childCount; i++){
                crystallPosition[i] = crystall.GetChild(i).position;
            }
            chestes = transform.GetChild(2);
            chestesTransform = new Transform[chestes.childCount];
            for(int i = 0; i < chestes.childCount; i++){
                chestesTransform[i] = chestes.GetChild(i);
            }
        }
    }
    private void OnTriggerEnter ( Collider coll ) {
        GamePlayManager.inside = true;
        SelectEnemyPos.enemyTr = enemyPosition;
        if( !GamePlayManager.isLevel ){
            NewRoom();
            EnemyEvent.DoorClose();
            EnemyEvent.EnemyEventSystem(0);
        }else{
            if (GamePlayManager.level.index == thisLevel.index) EnemyEvent.EnemyEventSystem(2);
            else {
                NewRoom();
                EnemyEvent.DoorClose();
                EnemyEvent.EnemyEventSystem(3);
                EnemyEvent.EnemyEventSystem(0);
            }
        }
    }
    private void OnTriggerExit (Collider coll){
        if (GamePlayManager.level.levelType != Level.LevelType.arena) {
            GamePlayManager.inside = false;
            EnemyEvent.EnemyEventSystem(1);
        }
    }
    void NewRoom(){
        //EnemyEvent.DoorClose();
        GamePlayManager.level = thisLevel;
        if (thisLevel.index != 0 & thisLevel.levelType == Level.LevelType.commonLevel)
            itemPositionEvent(crystallPosition, chestesTransform);
        GamePlayManager.isLevel = true;
        GamePlayManager.archers = 0;
        GamePlayManager.monsteres = 0;
        GamePlayManager.lich = 0;
        GamePlayManager.boss = 0;
        GamePlayManager.demon = 0;
        GamePlayManager.leftEnemiesForDoor = 10;
        if (GamePlayManager.level.levelType == Level.LevelType.arena) {
            GamePlayManager.waveOfArena = -1;
            GamePlayManager.Arenalevel();
            BossSlider(true, 0);
        } else {
            if (thisLevel.levelType == Level.LevelType.demonLevel) BossSlider(true, 2);
            else if (thisLevel.levelType == Level.LevelType.bossLevel) {
                if (PlayerPrefs.GetInt("QUEST_COUNT") < 5 && PlayerPrefs.GetInt("QUEST_COUNT") != 0) BossSlider(true, 1);
                else BossSlider(false, 0);
            } else BossSlider(false, 0);
            MainEnemy();
        } 
    }
    void MainEnemy() {
        print(PlayerPrefs.GetInt("QUEST_COUNT").ToString());
        //switch (HeroInformation.player.dungeonLevel) {
        switch (PlayerPrefs.GetInt("QUEST_COUNT")) {
            case 0:
            switch (GamePlayManager.level.index) {//без квеста
                case 0:
                SetEnemy(new int[] { 0, 0, 0, 0, 0 });
                break;
                default:
                SetEnemy(new int[] { Random.Range(1, 4), 0, 0, 0, 0 });
                break;
            }
            break;
            case 1:
            switch (GamePlayManager.level.index) {//1 квест
                case 0:
                SetEnemy(new int[] { 0, 0, 0, 0, 0 });
                break;
                case 8:
                SetEnemy(new int[] { 0, 0, 0, 1, 0 });
                break;
                default:
                SetEnemy(new int[] { Random.Range(1, 4), 0, 0, 0, 0 });
                break;
            }
            break;
            case 2:
            switch (GamePlayManager.level.index) {//2 квест
                case 0:
                SetEnemy(new int[] { 2, 1, 0, 0, 0 });
                break;
                case 8:
                SetEnemy(new int[] { Random.Range(1, 4), 2, 0, 1, 0 });
                break;
                default:
                SetEnemy(new int[] { Random.Range(1, 4), Random.Range(1, 3), 0, 0, 0 });
                break;
            }
            break;
            case 3:
            switch (GamePlayManager.level.index) {//3 квест
                case 0:
                SetEnemy(new int[] { 3, 2, 0, 0, 0 });
                break;
                case 8:
                SetEnemy(new int[] { Random.Range(1, 4), 3, 2, 1, 0 });
                break;
                default:
                SetEnemy(new int[] { Random.Range(1, 4), Random.Range(2, 4), 1, 0, 0 });
                break;
            }
            break;
            case 4:
            switch (GamePlayManager.level.index) {// 4 квест
                case 0:
                SetEnemy(new int[] { 3, 2, 1, 0, 0 });
                break;
                case 8:
                SetEnemy(new int[] { 3, 4, 2, 1, 0 });
                break;
                default:
                SetEnemy(new int[] { 3, 3, 1, 0, 0 });
                break;
            }
            break;
            case 5:
            switch (GamePlayManager.level.index) {// 5 квест
                case 0:
                SetEnemy(new int[] { 3, 2, 1, 1, 0 });
                break;
                case 8:
                SetEnemy(new int[] { 3, 4, 2, 3, 0 });
                break;
                case 9:
                SetEnemy(new int[] { 2, 2, 1, 4, 1 });
                break;
                default:
                SetEnemy(new int[] { 3, 3, 1, 2, 0 });
                break;
            }
            break;
            default:
            SetEnemy(new int[] { 3, 4, 2, 3, 0 });
            break;
        }
    }

    public void SetEnemy(int[] enemy) {
        GamePlayManager.level.enemyType = enemy;
    }
}
