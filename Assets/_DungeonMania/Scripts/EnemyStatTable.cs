using UnityEngine;

/// <summary>
/// Chỉ số 1 loại enemy trong 1 cấu hình (map + độ khó)
/// </summary>
[System.Serializable]
public class EnemyStatEntry
{
    [Tooltip("Loại enemy")]
    public EnemyScript.SpecificEnemyType enemyType;
    
    [Tooltip("Máu (HP) — sẽ override TakeDamageTest.MaxHealth")]
    public float hp = 100f;
    
    [Tooltip("Sát thương — override EnemyScript.attackDamage")]
    public int atk = 10;
    
    [Tooltip("Giáp — override EnemyScript.armorValue")]
    public int armor = 5;
    
    [Tooltip("Độ chính xác (%) — override EnemyScript.accuracy")]
    public int accuracy = 50;
}

/// <summary>
/// Wave config cho 1 độ khó: số lượng mỗi loại enemy per wave
/// </summary>
[System.Serializable]
public class WaveConfig
{
    [Tooltip("Số Skeleton mỗi wave [w1,w2,w3,w4,w5]")]
    public int[] skeletCount = { 3, 4, 5, 4, 3 };
    
    [Tooltip("Số Monster (Orc/Troll/Guul) mỗi wave")]
    public int[] monsterCount = { 0, 1, 2, 2, 2 };
    
    [Tooltip("Số Stoneogre mỗi wave")]
    public int[] stoneogreCount = { 0, 0, 0, 0, 0 };
    
    [Tooltip("Số Golem mỗi wave")]
    public int[] golemCount = { 0, 0, 0, 0, 0 };
    
    [Tooltip("Số Minotaur mỗi wave")]
    public int[] minotaurCount = { 0, 0, 0, 0, 0 };
    
    [Tooltip("Số Ifrit mỗi wave")]
    public int[] ifritCount = { 0, 0, 0, 0, 0 };
    
    [Tooltip("Số Lich mỗi wave")]
    public int[] lichCount = { 0, 0, 0, 0, 0 };
    
    [Tooltip("Số Demon mỗi wave")]
    public int[] demonCount = { 0, 0, 0, 0, 0 };
}

/// <summary>
/// Config cho 1 map + 1 độ khó
/// </summary>
[System.Serializable]
public class MapDifficultyConfig
{
    [Tooltip("Map type: 0=Desert, 1=Swamp, 2=Hell")]
    public int mapType;
    
    [Tooltip("Độ khó")]
    public DungeonDifficulty difficulty;
    
    [Tooltip("Chỉ số từng loại enemy")]
    public EnemyStatEntry[] enemyStats;
    
    [Tooltip("Wave config — số lượng enemy per wave")]
    public WaveConfig waveConfig;
}

/// <summary>
/// ScriptableObject chứa toàn bộ bảng cân bằng chỉ số enemy.
/// Tạo bằng menu: Create → Dungeon → EnemyStatTable
/// DungeonWaveManager sẽ đọc SO này + DungeonConfig để apply stats khi spawn.
/// </summary>
[CreateAssetMenu(fileName = "EnemyBalanceData", menuName = "Dungeon/EnemyStatTable")]
public class EnemyStatTable : ScriptableObject
{
    [Tooltip("Tất cả config: 3 map × 3 độ khó = 9 entries")]
    public MapDifficultyConfig[] configs;
    
    /// <summary>
    /// Tìm config theo map + difficulty
    /// </summary>
    public MapDifficultyConfig GetConfig(int mapType, DungeonDifficulty difficulty)
    {
        if (configs == null) return null;
        foreach (var c in configs)
        {
            if (c.mapType == mapType && c.difficulty == difficulty)
                return c;
        }
        Debug.LogWarning($"[EnemyStatTable] Không tìm thấy config: map={mapType}, diff={difficulty}");
        return null;
    }
    
    /// <summary>
    /// Tìm stat entry cho 1 loại enemy cụ thể
    /// </summary>
    public EnemyStatEntry GetStats(int mapType, DungeonDifficulty difficulty, EnemyScript.SpecificEnemyType enemyType)
    {
        var config = GetConfig(mapType, difficulty);
        if (config?.enemyStats == null) return null;
        foreach (var s in config.enemyStats)
        {
            if (s.enemyType == enemyType)
                return s;
        }
        return null;
    }
}
