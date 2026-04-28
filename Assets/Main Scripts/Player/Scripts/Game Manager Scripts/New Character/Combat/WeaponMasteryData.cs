using System;
using UnityEngine;

[System.Serializable]
public class WeaponMasteryData
{
    public WeaponType weaponType;
    public int currentLevel = 1;
    public float currentExp = 0f;

    // Skill unlock levels
    public const int E_SKILL_UNLOCK_LEVEL = 1;
    public const int R_SKILL_UNLOCK_LEVEL = 1;
    public const int T_SKILL_UNLOCK_LEVEL = 30;
    public const int Q_SKILL_UNLOCK_LEVEL = 60;

    public WeaponMasteryData(WeaponType type)
    {
        weaponType = type;
        currentLevel = 1;
        currentExp = 0f;
    }

    public bool IsSkillUnlocked(AbilityInput input)
    {
        int requiredLevel = input switch
        {
            AbilityInput.E => E_SKILL_UNLOCK_LEVEL,
            AbilityInput.R => R_SKILL_UNLOCK_LEVEL,
            AbilityInput.T => T_SKILL_UNLOCK_LEVEL,
            AbilityInput.Q_Ultimate => Q_SKILL_UNLOCK_LEVEL,
            _ => 999
        };
        return currentLevel >= requiredLevel;
    }

    public void AddExp(float exp)
    {
        currentExp += exp;
    }

    public void SetLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
        currentExp = 0f; // Reset exp when manually setting level
    }

    public void SetExp(float exp)
    {
        currentExp = Mathf.Max(0f, exp);
    }
}

[System.Serializable]
public class WeaponMasterySaveData
{
    public WeaponMasteryData[] weaponMasteries;

    public WeaponMasterySaveData()
    {
        weaponMasteries = new WeaponMasteryData[]
        {
            new WeaponMasteryData(WeaponType.Sword),
            new WeaponMasteryData(WeaponType.Axe),
            new WeaponMasteryData(WeaponType.Mage)
        };
    }
}

