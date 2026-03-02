using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DungeonWaveManager : MonoBehaviour
{
    [Header("=== DUNGEON SETTINGS ===")]
    [Tooltip("Tên dungeon để hiển thị")]
    public string dungeonName = "Dungeon 1";
    [Tooltip("Tổng số wave trong dungeon")]
    public int totalWaves = 5;
    [Tooltip("Map type: 0=Desert, 1=Swamp, 2=Hell")]
    public int mapType = 0;

    [Header("=== SPAWN SETTINGS ===")]
    [Tooltip("Bán kính spawn enemy xung quanh player")]
    public float spawnRadius = 20f;
    [Tooltip("Khoảng cách tối thiểu từ player khi spawn")]
    public float minDistanceFromPlayer = 5f;
    [Tooltip("Layer của vật cản (tường, đá...)")]
    public LayerMask obstacleLayer;
    [Tooltip("Số lần thử tìm vị trí spawn hợp lệ")]
    public int spawnAttemptCount = 10;

    [Header("=== WAVE TIMING ===")]
    [Tooltip("Thời gian đếm ngược trước khi wave bắt đầu (giây)")]
    public float waveCountdownTime = 5f;
    [Tooltip("Thời gian nghỉ giữa các wave (giây)")]
    public float waveDelayTime = 3f;

    [Header("=== ENEMY COUNT PER WAVE (CÁCH A - HỆ THỐNG MỚI) ===")]
    [Tooltip("Số lượng Skelet (Skeleton + skeleton_archer) mỗi wave [wave1, wave2, wave3, wave4, wave5]")]
    public int[] skeletCount = { 3, 4, 5, 4, 0 };
    
    [Tooltip("Số lượng Lich mỗi wave")]
    public int[] lichCount = { 0, 0, 0, 1, 0 };
    
    [Tooltip("Số lượng Boss mỗi wave")]
    public int[] bossCount = { 0, 0, 0, 0, 2 };
    
    [Tooltip("Số lượng Demon mỗi wave")]
    public int[] demonCount = { 0, 0, 0, 0, 1 };
    
    [Tooltip("Tổng số enemy mỗi wave (tự tính)")]
    public int[] totalEnemiesPerWave;

    [Header("=== PREFAB (CÁCH A) ===")]
    [Tooltip("Prefab EnemyNew (chứa tất cả enemy bên trong)")]
    public GameObject enemyNewPrefab;

    [Header("=== REFERENCES ===")]
    [Tooltip("Player object")]
    public Transform player;
    [Tooltip("UI Wave Notification")]
    public GameObject waveNotificationUI;
    [Tooltip("Text hiển thị tên wave")]
    public TextMeshProUGUI waveNameText;
    [Tooltip("UI Countdown")]
    public GameObject countdownUI;
    [Tooltip("Text hiển thị đếm ngược")]
    public TextMeshProUGUI countdownText;
    [Tooltip("UI Dungeon Complete")]
    public GameObject dungeonCompleteUI;
    [Tooltip("UI Dungeon Failed")]
    public GameObject dungeonFailedUI;
    [Tooltip("Text hiển thị EXP nhận được")]
    public TextMeshProUGUI expRewardText;
    [Tooltip("Text hiển thị thông báo")]
    public TextMeshProUGUI statusText;

    // ===== PRIVATE VARIABLES =====
    private int currentWave = 0;
    private int enemiesAlive = 0;
    private bool isWaveActive = false;
    private bool isCountingDown = false;
    private bool isDungeonActive = false;
    private bool isDungeonComplete = false;
    private bool showDebugLog = true;
    
    // Trackers for enemy spawn (tránh gọi GetEnemyCounts nhiều lần)
    private int currentSkeletCount = 0;
    private int currentLichCount = 0;
    private int currentBossCount = 0;
    private int currentDemonCount = 0;
    
    // Static instance for global access
    public static DungeonWaveManager Instance;

    // Events
    public System.Action<int> OnWaveStarted;
    public System.Action<int> OnWaveCompleted;
    public System.Action OnDungeonCompleted;
    public System.Action OnDungeonFailed;

    // EXP tracking
    private int totalExpGained = 0;

    // ===== UNITY LIFECYCLE =====

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Tìm player TRƯỚC KHI làm gì khác
        if (player == null)
        {
            // Thử tìm bằng tag "Player" trước (project của bạn dùng tag)
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            
            // Nếu không có tag, thử tìm bằng tên "Player" hoặc "player"
            if (playerObj == null)
                playerObj = GameObject.Find("Player");
            if (playerObj == null)
                playerObj = GameObject.Find("player");
                
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log($"[DungeonWave] Found player: {playerObj.name}");
            }
        }

        // Tính tổng số enemy mỗi wave
        CalculateTotalEnemiesPerWave();

        // THIẾT LẬP CÁC REFERENCES CẦN THIẾT CHO DUNGEONMANIA (SAU KHI TÌM ĐƯỢC PLAYER)
        SetupPlayerReference();

        // Ẩn tất cả UI
        HideAllUI();

        // Bắt đầu dungeon
        StartDungeon();
    }

    /// <summary>
    /// Thiết lập "player" (lowercase) reference cho EnemyScript
    /// EnemyScript tìm object "player" để làm target
    /// </summary>
    private void SetupPlayerReference()
    {
        // Tìm object "player" (lowercase) - EnemyScript cần cái này làm target
        GameObject playerLower = GameObject.Find("player");
        
        if (playerLower == null && player != null)
        {
            // Tạo mới "player" reference nếu chưa có
            playerLower = new GameObject("player");
            playerLower.transform.position = player.position;
            playerLower.transform.rotation = player.rotation;
            // Làm child của player thật để di chuyển cùng
            playerLower.transform.SetParent(player);
            Debug.Log("[DungeonWave] Created 'player' reference for EnemyScript");
        }
        else if (playerLower != null && player != null)
        {
            // Cập nhật vị trí nếu player đã di chuyển
            playerLower.transform.position = player.position;
            playerLower.transform.rotation = player.rotation;
        }

        Debug.Log("[DungeonWave] Player reference ready for enemies");
    }

    void Update()
    {
        if (!isDungeonActive) return;

        // Kiểm tra enemy còn sống
        if (isWaveActive && enemiesAlive <= 0 && !isCountingDown)
        {
            OnWaveComplete();
        }

        // Debug: Nhấn K để kill tất cả enemy (test)
        if (Input.GetKeyDown(KeyCode.K))
        {
            KillAllEnemiesForTest();
        }
        
        // Debug: Nhấn N để next wave (test)
        if (Input.GetKeyDown(KeyCode.N))
        {
            ForceNextWave();
        }
        
        // Debug: Nhấn 1-5 để jump to wave (test)
        if (Input.GetKeyDown(KeyCode.Alpha1)) GoToWave(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) GoToWave(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) GoToWave(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) GoToWave(4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) GoToWave(5);
    }

    // ===== DUNGEON FLOW =====

    /// <summary>
    /// Tính tổng số enemy mỗi wave
    /// </summary>
    private void CalculateTotalEnemiesPerWave()
    {
        totalEnemiesPerWave = new int[totalWaves];
        for (int i = 0; i < totalWaves; i++)
        {
            int skelets = i < skeletCount.Length ? skeletCount[i] : 0;
            int liches = i < lichCount.Length ? lichCount[i] : 0;
            int bosses = i < bossCount.Length ? bossCount[i] : 0;
            int demons = i < demonCount.Length ? demonCount[i] : 0;
            totalEnemiesPerWave[i] = skelets + liches + bosses + demons;
        }
    }

    /// <summary>
    /// Bắt đầu dungeon
    /// </summary>
    public void StartDungeon()
    {
        isDungeonActive = true;
        isDungeonComplete = false;
        currentWave = 0;
        totalExpGained = 0;

        Debug.Log($"[DungeonWave] Bắt đầu dungeon: {dungeonName}");

        // Bắt đầu wave đầu tiên
        StartCoroutine(StartWaveSequence());
    }

    /// <summary>
    /// Sequence bắt đầu wave: countdown → spawn
    /// </summary>
    private IEnumerator StartWaveSequence()
    {
        currentWave++;
        
        if (currentWave > totalWaves)
        {
            // Dungeon hoàn thành
            yield break;
        }

        // === HIỂN THỊ THÔNG BÁO WAVE ===
        ShowWaveNotification(currentWave);
        
        // Đợi 2 giây để đọc thông báo
        yield return new WaitForSeconds(2f);

        // === ĐẾM NGƯỢC ===
        isCountingDown = true;
        ShowCountdown(waveCountdownTime);

        float timer = waveCountdownTime;
        while (timer > 0)
        {
            countdownText.text = Mathf.Ceil(timer).ToString();
            yield return new WaitForSeconds(1f);
            timer--;
        }

        HideCountdown();
        isCountingDown = false;

        // === BẮT ĐẦU WAVE ===
        SpawnWave(currentWave);
        OnWaveStarted?.Invoke(currentWave);
    }

    /// <summary>
    /// Khi wave hoàn thành (kill hết enemy)
    /// </summary>
    private void OnWaveComplete()
    {
        isWaveActive = false;
        OnWaveCompleted?.Invoke(currentWave);

        Debug.Log($"[DungeonWave] Wave {currentWave} hoàn thành!");

        // Kiểm tra nếu là wave cuối cùng
        if (currentWave >= totalWaves)
        {
            // Dungeon hoàn thành!
            CompleteDungeon();
        }
        else
        {
            // Nghỉ giữa các wave rồi bắt đầu wave mới
            StartCoroutine(DelayBeforeNextWave());
        }
    }

    /// <summary>
    /// Delay trước khi bắt đầu wave tiếp theo
    /// </summary>
    private IEnumerator DelayBeforeNextWave()
    {
        if (waveDelayTime > 0)
        {
            statusText.text = $"Wave {currentWave} hoàn thành! Chuẩn bị wave {currentWave + 1}...";
            statusText.gameObject.SetActive(true);
            yield return new WaitForSeconds(waveDelayTime);
            statusText.gameObject.SetActive(false);
        }

        // Bắt đầu wave tiếp theo
        StartCoroutine(StartWaveSequence());
    }

    /// <summary>
    /// Khi player chết
    /// </summary>
    public void OnPlayerDied()
    {
        if (!isDungeonActive || isDungeonComplete) return;

        Debug.Log("[DungeonWave] Player đã chết!");
        
        // Dừng tất cả enemy
        StopAllEnemies();

        // Hiển thị UI thua
        ShowDungeonFailed();
        
        isDungeonActive = false;
        OnDungeonFailed?.Invoke();
    }

    /// <summary>
    /// Khi hoàn thành dungeon (kill hết boss wave 5)
    /// </summary>
    private void CompleteDungeon()
    {
        isDungeonComplete = true;
        isDungeonActive = false;

        Debug.Log($"[DungeonWave] Dungeon {dungeonName} hoàn thành! Tổng EXP: {totalExpGained}");

        // Hiển thị UI thắng
        ShowDungeonComplete();
        
        OnDungeonCompleted?.Invoke();
    }

    // ===== SPAWNING SYSTEM (CÁCH A - HỆ THỐNG MỚI) =====

    /// <summary>
    /// Spawn enemy cho wave hiện tại (CÁCH A - Dùng EnemyNew + GamePlayManager)
    /// Hệ thống mới: Wave 1-3 = Skelet, Wave 4 = Skelet + Lich, Wave 5 = Boss + Demon
    /// </summary>
    private void SpawnWave(int waveIndex)
    {
        int waveIdx = waveIndex - 1; // Array index (0-based)

        // Lấy số lượng enemy cho wave này (hệ thống mới)
        currentSkeletCount = waveIdx < skeletCount.Length ? skeletCount[waveIdx] : 0;
        currentLichCount = waveIdx < lichCount.Length ? lichCount[waveIdx] : 0;
        currentBossCount = waveIdx < bossCount.Length ? bossCount[waveIdx] : 0;
        currentDemonCount = waveIdx < demonCount.Length ? demonCount[waveIdx] : 0;

        int totalEnemies = currentSkeletCount + currentLichCount + currentBossCount + currentDemonCount;
        enemiesAlive = totalEnemies;

        Debug.Log($"[DungeonWave] Wave {waveIndex}: Skelet={currentSkeletCount} Lich={currentLichCount} Boss={currentBossCount} Demon={currentDemonCount} (Tổng: {totalEnemies})");

        // === CẤU HÌNH GAMEPLAY MANAGER (CÁCH A) ===
        // enemyType[0] = skelet (archers trong code cũ), [1] = monster (không dùng), [2] = lich, [3] = boss, [4] = demon
        ConfigureGamePlayManager(currentSkeletCount, 0, currentLichCount, currentBossCount, currentDemonCount);

        // Spawn từng enemy một
        for (int i = 0; i < totalEnemies; i++)
        {
            SpawnEnemyFromEnemyNew();
        }

        isWaveActive = true;
    }

    /// <summary>
    /// Cấu hình GamePlayManager trước khi spawn (CÁCH A - HỆ THỐNG MỚI)
    /// </summary>
    private void ConfigureGamePlayManager(int skelets, int monsters, int liches, int bosses, int demons)
    {
        // Reset static counters trong GamePlayManager
        GamePlayManager.archers = 0;
        GamePlayManager.monsteres = 0;
        GamePlayManager.lich = 0;
        GamePlayManager.boss = 0;
        GamePlayManager.demon = 0;

        // Cấu hình level.enemyType [skelet, monster(ignored), lich, boss, demon]
        // RandomEnemy sẽ đọc:
        // - enemyType[0] = skelet (Skeleton + skeleton_archer)
        // - enemyType[1] = monster (không dùng trong hệ thống mới)
        // - enemyType[2] = lich
        // - enemyType[3] = boss
        // - enemyType[4] = demon
        if (GamePlayManager.level.enemyType == null || GamePlayManager.level.enemyType.Length != 5)
        {
            GamePlayManager.level.enemyType = new int[5];
        }
        
        // Đặt theo thứ tự mới
        GamePlayManager.level.enemyType[0] = skelets; // Skelet
        GamePlayManager.level.enemyType[1] = monsters; // Monster (không dùng)
        GamePlayManager.level.enemyType[2] = liches;   // Lich
        GamePlayManager.level.enemyType[3] = bosses;  // Boss
        GamePlayManager.level.enemyType[4] = demons;   // Demon

        Debug.Log($"[DungeonWave] GamePlayManager configured: Skelet={skelets} Lich={liches} Boss={bosses} Demon={demons}");
    }

    /// <summary>
    /// Spawn 1 enemy từ EnemyNew prefab
    /// FIX: Không dùng static event broadcast nữa — gọi trực tiếp trên instance
    /// </summary>
    private void SpawnEnemyFromEnemyNew()
    {
        if (enemyNewPrefab == null || player == null)
        {
            Debug.LogWarning($"[DungeonWave] Không thể spawn enemy: enemyNewPrefab null hoặc player null");
            return;
        }

        Vector3 spawnPos = GetRandomSpawnPosition();

        // Instantiate EnemyNew
        GameObject enemy = Instantiate(enemyNewPrefab, spawnPos, Quaternion.identity);
        
        // === QUAN TRỌNG: Tắt tất cả enemy con trong prefab ===
        DisableAllChildEnemies(enemy);
        
        // Thiết lập SelectEnemyPos với vị trí spawn
        SetupSelectEnemyPosForAllEnemies(spawnPos);
        
        // Thêm EnemyWaveTracker
        EnemyWaveTracker tracker = enemy.AddComponent<EnemyWaveTracker>();
        tracker.waveManager = this;
        
        // === FIX: GỌI TRỰC TIẾP trên instance, KHÔNG broadcast static event ===
        // Static event EnemyEvent.EnemyEventSystem(0/2) sẽ fire trên TẤT CẢ enemy đã subscribe
        // Điều này khiến enemy trước đó bị re-trigger Enable() → reset vị trí, animation
        RandomEnemy randomEnemy = enemy.GetComponent<RandomEnemy>();
        if (randomEnemy != null)
        {
            // Gọi Enable trực tiếp trên instance này (không broadcast)
            randomEnemy.EnableDirect();
        }

        // SAU KHI enemy bên trong đã được kích hoạt
        // Thêm EnemyDeathBridge vào enemy bên TRONG (không phải EnemyNew parent)
        AddEnemyDeathBridgeToActiveEnemy(enemy);

        // === Set player target + Start Chase trực tiếp cho enemy ===
        SetPlayerTargetAndChaseForActiveEnemy(enemy);

        Debug.Log($"[DungeonWave] Spawned EnemyNew at {spawnPos} - AI activated (direct call)");
    }

    /// <summary>
    /// Set player target + bắt đầu chase TRỰC TIẾP cho enemy đang active
    /// Thay vì dùng EnemyEvent.AttackEvent (broadcast tới TẤT CẢ enemy)
    /// </summary>
    private void SetPlayerTargetAndChaseForActiveEnemy(GameObject enemyNew)
    {
        if (player == null) return;
        
        RandomEnemy randomEnemy = enemyNew.GetComponent<RandomEnemy>();
        if (randomEnemy == null || randomEnemy.enemys == null) return;

        // Tìm enemy đang active
        for (int i = 0; i < randomEnemy.enemys.Length; i++)
        {
            if (randomEnemy.enemys[i] != null && randomEnemy.enemys[i].activeSelf)
            {
                EnemyScript enemyScript = randomEnemy.enemys[i].GetComponent<EnemyScript>();
                if (enemyScript != null)
                {
                    // Set player target trực tiếp
                    enemyScript.SetPlayerTarget(player);
                    
                    // Bắt đầu chase TRỰC TIẾP (không qua EnemyEvent.AttackEvent broadcast)
                    enemyScript.StartChase();
                    
                    // Thêm Stuck Detection để tự warp khi bị kẹt trên terrain lồi lõm
                    if (randomEnemy.enemys[i].GetComponent<EnemyStuckDetection>() == null)
                    {
                        randomEnemy.enemys[i].AddComponent<EnemyStuckDetection>();
                    }
                }
                break; // Chỉ set cho enemy đang active
            }
        }
    }

    /// <summary>
    /// Thêm EnemyDeathBridge vào enemy đang active bên trong EnemyNew
    /// </summary>
    private void AddEnemyDeathBridgeToActiveEnemy(GameObject enemyNew)
    {
        RandomEnemy randomEnemy = enemyNew.GetComponent<RandomEnemy>();
        if (randomEnemy == null || randomEnemy.enemys == null) return;

        // Tìm enemy đang active
        for (int i = 0; i < randomEnemy.enemys.Length; i++)
        {
            if (randomEnemy.enemys[i] != null && randomEnemy.enemys[i].activeSelf)
            {
                GameObject activeEnemy = randomEnemy.enemys[i];
                
                // Kiểm tra xem đã có EnemyDeathBridge chưa
                if (activeEnemy.GetComponent<EnemyDeathBridge>() == null)
                {
                    activeEnemy.AddComponent<EnemyDeathBridge>();
                    Debug.Log($"[DungeonWave] Added EnemyDeathBridge to {activeEnemy.name}");
        }
                break; // Chỉ thêm vào một enemy đang active
            }
        }
    }

    /// <summary>
    /// Thiết lập SelectEnemyPos cho tất cả các enemy trong wave
    /// </summary>
    private void SetupSelectEnemyPosForAllEnemies(Vector3 spawnPos)
    {
        int count = 10;
        SelectEnemyPos.enemyTr = new Transform[count];

        // Tạo parent để quản lý
        GameObject parent = new GameObject("TempSpawnPoints");
        parent.transform.position = spawnPos;

        float radius = 5f;

        for (int i = 0; i < count; i++)
        {
            float angle = (Mathf.PI * 2f / count) * i;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;

            GameObject p = new GameObject($"TempSpawnPos_{i}");
            p.transform.position = spawnPos + offset;
            p.transform.parent = parent.transform;

            SelectEnemyPos.enemyTr[i] = p.transform;
        }

        // Destroy sau khi spawn xong để tránh memory leak
        Destroy(parent, 5f);
        
        Debug.Log($"[DungeonWave] Setup SelectEnemyPos với {count} vị trí tại {spawnPos}");
    }

    /// <summary>
    /// Tắt tất cả enemy con trong EnemyNew prefab
    /// </summary>
    private void DisableAllChildEnemies(GameObject enemyRoot)
    {
        // Tắt tất cả child objects (các enemy)
        for (int i = 0; i < enemyRoot.transform.childCount; i++)
        {
            Transform child = enemyRoot.transform.GetChild(i);
            // Không tắt RandomEnemy component
            if (child.GetComponent<RandomEnemy>() == null)
            {
                child.gameObject.SetActive(false);
            }
        }
        Debug.Log("[DungeonWave] Đã tắt tất cả child enemies trong prefab");
    }
    
    /// <summary>
    /// Thiết lập SelectEnemyPos với vị trí spawn
    /// </summary>
    private void SetupSelectEnemyPos(Vector3 spawnPos)
    {
        // Số vị trí spawn - khớp với kích thước mảng trong SelectEnemyPos
        int count = 10;

        // Xóa các temp objects cũ nếu có
        GameObject oldParent = GameObject.Find("TempSpawnPoints");
        if (oldParent != null)
        {
            Destroy(oldParent);
        }

        // Khởi tạo mảng mới với đủ số phần tử
        SelectEnemyPos.enemyTr = new Transform[count];

        // Tạo một parent để dễ quản lý
        GameObject parent = new GameObject("TempSpawnPoints");
        parent.transform.position = spawnPos;

        float radius = 5f; // bán kính vòng tròn quanh player

        for (int i = 0; i < count; i++)
        {
            // Tạo vị trí xung quanh player theo vòng tròn
            float angle = (Mathf.PI * 2f / count) * i;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;

            GameObject p = new GameObject($"TempSpawnPos_{i}");
            p.transform.position = spawnPos + offset;
            p.transform.parent = parent.transform;

            SelectEnemyPos.enemyTr[i] = p.transform;
        }

        Debug.Log($"[DungeonWave] Đã setup SelectEnemyPos với {count} vị trí quanh player tại {spawnPos}");
    }

    /// <summary>
    /// Theo dõi enemy type sau khi RandomEnemy chọn xong
    /// </summary>
    private IEnumerator TrackEnemyType(GameObject enemy, EnemyWaveTracker tracker)
    {
        // Đợi RandomEnemy chạy Awake/Start
        yield return new WaitForSeconds(0.1f);

        // Tìm child object đang active để xác định loại enemy
        int enemyType = 0; // mặc định skeleton
        
        Transform enemyRoot = enemy.transform;
        for (int i = 0; i < enemyRoot.childCount; i++)
        {
            Transform child = enemyRoot.GetChild(i);
            string childName = child.name.ToLower();
            
            if (child.gameObject.activeSelf)
            {
                if (childName.Contains("skeleton"))
                    enemyType = 0;
                else if (childName.Contains("archer") || childName.Contains("skeleton_archer"))
                    enemyType = 1;
                else if (childName.Contains("monster"))
                    enemyType = 2;
                else if (childName.Contains("lich"))
                    enemyType = 3;
                else if (childName.Contains("boss"))
                    enemyType = 4;
                else if (childName.Contains("demon"))
                    enemyType = 5;
                
                break;
            }
        }

        tracker.enemyType = enemyType;
        Debug.Log($"[DungeonWave] Enemy type identified: {enemyType}");
    }

    /// <summary>
    /// Tìm vị trí spawn ngẫu nhiên hợp lệ
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        for (int attempt = 0; attempt < spawnAttemptCount; attempt++)
        {
            // Tạo vị trí ngẫu nhiên trong hình tròn
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector3 candidatePos = player.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Kiểm tra khoảng cách từ player
            float distFromPlayer = Vector3.Distance(candidatePos, player.position);
            if (distFromPlayer < minDistanceFromPlayer) continue;

            // Kiểm tra va chạm với vật cản
            if (!IsPositionValid(candidatePos)) continue;

            // Thành công!
            return candidatePos;
        }

        // Nếu không tìm được vị trí hợp lệ sau nhiều lần thử
        // Spawn tại vị trí ngẫu nhiên đơn giản
        Vector2 fallbackPos = UnityEngine.Random.insideUnitCircle.normalized * spawnRadius;
        return player.position + new Vector3(fallbackPos.x, 0, fallbackPos.y);
    }

    /// <summary>
    /// Kiểm tra vị trí có hợp lệ không (không trong tường)
    /// </summary>
    private bool IsPositionValid(Vector3 position)
    {
        // Kiểm tra va chạm tại vị trí
        Collider[] colliders = Physics.OverlapSphere(position, 1f, obstacleLayer);
        
        // Nếu có collider trong layer obstacle thì không hợp lệ
        return colliders.Length == 0;
    }

    // ===== ENEMY TRACKING =====

    /// <summary>
    /// Gọi khi 1 enemy chết
    /// </summary>
    public void OnEnemyKilled(int enemyType, int expValue)
    {
        if (!isDungeonActive) return;

        enemiesAlive--;
        totalExpGained += expValue;

        Debug.Log($"[DungeonWave] Enemy type {enemyType} died. Còn lại: {enemiesAlive}. EXP: {expValue}");
    }

    // ===== UI METHODS =====

    private void HideAllUI()
    {
        if (waveNotificationUI) waveNotificationUI.SetActive(false);
        if (countdownUI) countdownUI.SetActive(false);
        if (dungeonCompleteUI) dungeonCompleteUI.SetActive(false);
        if (dungeonFailedUI) dungeonFailedUI.SetActive(false);
        if (statusText) statusText.gameObject.SetActive(false);
    }

    private void ShowWaveNotification(int wave)
    {
        if (waveNotificationUI)
        {
            waveNotificationUI.SetActive(true);
            
            string waveName = "";
            if (wave == totalWaves - 1)
                waveName = "BOSS WAVE";
            else if (wave == totalWaves)
                waveName = "FINAL BOSS";
            else
                waveName = $"WAVE {wave}";

            if (waveNameText)
                waveNameText.text = waveName;

            StartCoroutine(HideWaveNotificationAfterDelay(2f));
        }
    }

    private IEnumerator HideWaveNotificationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (waveNotificationUI) waveNotificationUI.SetActive(false);
    }

    private void ShowCountdown(float time)
    {
        if (countdownUI)
        {
            countdownUI.SetActive(true);
            countdownText.text = Mathf.Ceil(time).ToString();
        }
    }

    private void HideCountdown()
    {
        if (countdownUI) countdownUI.SetActive(false);
    }

    private void ShowDungeonComplete()
    {
        if (dungeonCompleteUI)
        {
            dungeonCompleteUI.SetActive(true);
            
            if (expRewardText)
                expRewardText.text = $"EXP nhận được: {totalExpGained}";
        }
    }

    private void ShowDungeonFailed()
    {
        if (dungeonFailedUI)
        {
            dungeonFailedUI.SetActive(true);
        }
    }

    // ===== PUBLIC METHODS =====

    /// <summary>
    /// Quay về main map (gọi từ button)
    /// </summary>
    public void ReturnToMainMap()
    {
        // TODO: Load scene main map
        // SceneManager.LoadScene("MainMap");
        Debug.Log("[DungeonWave] Quay về main map...");
    }

    /// <summary>
    /// Restart dungeon
    /// </summary>
    public void RestartDungeon()
    {
        // Reload scene hiện tại
        // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log("[DungeonWave] Restart dungeon...");
    }

    // ===== DEBUG / TEST =====

    private void KillAllEnemiesForTest()
    {
        EnemyWaveTracker[] enemies = FindObjectsOfType<EnemyWaveTracker>();
        foreach (var e in enemies)
        {
            EnemyScript enemyScript = e.GetComponent<EnemyScript>();
            if (enemyScript != null && enemyScript.alive)
            {
                // Set health to 0 to trigger death
                enemyScript.enemy.helth.value = 0;
            }
        }
    }

    private void StopAllEnemies()
    {
        EnemyScript[] enemies = FindObjectsOfType<EnemyScript>();
        foreach (var enemy in enemies)
        {
            enemy.enabled = false;
            if (enemy.navMeshAgent != null)
                enemy.navMeshAgent.isStopped = true;
        }
    }

    /// <summary>
    /// Force chuyển sang wave tiếp theo (debug)
    /// </summary>
    private void ForceNextWave()
    {
        if (isDungeonActive && isWaveActive)
        {
            KillAllEnemiesForTest();
        }
    }

    /// <summary>
    /// Jump to specific wave (debug)
    /// </summary>
    private void GoToWave(int wave)
    {
        if (wave < 1 || wave > totalWaves) return;
        
        // Kill all current enemies first
        KillAllEnemiesForTest();
        
        // Reset wave counter
        currentWave = wave - 1;
        
        // Start new wave
        StartCoroutine(StartWaveSequence());
    }

    // ===== GETTERS =====

    public int CurrentWave => currentWave;
    public int TotalWaves => totalWaves;
    public int EnemiesAlive => enemiesAlive;
    public bool IsDungeonActive => isDungeonActive;
    public bool IsWaveActive => isWaveActive;
    public int TotalExpGained => totalExpGained;

    // ===== CÁC PHƯƠNG THỨC CHO GAMEPLAY MANAGER =====

    /// <summary>
    /// Thiết lập wave hiện tại (được gọi từ GamePlayManager.Arenalevel)
    /// </summary>
    public void SetWave(int wave)
    {
        currentWave = Mathf.Clamp(wave, 1, totalWaves);
        
        if (showDebugLog)
            Debug.Log($"[DungeonWaveManager] SetWave: {currentWave}");
    }

    /// <summary>
    /// Lấy số lượng enemy theo từng loại cho wave hiện tại
    /// </summary>
    public void GetEnemyCounts(out int skeletOut, out int lichOut, out int bossOut, out int demonOut)
    {
        int waveIdx = Mathf.Clamp(currentWave - 1, 0, totalWaves - 1);
        
        skeletOut = waveIdx < skeletCount.Length ? skeletCount[waveIdx] : 0;
        lichOut = waveIdx < lichCount.Length ? lichCount[waveIdx] : 0;
        bossOut = waveIdx < bossCount.Length ? bossCount[waveIdx] : 0;
        demonOut = waveIdx < demonCount.Length ? demonCount[waveIdx] : 0;
    }

    /// <summary>
    /// Lấy số lượng enemy còn lại có thể spawn (dùng trong RandomEnemy - KHÔNG reset counters)
    /// </summary>
    public void GetRemainingEnemyCounts(out int skeletOut, out int lichOut, out int bossOut, out int demonOut)
    {
        skeletOut = Mathf.Max(0, currentSkeletCount - GamePlayManager.archers);
        lichOut = Mathf.Max(0, currentLichCount - GamePlayManager.lich);
        bossOut = Mathf.Max(0, currentBossCount - GamePlayManager.boss);
        demonOut = Mathf.Max(0, currentDemonCount - GamePlayManager.demon);
    }
}

/// <summary>
/// Script theo dõi enemy trong wave
/// </summary>
public class EnemyWaveTracker : MonoBehaviour
{
    public DungeonWaveManager waveManager;
    public int enemyType;
    private bool isDead = false;

    void Start()
    {
        // Đăng ký theo dõi
        StartCoroutine(TrackEnemy());
    }

    IEnumerator TrackEnemy()
    {
        // Chờ đợi enemy chết hoặc bị destroy
        while (gameObject != null && !isDead)
        {
            EnemyScript es = GetComponent<EnemyScript>();
            if (es != null && !es.alive)
            {
                OnEnemyDeath();
                break;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void OnEnemyDeath()
    {
        if (isDead) return;
        isDead = true;

        // Tính EXP theo loại enemy
        int exp = 0;
        switch (enemyType)
        {
            case 0: exp = 100; break;   // Skeleton
            case 1: exp = 150; break;   // Archer
            case 2: exp = 300; break;   // Monster
            case 3: exp = 350; break;   // Lich
            case 4: exp = 1500; break;  // Boss
            case 5: exp = 3000; break;  // Demon
        }

        if (waveManager != null)
        {
            waveManager.OnEnemyKilled(enemyType, exp);
        }
    }
}
