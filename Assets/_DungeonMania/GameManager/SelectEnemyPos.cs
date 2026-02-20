using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class SelectEnemyPos : MonoBehaviour{
    public static Transform[] enemyTr = new Transform[10];
    public static Vector3 SelectNewPos(int number) {
        return enemyTr [ number ].position;
    }
}
