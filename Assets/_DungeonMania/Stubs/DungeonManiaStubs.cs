using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// COMPREHENSIVE STUBS for Dungeon Mania 
/// Only classes that DON'T exist in DungeonMania original files
/// </summary>

#region Structs

[System.Serializable]
public struct Damage
{
    public int damage;
    public int elementalType;
    public int damageElemental;
    public int crit;
    public bool isBow;
    public bool isSpell;
    public int spellID;
}

[System.Serializable]
public struct Characteristic
{
    public int value;
    public int baseValue;
    
    public Characteristic(int val) 
    { 
        value = val; 
        baseValue = val;
    }
}

[System.Serializable]
public struct Sword
{
    public int index;
    public string name;
    public int damage;
    public int attackSpeed;
    public int criticalChance;
    public int price;
}

#endregion

#region Player Classes (KHONG co trong DungeonMania goc)

public class PlayerHelth : MonoBehaviour
{
    public void PlayerDamage(Damage damage, int hit)
    {
        Debug.Log($"[Stub] Player took {damage.damage} damage");
    }
}

public class PlayerManager : MonoBehaviour
{
    public PlayerHelth playerHelth;
    public PlayerBar playerBar;
    public Animator animator;
    public Slider sliderExperiance;
    public Slider sliderHelth;
    
    public void PlayerWin(int i) { }
}

public class PlayerBar : MonoBehaviour
{
    public void CheckExperience(int exp) { }
}

[System.Serializable]
public class PlayerClass
{
    public int gameLevel = 1;
    public int dungeonLevel = 0;
    public int playerLevel = 1;
    public int gold = 0;
    public int score = 0;
    public int experiencePoint = 0;
    
    public Characteristic lucky;
    public Characteristic miner;
    public Characteristic medal;
    public Characteristic jesus;
    
    public Sword sword;
    public int currentSwordIndex = 0;
    public List<int> inventorySword = new List<int>();
    public int indicatorSwordList = 1;
    
    public CharactersClass.StatePlayerForDialogue statePlayerForDialogue;
    public bool itemMeet;
    public bool storyMeet;
    public bool bossMeet;
    public bool demonMeet;
    public bool firstBoss;
    public bool firstDemon;
    public bool firstDead;
    
    public PlayerClass() { }
    public PlayerClass(string name) 
    {
        lucky = new Characteristic(5);
        miner = new Characteristic(0);
        medal = new Characteristic(0);
        jesus = new Characteristic(0);
        statePlayerForDialogue = CharactersClass.StatePlayerForDialogue.FirstMeet;
    }
    
    public void UpdateAbilitys() { }
}

public class CharactersClass
{
    [System.Serializable]
    public enum StatePlayerForDialogue
    {
        FirstMeet,
        SecondMeet,
        ThirdMeet
    }
}

#endregion

#region Game Classes (KHONG co trong DungeonMania goc)

public class StartGame : MonoBehaviour
{
    public GameObject pauseObject;
    
    public System.Collections.IEnumerator StartBeginGame() 
    { 
        yield return null; 
    }
}

public class PlayerResurection : MonoBehaviour
{
    public void Resurection() { }
    public void NoResurection() { }
}

public class AdsManager : MonoBehaviour { }

public class IapManager : MonoBehaviour
{
    public static bool noAds = false;
    
    // Static method - được gọi từ Chest.cs và GameController.cs
    public static bool CheckNoAds() 
    { 
        return noAds; 
    }
    
    public void BuyMoney() { }
}

public class LocalizationManager : MonoBehaviour
{
    public static int localizationIndex = 0;
    public static void Init() { }
    public static string GetText(string key) { return key; }
}

public class UpdateCharacteristicsInfo : MonoBehaviour
{
    public void Info() { }
}

public class ItemDataBase : MonoBehaviour
{
    public List<Sword> swords = new List<Sword>();
}

public class Traning : MonoBehaviour { }

public class EndGame : MonoBehaviour
{
    public void GameOver() { }
    public void Win() { }
    public void ResurectionMenu() { }
}

public class Srystall : MonoBehaviour
{
    public void SetObject() { }
}

public class Door : MonoBehaviour
{
    public void DoorOpen() { }
    public void DoorClose() { }
}

public class LoadTextFiles : MonoBehaviour
{
    // Overload với 2 parameters - được gọi từ SetMenuParts.cs
    // Trả về string[] thay vì string
    public static string[] Load(string path, char separator) 
    { 
        return new string[] { "" }; 
    }
    
    public static string Load() 
    { 
        return ""; 
    }
}

public class SetStars : MonoBehaviour
{
    public static void Set() { }
}

public class SelectSword : MonoBehaviour
{
    public static void SetSword() { }
}

public class SkillButton : MonoBehaviour
{
    // Event với Action (không có parameter) - được gọi từ GameController.cs
    public static event System.Action EventSkillButton;
    
    // Instance method để match với delegate trong GameController
    public void Pause() 
    { 
        Debug.Log($"[Stub] SkillButton.Pause()");
    }
    
    public void Pause(bool b) 
    { 
        Debug.Log($"[Stub] SkillButton.Pause({b})");
    }
    
    public static void CallEvent() 
    { 
        EventSkillButton?.Invoke(); 
    }
}

#endregion
