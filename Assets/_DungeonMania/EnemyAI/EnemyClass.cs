using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
public struct EnemySword{
    public int damageMin;
    public int damageMax;
    public EnemySword(int d){
        damageMin = d;
        damageMax = d + 5;
    }
}
public class EnemyClass{
    public int gold;
    public int score;
    public int experiance;
    public Characteristic attack, helth, armor, magic, crit, accuracy;
    public int mainHelth;
    public EnemySword sword;
    public float distance;
    public bool isBoss;
    public enum EnemyMagic{
        none,
        fire,
        ice,
        light,
        dead
    }
    public int enemyMagic;
    public int magicValue;
    bool canSelectMagic;
    public enum Boss{
        None,
        Ogre,
        Golem,
        Mino,
        Ifrit
    }
    void IniStat(int a, int h, int ar, int m, int c, int ac) {
        attack = new Characteristic(a);
        helth = new Characteristic(h);
        armor = new Characteristic(ar);
        magic = new Characteristic(m);
        crit = new Characteristic(c);
        accuracy = new Characteristic(ac);
    }
    void SelectMagic(){
        int[] percent = new int[10]{0,0,0,0,0,0,1,2,3,4};
        enemyMagic = percent[Random.Range(0, percent.Length)];
        if(enemyMagic != 0) magicValue = magic.value;
        else magicValue = 0;       
    }
    public EnemyClass(int i){
        EnemySet(i);
        mainHelth = helth.value;
    }
    public void EnemySet(int i){
            switch(i){
                case 0://skelet
                    IniStat(3, 70, 5, 0, 1, 50);
                    gold = Random.Range(1, 3);
                    score = 90;   
                    experiance = 100;
                    sword = new EnemySword(3);       
                    distance = 1.5f;
                    canSelectMagic = false;
                break;
                case 1://archer
                    IniStat(5, 70, 5, 0, 10, 55);
                    gold = Random.Range(1, 3);
                    score = 150;  
                    experiance = 150; 
                    sword = new EnemySword(4);       
                    distance = 5f;   
                    canSelectMagic = true;   
                break;
                case 2://monster
                    IniStat(15, 100, 15, 1, 6, 60);
                    gold = Random.Range(5, 7);
                    score = 170;   
                    experiance = 300;
                    sword = new EnemySword(5);    
                    distance = 2f;  
                    canSelectMagic = true;             
                break;
                case 3://lich
                    IniStat(20, 150, 10, 3, 15, 65);
                    gold = Random.Range(7, 9);
                    score = 250;   
                    experiance = 350;
                    sword = new EnemySword(4);          
                    distance = 5f;    
                    canSelectMagic = true;     
                break;
                case 4://boss
                    IniStat(25, 500, 10, 10, 20, 65);
                    gold = Random.Range(10, 15);
                    score = 350;   
                    experiance = 1500;
                    sword = new EnemySword(7);     
                    distance = 2.5f;
                    isBoss = true;
                break;
                case 5://demon
                    IniStat(30, 1500, 20, 15, 30, 70);
                    gold = Random.Range(25, 31);
                    score = 1000;   
                    experiance = 3000;
                    sword = new EnemySword(10);       
                    distance = 3f;
                    isBoss = true;
                break;
            }
    }
    public void UpdateEnemy(){
        int k = 0;
        if (GamePlayManager.level.levelType == Level.LevelType.arena) {
            k += GamePlayManager.waveOfArena * 5;
            score *= GamePlayManager.waveOfArena * 250;
            if (GamePlayManager.waveOfArena > 3) {
                attack.value += k;
                helth.value += helth.value * k / 100;
                armor.value += GamePlayManager.waveOfArena;
                magic.value += k;
                crit.value += k;
                accuracy.value += k;
            } else {
                armor.value = 1 + GamePlayManager.waveOfArena;
                helth.value = 20 + GamePlayManager.waveOfArena + 5;
            }
        } 
        else {
            if (HeroInformation.player.gameLevel > 1) {
                k += (HeroInformation.player.gameLevel * 5);
                int percent = k * 20 / 100;
                attack.value += k * 2;
                helth.value += helth.value * percent;
                armor.value += HeroInformation.player.gameLevel;
                magic.value += k * 2;
                crit.value += k * 2;
                accuracy.value += k + 1;
                gold *= k;
                score *= k + 300;
            } else {
                if (HeroInformation.player.dungeonLevel != 0) {
                    k = HeroInformation.player.dungeonLevel;
                    if (GamePlayManager.level.levelType == Level.LevelType.bossLevel || GamePlayManager.level.levelType == Level.LevelType.demonLevel) {
                        if (isBoss) {
                            attack.value += k;
                            helth.value += (helth.value * (PlayerPrefs.GetInt("QUEST_COUNT") * 10)) / 100;
                            armor.value += PlayerPrefs.GetInt("QUEST_COUNT");
                            magic.value += k;
                            crit.value += k;
                            accuracy.value += k;
                        }
                        else PercentUp(k + 5);
                    } else PercentUp(k);


                    gold *= k * HeroInformation.player.gameLevel;
                    score *= k + 200;
                } else {
                    k = GamePlayManager.level.currentLevel;
                    if (!isBoss) {
                        attack.value += k;
                        helth.value = 20 + k * 5;
                        armor.value = k;
                        magic.value = k;
                        crit.value = k;
                        accuracy.value += k;
                    } else {
                        armor.value = 7;
                        //helth.value = 500;
                    }
                    gold *= k;
                    score *= k + 100;
                }
            }
        }
        if (canSelectMagic && magic.value >= 5) SelectMagic();
    }

    void PercentUp(int k) {
        int percent = k * 20 / 100;
        attack.value += k + 1;
        helth.value += helth.value * percent;
        armor.value += k;
        magic.value += k + 1;
        crit.value += k + 1;
        accuracy.value += k + 1;
    }

}

