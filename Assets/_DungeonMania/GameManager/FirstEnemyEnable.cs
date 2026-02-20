using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstEnemyEnable : MonoBehaviour{
    RandomEnemy re;
    void Start(){
        foreach(Transform tr in transform){
            re = tr.GetComponent<RandomEnemy>();
            re.Enable();
            re.firstEnable = false;
        }
    }
}
