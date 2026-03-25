#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script tự tạo EnemyBalanceData.asset với toàn bộ bảng chỉ số.
/// Menu: Dungeon → Tạo EnemyBalanceData Asset
/// </summary>
public class EnemyBalanceDataGenerator
{
    [MenuItem("Dungeon/Tạo EnemyBalanceData Asset")]
    public static void CreateBalanceDataAsset()
    {
        var table = ScriptableObject.CreateInstance<EnemyStatTable>();
        table.configs = new MapDifficultyConfig[9]; // 3 maps × 3 difficulties
        
        int idx = 0;
        
        // ===== MAP 0: SA MẠC (DESERT) =====
        // Boss phụ: Stoneogre, Boss chính: Ifrit
        
        // Desert - Easy
        table.configs[idx++] = new MapDifficultyConfig
        {
            mapType = 0, difficulty = DungeonDifficulty.Easy,
            enemyStats = new EnemyStatEntry[]
            {
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Skeleton,       hp = 50,  atk = 5,  armor = 2,  accuracy = 40 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.SkeletonArcher,  hp = 40,  atk = 8,  armor = 2,  accuracy = 45 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Orc,             hp = 80,  atk = 12, armor = 5,  accuracy = 50 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Troll,           hp = 80,  atk = 12, armor = 5,  accuracy = 50 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Guul,            hp = 80,  atk = 12, armor = 5,  accuracy = 50 },
            },
            waveConfig = new WaveConfig
            {
                skeletCount   = new int[] { 3, 4, 5, 4, 5 },
                monsterCount  = new int[] { 0, 1, 2, 2, 3 },
                stoneogreCount = new int[] { 0, 0, 0, 0, 0 },
                golemCount     = new int[] { 0, 0, 0, 0, 0 },
                minotaurCount  = new int[] { 0, 0, 0, 0, 0 },
                ifritCount     = new int[] { 0, 0, 0, 0, 0 },
                lichCount      = new int[] { 0, 0, 0, 0, 0 },
                demonCount     = new int[] { 0, 0, 0, 0, 0 },
            }
        };
        
