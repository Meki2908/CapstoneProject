using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyEvent : MonoBehaviour{
    public delegate void EnemyDelegate();
    public static event EnemyDelegate EnableEvent;
    public static event EnemyDelegate WaitEvent;
    public static event EnemyDelegate AttackEvent;
    public static event EnemyDelegate DisableEvent;
    public static event EnemyDelegate DamageEvent;
    public static event EnemyDelegate DeadEvent;
    public static event EnemyDelegate LeftEnemies;
    
    public delegate void ChestDelegate(bool b);
    public delegate void DoorDelegate();
    public static event ChestDelegate CloseChestes;
    public static event DoorDelegate CloseDoor;
    public static void ChestClose(bool b){
        CloseChestes(b);
    }
    public static void DoorClose(){
        CloseDoor();
    }
    public static void LeftEnemy(){
        if(LeftEnemies != null)LeftEnemies();
    }
    public static void EnemyEventSystem( int e ){
        switch (e){
            case 0:
            if(EnableEvent != null)EnableEvent();
            break;
            case 1:
            if(WaitEvent != null) WaitEvent();
            break;
            case 2:
            if(AttackEvent != null)AttackEvent();
            break;
            case 3:
            if(DisableEvent != null)DisableEvent();
            break;
            case 4:
            if(DamageEvent != null)DamageEvent();
            break;
            case 5:
            if(DeadEvent != null)DeadEvent();
            break;
        }
    }
}
