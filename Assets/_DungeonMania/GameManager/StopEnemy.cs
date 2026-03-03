using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StopEnemy : MonoBehaviour{
    public bool isEnableTrigger;
    public delegate void StopEnemyDelegate ();
    public static event StopEnemyDelegate StopEnemyEvent;
    public static event StopEnemyDelegate GoEnemyEvents;
    private void OnTriggerEnter ( Collider other ) {
        StopEnemyEvent ();
    }
    private void OnTriggerExit ( Collider other ) {
        GoEnemyEvents ();
    }
}
