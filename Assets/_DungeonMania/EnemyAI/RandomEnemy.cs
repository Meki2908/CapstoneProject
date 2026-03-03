using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomEnemy : MonoBehaviour{
    int childNumber;
    public ParticleSystem pentagram;
    int index;
    public GameObject[] enemys;
    public bool firstEnable;
    Vector3 newPos;
    
    // Sử dụng hệ thống wave mới
    public bool useNewWaveSystem = true;
    
    private void Awake(){
        childNumber = transform.GetSiblingIndex ();
    }
    private void OnEnable(){
        EnemyEvent.EnableEvent += Enable;
        EnemyEvent.DisableEvent += Disable;
    }
    private void OnDisable(){
        EnemyEvent.EnableEvent -= Enable;
        EnemyEvent.DisableEvent -= Disable;
    }
    public void Enable (){
        if (GamePlayManager.level.levelType == Level.LevelType.arena) {
            GamePlayManager.checkAreneEnemys++;
        }
        
        if (useNewWaveSystem && DungeonWaveManager.Instance != null)
        {
            // Sử dụng hệ thống wave mới
            EnableNewWaveSystem();
        }
        else
        {
            // Sử dụng hệ thống cũ
        EnemyChoose( GamePlayManager.level.enemyType[0],
            GamePlayManager.level.enemyType[1],
            GamePlayManager.level.enemyType[2],
            GamePlayManager.level.enemyType[3],
            GamePlayManager.level.enemyType[4] );
    }
    }
    
    /// <summary>
    /// Được gọi TRỰC TIẾP bởi DungeonWaveManager thay vì qua static event
    /// Tránh broadcast tới tất cả enemy đã subscribe
    /// </summary>
    public void EnableDirect()
    {
        Debug.Log($"[RandomEnemy] EnableDirect called on {gameObject.name}");
        Enable();
    }
    
    // Hệ thống wave mới
    // Wave 1-3: Skelet (Skeleton + skeleton_archer) - index 0,1
    // Wave 4: Skelet + Lich - index 0,1,5
    // Wave 5: Boss + Demon - index 6,7,8,9,10
    void EnableNewWaveSystem()
    {
        if (DungeonWaveManager.Instance == null)
        {
            Debug.LogWarning("[RandomEnemy] DungeonWaveManager.Instance is null! Fallback to old system.");
            useNewWaveSystem = false;
            Enable();
            return;
        }
        
        // Sử dụng GetRemainingEnemyCounts để KHÔNG reset counters
        int skeletCount, lichCount, bossCount, demonCount;
        DungeonWaveManager.Instance.GetRemainingEnemyCounts(out skeletCount, out lichCount, out bossCount, out demonCount);
        
        // KHÔNG reset counters ở đây nữa - đã được ConfigureGamePlayManager reset rồi
        // GamePlayManager.archers, boss, lich, demon đã được theo dõi trong EnemyChooseNewWave
        
        EnemyChooseNewWave(skeletCount, lichCount, bossCount, demonCount);
    }
    
    void EnemyChooseNewWave(int skeletCount, int lichCount, int bossCount, int demonCount)
    {
        // Kiểm tra bounds cho mảng enemy trước
        if (enemys == null || enemys.Length == 0) {
            Debug.LogError("[RandomEnemy] enemys array is null or empty!");
            return;
        }

        // Reset index về giá trị mặc định an toàn
        index = 0;

        // Ưu tiên theo thứ tự: Demon > Boss > Lich > Skelet
        // Demon: index 10
        if (GamePlayManager.demon < demonCount) {
            index = 10; // Demon
            GamePlayManager.demon++;
        }
        // Boss: index 6-9 (Stoneogre, Golem, Minotaur, Ifrit)
        else if (GamePlayManager.boss < bossCount) {
            index = Random.Range(6, 10); // Random Boss type
            GamePlayManager.boss++;
        }
        // Lich: index 5
        else if (GamePlayManager.lich < lichCount) {
            index = 5;
            GamePlayManager.lich++;
        }
        // Skelet: index 0-1 (Skeleton, skeleton_archer)
        else if (GamePlayManager.archers < skeletCount) {
            index = Random.Range(0, 2); // Random giữa Skeleton và Archer
            GamePlayManager.archers++;
        }
        else {
            // Fallback: Chọn enemy ngẫu nhiên nếu tất cả các loại đều đạt giới hạn
            // Ưu tiên chọn từ các loại đã có trong wave
            List<int> availableIndices = new List<int>();
            
            if (demonCount > 0) availableIndices.Add(10);
            if (bossCount > 0) availableIndices.AddRange(new int[] {6, 7, 8, 9});
            if (lichCount > 0) availableIndices.Add(5);
            if (skeletCount > 0) availableIndices.AddRange(new int[] {0, 1});
            
            if (availableIndices.Count > 0) {
                index = availableIndices[Random.Range(0, availableIndices.Count)];
            }
            else {
                index = Random.Range(0, Mathf.Min(6, enemys.Length));
            }
            Debug.LogWarning($"[RandomEnemy] All enemy types at limit, using random index: {index}");
        }

        // Đảm bảo index nằm trong bounds
        index = Mathf.Clamp(index, 0, enemys.Length - 1);

        SetEnemy();
    }
    
    // Hệ thống cũ - giữ nguyên để tương thích
    void EnemyChoose(int archers, int monsters, int lichCount, int bossCount, int demonCount) {
        // Sửa: Kiểm tra bounds cho mảng enemy trước
        if (enemys == null || enemys.Length == 0) {
            Debug.LogError("[RandomEnemy] enemys array is null or empty!");
            return;
        }

        // Reset index về giá trị mặc định an toàn
        index = 0;

        // Sửa: So sánh với tham số đúng (lichCount thay vì lich)
        if (GamePlayManager.demon < demonCount) {
            // Demon index từ 6-9
            if (GamePlayManager.level.levelType == Level.LevelType.arena) index = Random.Range(6, 10);
            else index = 6 + GamePlayManager.demon; // Assign sequential demon types
            GamePlayManager.demon++;
        }
        else if (GamePlayManager.boss < bossCount) {
            if (GamePlayManager.level.levelType == Level.LevelType.arena || PlayerPrefs.GetInt("QUEST_COUNT") >= 5) index = Random.Range(6, 10);
            else index = PlayerPrefs.GetInt("QUEST_COUNT") + 5;
            GamePlayManager.boss++;
        }
        else if (GamePlayManager.lich < lichCount) {
            index = 5;
            GamePlayManager.lich++;
        }
        else if (GamePlayManager.monsteres < monsters) {
            if (GamePlayManager.level.levelType == Level.LevelType.arena) index = Random.Range(2, 5);
            else {
                switch (PlayerPrefs.GetInt("QUEST_COUNT")) {
                    case 0:
                        index = 2; // Default to monster type
                        break;
                    case 1:
                        index = 2;
                        break;
                    case 2:
                        index = Random.Range(2, 4);
                        break;
                    default:
                        index = Random.Range(2, 5);
                        break;
                }
            }
            GamePlayManager.monsteres++;
        }
        else if (GamePlayManager.archers < archers) {
            index = 1;
            GamePlayManager.archers++;
        }
        else {
            // Fallback: Chọn enemy ngẫu nhiên nếu tất cả các loại đều đạt giới hạn
            index = Random.Range(0, Mathf.Min(6, enemys.Length));
            Debug.LogWarning($"[RandomEnemy] All enemy types at limit, using random index: {index}");
        }

        // Đảm bảo index nằm trong bounds
        index = Mathf.Clamp(index, 0, enemys.Length - 1);

        SetEnemy();
    }
    void Disable(){
        if (enemys == null || index < 0 || index >= enemys.Length) return;
        if (enemys[index] != null) {
            enemys[index].SetActive(false);
        }
    }
    void SetEnemy(){
        if (enemys == null || index < 0 || index >= enemys.Length) {
            Debug.LogError($"[RandomEnemy] Invalid index {index} or null array!");
            return;
        }

        newPos = SelectEnemyPos.SelectNewPos(childNumber);

        // Kiểm tra nếu vị trí hợp lệ
        if (newPos == Vector3.zero) {
            Debug.LogWarning("[RandomEnemy] Invalid spawn position!");
            // Thử vị trí hiện tại của transform
            newPos = transform.position;
        }

        if (pentagram != null) {
            pentagram.transform.position = newPos;
            pentagram.Play();
        }

        if (enemys[index] != null) {
            // Cập nhật vị trí và kích hoạt enemy
            enemys[index].transform.position = newPos;
            enemys[index].SetActive(true);
            
            // Cập nhật specific enemy type cho EnemyScript
            EnemyScript enemyScript = enemys[index].GetComponent<EnemyScript>();
            if (enemyScript != null)
            {
                enemyScript.SetSpecificEnemyType(index);
                // Áp dụng lại giá trị từ Inspector sau khi đã set specific type
                enemyScript.ApplyInspectorValuesManual();
            }
        } else {
            Debug.LogError($"[RandomEnemy] Enemy at index {index} is null!");
        }
    }
}
