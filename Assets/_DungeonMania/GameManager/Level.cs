using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
[Serializable]
public struct Level{
    public enum LevelType {
        commonLevel,
        bossLevel,
        demonLevel,
        arena
    }
    public LevelType levelType;
    public int currentLevel;
    [HideInInspector]public int index;
    [HideInInspector]public bool isSweep;
    [HideInInspector]public int[] enemyType;
}