        // Desert - Normal
        table.configs[idx++] = new MapDifficultyConfig
        {
            mapType = 0, difficulty = DungeonDifficulty.Normal,
            enemyStats = new EnemyStatEntry[]
            {
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Skeleton,       hp = 80,   atk = 8,  armor = 4,  accuracy = 45 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.SkeletonArcher,  hp = 60,   atk = 12, armor = 3,  accuracy = 50 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Orc,             hp = 120,  atk = 18, armor = 8,  accuracy = 55 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Troll,           hp = 120,  atk = 18, armor = 8,  accuracy = 55 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Guul,            hp = 120,  atk = 18, armor = 8,  accuracy = 55 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Stoneogre,       hp = 600,  atk = 30, armor = 12, accuracy = 65 },
            },
            waveConfig = new WaveConfig
            {
                skeletCount    = new int[] { 3, 4, 5, 4, 0 },
                monsterCount   = new int[] { 0, 1, 2, 2, 0 },
                stoneogreCount = new int[] { 0, 0, 0, 0, 1 },
                golemCount     = new int[] { 0, 0, 0, 0, 0 },
                minotaurCount  = new int[] { 0, 0, 0, 0, 0 },
                ifritCount     = new int[] { 0, 0, 0, 0, 0 },
                lichCount      = new int[] { 0, 0, 0, 0, 0 },
                demonCount     = new int[] { 0, 0, 0, 0, 0 },
            }
        };
        
        // Desert - Hard
        table.configs[idx++] = new MapDifficultyConfig
        {
            mapType = 0, difficulty = DungeonDifficulty.Hard,
            enemyStats = new EnemyStatEntry[]
            {
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Skeleton,       hp = 120,  atk = 12, armor = 6,  accuracy = 50 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.SkeletonArcher,  hp = 90,   atk = 18, armor = 5,  accuracy = 55 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Orc,             hp = 180,  atk = 25, armor = 12, accuracy = 60 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Troll,           hp = 180,  atk = 25, armor = 12, accuracy = 60 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Guul,            hp = 180,  atk = 25, armor = 12, accuracy = 60 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Stoneogre,       hp = 900,  atk = 45, armor = 18, accuracy = 70 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Ifrit,           hp = 2000, atk = 70, armor = 25, accuracy = 75 },
            },
            waveConfig = new WaveConfig
            {
                skeletCount    = new int[] { 3, 4, 5, 3, 0 },
                monsterCount   = new int[] { 0, 1, 2, 0, 0 },
                stoneogreCount = new int[] { 0, 0, 0, 1, 0 },
                golemCount     = new int[] { 0, 0, 0, 0, 0 },
                minotaurCount  = new int[] { 0, 0, 0, 0, 0 },
                ifritCount     = new int[] { 0, 0, 0, 0, 1 },
                lichCount      = new int[] { 0, 0, 0, 0, 0 },
                demonCount     = new int[] { 0, 0, 0, 0, 0 },
            }
        };
        
        // ===== MAP 1: ĐẦM LẦY (SWAMP) =====
        // Boss phụ: Golem, Boss chính: Lich
        
        // Swamp - Easy
        table.configs[idx++] = new MapDifficultyConfig
        {
            mapType = 1, difficulty = DungeonDifficulty.Easy,
            enemyStats = new EnemyStatEntry[]
            {
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Skeleton,       hp = 80,  atk = 10, armor = 4,  accuracy = 45 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.SkeletonArcher,  hp = 60,  atk = 15, armor = 3,  accuracy = 50 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Orc,             hp = 130, atk = 20, armor = 8,  accuracy = 55 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Troll,           hp = 130, atk = 20, armor = 8,  accuracy = 55 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Guul,            hp = 130, atk = 20, armor = 8,  accuracy = 55 },
            },
            waveConfig = new WaveConfig
            {
                skeletCount    = new int[] { 3, 4, 5, 4, 5 },
                monsterCount   = new int[] { 0, 1, 2, 2, 3 },
                stoneogreCount = new int[] { 0, 0, 0, 0, 0 },
                golemCount     = new int[] { 0, 0, 0, 0, 0 },
                minotaurCount  = new int[] { 0, 0, 0, 0, 0 },
                ifritCount     = new int[] { 0, 0, 0, 0, 0 },
                lichCount      = new int[] { 0, 0, 0, 0, 0 },
                demonCount     = new int[] { 0, 0, 0, 0, 0 },
            }
        };
        
        // Swamp - Normal
        table.configs[idx++] = new MapDifficultyConfig
        {
            mapType = 1, difficulty = DungeonDifficulty.Normal,
            enemyStats = new EnemyStatEntry[]
            {
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Skeleton,       hp = 120,  atk = 15, armor = 6,  accuracy = 50 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.SkeletonArcher,  hp = 90,   atk = 20, armor = 5,  accuracy = 55 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Orc,             hp = 200,  atk = 28, armor = 12, accuracy = 60 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Troll,           hp = 200,  atk = 28, armor = 12, accuracy = 60 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Guul,            hp = 200,  atk = 28, armor = 12, accuracy = 60 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Golem,           hp = 1000, atk = 50, armor = 20, accuracy = 70 },
            },
            waveConfig = new WaveConfig
            {
                skeletCount    = new int[] { 3, 4, 5, 4, 0 },
                monsterCount   = new int[] { 0, 1, 2, 2, 0 },
                stoneogreCount = new int[] { 0, 0, 0, 0, 0 },
                golemCount     = new int[] { 0, 0, 0, 0, 1 },
                minotaurCount  = new int[] { 0, 0, 0, 0, 0 },
                ifritCount     = new int[] { 0, 0, 0, 0, 0 },
                lichCount      = new int[] { 0, 0, 0, 0, 0 },
                demonCount     = new int[] { 0, 0, 0, 0, 0 },
            }
        };
        
        // Swamp - Hard
        table.configs[idx++] = new MapDifficultyConfig
        {
            mapType = 1, difficulty = DungeonDifficulty.Hard,
            enemyStats = new EnemyStatEntry[]
            {
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Skeleton,       hp = 180,  atk = 22, armor = 8,  accuracy = 55 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.SkeletonArcher,  hp = 130,  atk = 28, armor = 7,  accuracy = 60 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Orc,             hp = 300,  atk = 38, armor = 18, accuracy = 65 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Troll,           hp = 300,  atk = 38, armor = 18, accuracy = 65 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Guul,            hp = 300,  atk = 38, armor = 18, accuracy = 65 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Golem,           hp = 1500, atk = 70, armor = 28, accuracy = 75 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Lich,            hp = 2500, atk = 85, armor = 30, accuracy = 78 },
            },
            waveConfig = new WaveConfig
            {
                skeletCount    = new int[] { 3, 4, 5, 3, 0 },
                monsterCount   = new int[] { 0, 1, 2, 0, 0 },
                stoneogreCount = new int[] { 0, 0, 0, 0, 0 },
                golemCount     = new int[] { 0, 0, 0, 1, 0 },
                minotaurCount  = new int[] { 0, 0, 0, 0, 0 },
                ifritCount     = new int[] { 0, 0, 0, 0, 0 },
                lichCount      = new int[] { 0, 0, 0, 0, 1 },
                demonCount     = new int[] { 0, 0, 0, 0, 0 },
            }
        };
        
        // ===== MAP 2: HELL =====
        // Boss phụ: Minotaur, Boss chính: Demon
        
        // Hell - Easy
        table.configs[idx++] = new MapDifficultyConfig
        {
            mapType = 2, difficulty = DungeonDifficulty.Easy,
            enemyStats = new EnemyStatEntry[]
            {
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Skeleton,       hp = 150, atk = 18, armor = 6,  accuracy = 50 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.SkeletonArcher,  hp = 110, atk = 22, armor = 5,  accuracy = 55 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Orc,             hp = 250, atk = 30, armor = 12, accuracy = 60 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Troll,           hp = 250, atk = 30, armor = 12, accuracy = 60 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Guul,            hp = 250, atk = 30, armor = 12, accuracy = 60 },
            },
            waveConfig = new WaveConfig
            {
                skeletCount    = new int[] { 3, 4, 5, 4, 5 },
                monsterCount   = new int[] { 0, 1, 2, 2, 3 },
                stoneogreCount = new int[] { 0, 0, 0, 0, 0 },
                golemCount     = new int[] { 0, 0, 0, 0, 0 },
                minotaurCount  = new int[] { 0, 0, 0, 0, 0 },
                ifritCount     = new int[] { 0, 0, 0, 0, 0 },
                lichCount      = new int[] { 0, 0, 0, 0, 0 },
                demonCount     = new int[] { 0, 0, 0, 0, 0 },
            }
        };
        
        // Hell - Normal
        table.configs[idx++] = new MapDifficultyConfig
        {
            mapType = 2, difficulty = DungeonDifficulty.Normal,
            enemyStats = new EnemyStatEntry[]
            {
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Skeleton,       hp = 220,  atk = 25, armor = 8,  accuracy = 55 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.SkeletonArcher,  hp = 160,  atk = 30, armor = 7,  accuracy = 60 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Orc,             hp = 380,  atk = 42, armor = 18, accuracy = 65 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Troll,           hp = 380,  atk = 42, armor = 18, accuracy = 65 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Guul,            hp = 380,  atk = 42, armor = 18, accuracy = 65 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Minotaur,        hp = 1800, atk = 75, armor = 30, accuracy = 75 },
            },
            waveConfig = new WaveConfig
            {
                skeletCount    = new int[] { 3, 4, 5, 4, 0 },
                monsterCount   = new int[] { 0, 1, 2, 2, 0 },
                stoneogreCount = new int[] { 0, 0, 0, 0, 0 },
                golemCount     = new int[] { 0, 0, 0, 0, 0 },
                minotaurCount  = new int[] { 0, 0, 0, 0, 1 },
                ifritCount     = new int[] { 0, 0, 0, 0, 0 },
                lichCount      = new int[] { 0, 0, 0, 0, 0 },
                demonCount     = new int[] { 0, 0, 0, 0, 0 },
            }
        };
        
        // Hell - Hard
        table.configs[idx++] = new MapDifficultyConfig
        {
            mapType = 2, difficulty = DungeonDifficulty.Hard,
            enemyStats = new EnemyStatEntry[]
            {
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Skeleton,       hp = 320,  atk = 35,  armor = 12, accuracy = 60 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.SkeletonArcher,  hp = 240,  atk = 42,  armor = 10, accuracy = 65 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Orc,             hp = 550,  atk = 58,  armor = 25, accuracy = 70 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Troll,           hp = 550,  atk = 58,  armor = 25, accuracy = 70 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Guul,            hp = 550,  atk = 58,  armor = 25, accuracy = 70 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Minotaur,        hp = 2500, atk = 100, armor = 40, accuracy = 80 },
                new EnemyStatEntry { enemyType = EnemyScript.SpecificEnemyType.Demon,           hp = 5000, atk = 130, armor = 50, accuracy = 85 },
            },
            waveConfig = new WaveConfig
            {
                skeletCount    = new int[] { 3, 4, 5, 3, 0 },
                monsterCount   = new int[] { 0, 1, 2, 0, 0 },
                stoneogreCount = new int[] { 0, 0, 0, 0, 0 },
                golemCount     = new int[] { 0, 0, 0, 0, 0 },
                minotaurCount  = new int[] { 0, 0, 0, 1, 0 },
                ifritCount     = new int[] { 0, 0, 0, 0, 0 },
                lichCount      = new int[] { 0, 0, 0, 0, 0 },
                demonCount     = new int[] { 0, 0, 0, 0, 1 },
            }
        };
        
        // Lưu asset
        string path = "Assets/_DungeonMania/Data/EnemyBalanceData.asset";
        // Tạo folder nếu chưa có
        if (!AssetDatabase.IsValidFolder("Assets/_DungeonMania/Data"))
        {
            AssetDatabase.CreateFolder("Assets/_DungeonMania", "Data");
        }
        
        AssetDatabase.CreateAsset(table, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = table;
        
        Debug.Log($"[EnemyBalance] Đã tạo EnemyBalanceData.asset tại {path} — 9 configs (3 map × 3 khó)");
    }
}
#endif
