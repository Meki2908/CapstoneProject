using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

public class WeaponMasteryManager : MonoBehaviour
{
    public static WeaponMasteryManager Instance { get; private set; }

    [Header("Save Settings")]
    [SerializeField] private string saveFileName = "weapon_mastery.json";

    private Dictionary<WeaponType, WeaponMasteryData> masteryData = new Dictionary<WeaponType, WeaponMasteryData>();
    private WeaponMasterySaveData saveData;

    // Events
    public event Action<WeaponType, int> OnLevelUp;
    public event Action<WeaponType> OnExpGained;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeMasteryData();
            LoadMasteryData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeMasteryData()
    {
        masteryData[WeaponType.Sword] = new WeaponMasteryData(WeaponType.Sword);
        masteryData[WeaponType.Axe] = new WeaponMasteryData(WeaponType.Axe);
        masteryData[WeaponType.Mage] = new WeaponMasteryData(WeaponType.Mage);
    }

    public WeaponMasteryData GetMasteryData(WeaponType weaponType)
    {
        if (!masteryData.ContainsKey(weaponType))
        {
            masteryData[weaponType] = new WeaponMasteryData(weaponType);
        }
        return masteryData[weaponType];
    }

    public void AddExp(WeaponType weaponType, float exp, WeaponSO weaponSO = null)
    {
        if (!masteryData.ContainsKey(weaponType))
        {
            masteryData[weaponType] = new WeaponMasteryData(weaponType);
        }

        var data = masteryData[weaponType];
        int oldLevel = data.currentLevel;

        data.AddExp(exp);

        // Check for level up
        if (weaponSO != null)
        {
            while (true)
            {
                float expRequired = weaponSO.GetExpRequiredForNextLevel(data.currentLevel);
                if (data.currentExp >= expRequired)
                {
                    data.currentExp -= expRequired;
                    data.currentLevel++;

                    if (data.currentLevel > 100)
                    {
                        data.currentLevel = 100;
                        data.currentExp = 0f;
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        OnExpGained?.Invoke(weaponType);

        if (data.currentLevel > oldLevel)
        {
            OnLevelUp?.Invoke(weaponType, data.currentLevel);
        }

        SaveMasteryData();
    }

    public bool IsSkillUnlocked(WeaponType weaponType, AbilityInput input)
    {
        if (!masteryData.ContainsKey(weaponType))
        {
            masteryData[weaponType] = new WeaponMasteryData(weaponType);
        }
        return masteryData[weaponType].IsSkillUnlocked(input);
    }

    public int GetMasteryLevel(WeaponType weaponType)
    {
        if (!masteryData.ContainsKey(weaponType))
        {
            masteryData[weaponType] = new WeaponMasteryData(weaponType);
        }
        return masteryData[weaponType].currentLevel;
    }

    public float GetCurrentExp(WeaponType weaponType)
    {
        if (!masteryData.ContainsKey(weaponType))
        {
            masteryData[weaponType] = new WeaponMasteryData(weaponType);
        }
        return masteryData[weaponType].currentExp;
    }

    public float GetExpRequiredForNextLevel(WeaponType weaponType, WeaponSO weaponSO)
    {
        if (!masteryData.ContainsKey(weaponType))
        {
            masteryData[weaponType] = new WeaponMasteryData(weaponType);
        }
        return weaponSO.GetExpRequiredForNextLevel(masteryData[weaponType].currentLevel);
    }

    // Editor/Demo methods
    public void SetMasteryLevel(WeaponType weaponType, int level)
    {
        if (!masteryData.ContainsKey(weaponType))
        {
            masteryData[weaponType] = new WeaponMasteryData(weaponType);
        }
        masteryData[weaponType].SetLevel(level);
        SaveMasteryData();
        OnLevelUp?.Invoke(weaponType, level);
    }

    public void SetMasteryExp(WeaponType weaponType, float exp)
    {
        if (!masteryData.ContainsKey(weaponType))
        {
            masteryData[weaponType] = new WeaponMasteryData(weaponType);
        }
        masteryData[weaponType].SetExp(exp);
        SaveMasteryData();
        OnExpGained?.Invoke(weaponType);
    }

    // Save/Load
    private void SaveMasteryData()
    {
        saveData = new WeaponMasterySaveData();
        saveData.weaponMasteries = new WeaponMasteryData[]
        {
            masteryData.ContainsKey(WeaponType.Sword) ? masteryData[WeaponType.Sword] : new WeaponMasteryData(WeaponType.Sword),
            masteryData.ContainsKey(WeaponType.Axe) ? masteryData[WeaponType.Axe] : new WeaponMasteryData(WeaponType.Axe),
            masteryData.ContainsKey(WeaponType.Mage) ? masteryData[WeaponType.Mage] : new WeaponMasteryData(WeaponType.Mage)
        };

        string json = JsonUtility.ToJson(saveData, true);
        string filePath = Path.Combine(Application.persistentDataPath, saveFileName);

        try
        {
            File.WriteAllText(filePath, json);
            Debug.Log($"[WeaponMasteryManager] Saved mastery data to {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[WeaponMasteryManager] Failed to save mastery data: {e.Message}");
        }
    }

    private void LoadMasteryData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, saveFileName);

        if (!File.Exists(filePath))
        {
            Debug.Log($"[WeaponMasteryManager] No save file found at {filePath}, using default data");
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            saveData = JsonUtility.FromJson<WeaponMasterySaveData>(json);

            if (saveData != null && saveData.weaponMasteries != null)
            {
                foreach (var mastery in saveData.weaponMasteries)
                {
                    masteryData[mastery.weaponType] = mastery;
                }
                Debug.Log($"[WeaponMasteryManager] Loaded mastery data from {filePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[WeaponMasteryManager] Failed to load mastery data: {e.Message}");
        }
    }

    public void ResetAllMasteryData()
    {
        InitializeMasteryData();
        SaveMasteryData();
        Debug.Log("[WeaponMasteryManager] Reset all mastery data");
    }
}

