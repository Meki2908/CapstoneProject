using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

public class CheckDungeonConfigs
{
    public static void Check()
    {
        string[] scenes = {
            "Assets/ASSETS/Dungeon_SaMac/Asset_Map_Samac/Scenes/MapSaMac.unity",
            "Assets/ASSETS/Dungeon_DamLay/Asset_Map_DamLay/Scenes/MapDamLay.unity",
            "Assets/ASSETS/Dungeon_Hell/Asset_Map_Hell/Scenes/MapDemon.unity"
        };
        
        string logPath = "CheckDungeonConfigsLog.txt";
        using (StreamWriter writer = new StreamWriter(logPath, false))
        {
            foreach (var scenePath in scenes)
            {
                writer.WriteLine($"=== SCENE: {scenePath} ===");
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var dwm = Object.FindAnyObjectByType<DungeonWaveManager>(FindObjectsInactive.Include);
                
                if (dwm != null)
                {
                    writer.WriteLine($"- DungeonWaveManager found on: {dwm.gameObject.name}");
                    writer.WriteLine($"- mapType: {dwm.mapType}");
                    writer.WriteLine($"- balanceData: {(dwm.balanceData != null ? dwm.balanceData.name : "NULL")}");
                    
                    if (dwm.enemyNewPrefab != null)
                    {
                        writer.WriteLine($"- enemyNewPrefab: {dwm.enemyNewPrefab.name} (IsAsset: {AssetDatabase.Contains(dwm.enemyNewPrefab)})");
                        var re = dwm.enemyNewPrefab.GetComponent<RandomEnemy>();
                        if (re != null)
                        {
                            writer.WriteLine($"- RandomEnemy.enemys.Length: {(re.enemys != null ? re.enemys.Length : 0)}");
                        }
                    }
                    else
                    {
                        writer.WriteLine($"- enemyNewPrefab: NULL");
                    }
                }
                else
                {
                    writer.WriteLine("- DungeonWaveManager NOT FOUND");
                }
                writer.WriteLine("");
            }
        }
        Debug.Log("Finished checking. See " + logPath);
    }
}
