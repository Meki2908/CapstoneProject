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

    [Header("=== ENEMY COUNT PER WAVE (CÁCH A) ===")]
    [Tooltip("Số lượng Archer mỗi wave [wave1, wave2, wave3, wave4, wave5]")]
    public int[] archerCount = { 1, 2, 3, 2, 1 };
    [Tooltip("Số lượng Monster mỗi wave")]
    public int[] monsterCount = { 0, 1, 2, 3, 2 };
    [Tooltip("Số lượng Lich mỗi wave")]
    public int[] lichCount = { 0, 0, 1, 1, 2 };
    [Tooltip("Số lượng Boss mỗi wave")]
    public int[] bossCount = { 0, 0, 0, 1, 0 };
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

    // Events
    public System.Action<int> OnWaveStarted;
    public System.Action<int> OnWaveCompleted;
    public System.Action OnDungeonCompleted;
    public System.Action OnDungeonFailed;

    // EXP tracking
    private int totalExpGained = 0;

    // ===== UNITY LIFECYCLE =====

    void Start()
    {
        // Tính tổng số enemy mỗi wave
        CalculateTotalEnemiesPerWave();

        // Tìm player nếu chưa gán
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

        // THIẾT LẬP CÁC REFERENCES CẦN THIẾT CHO DUNGEONMANIA
        SetupDungeonManiaReferences();

        // Ẩn tất cả UI
        HideAllUI();

        // Bắt đầu dungeon
        StartDungeon();
    }

    /// <summary>
    /// Thiết lập các references cần thiết cho DungeonMania enemy
    /// </summary>
    private void SetupDungeonManiaReferences()
    {
        // 1. Tạo object "player" (chữ thường) nếu chưa có
        // EnemyScript tìm "player" (chữ thường)
        GameObject playerLower = GameObject.Find("player");
        if (playerLower == null && player != null)
        {
            playerLower = new GameObject("player");
            playerLower.transform.position = player.position;
            playerLower.transform.rotation = player.rotation;
            // Làm child của player thật để di chuyển cùng
            playerLower.transform.SetParent(player);
            Debug.Log("[DungeonWave] Created 'player' reference object");
        }

        // 2. Tạo GameManager object nếu chưa có
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager == null)
        {
            gameManager = new GameObject("GameManager");
            Debug.Log("[DungeonWave] Created GameManager object");
            
            // Thêm các component cần thiết
            if (gameManager.GetComponent<AudioManager>() == null)
                gameManager.AddComponent<AudioManager>();
            if (gameManager.GetComponent<HeroInformation>() == null)
                gameManager.AddComponent<HeroInformation>();
            if (gameManager.GetComponent<GameController>() == null)
                gameManager.AddComponent<GameController>();
        }
        
        // 3. Đảm bảo PlayerManager component tồn tại trên player
        if (player != null && player.GetComponent<PlayerManager>() == null)
        {
            player.gameObject.AddComponent<PlayerManager>();
            Debug.Log("[DungeonWave] Added PlayerManager to player");
        }

        Debug.Log("[DungeonWave] DungeonMania references setup complete");
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
            int archers = i < archerCount.Length ? archerCount[i] : 0;
            int monsters = i < monsterCount.Length ? monsterCount[i] : 0;
            int liches = i < lichCount.Length ? lichCount[i] : 0;
            int bosses = i < bossCount.Length ? bossCount[i] : 0;
            int demons = i < demonCount.Length ? demonCount[i] : 0;
            totalEnemiesPerWave[i] = archers + monsters + liches + bosses + demons;
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

    // ===== SPAWNING SYSTEM (CÁCH A) =====

    /// <summary>
    /// Spawn enemy cho wave hiện tại (CÁCH A - Dùng EnemyNew + GamePlayManager)
    /// </summary>
    private void SpawnWave(int waveIndex)
    {
        int waveIdx = waveIndex - 1; // Array index (0-based)

        // Lấy số lượng enemy cho wave này
        int archers = waveIdx < archerCount.Length ? archerCount[waveIdx] : 0;
        int monsters = waveIdx < monsterCount.Length ? monsterCount[waveIdx] : 0;
        int liches = waveIdx < lichCount.Length ? lichCount[waveIdx] : 0;
        int bosses = waveIdx < bossCount.Length ? bossCount[waveIdx] : 0;
        int demons = waveIdx < demonCount.Length ? demonCount[waveIdx] : 0;

        int totalEnemies = archers + monsters + liches + bosses + demons;
        enemiesAlive = totalEnemies;

        Debug.Log($"[DungeonWave] Wave {waveIndex}: A={archers} M={monsters} L={liches} B={bosses} D={demons} (Tổng: {totalEnemies})");

        // === CẤU HÌNH GAMEPLAY MANAGER (CÁCH A) ===
        ConfigureGamePlayManager(archers, monsters, liches, bosses, demons);

        // Spawn từng enemy một
        for (int i = 0; i < totalEnemies; i++)
        {
            SpawnEnemyFromEnemyNew();
        }

        isWaveActive = true;
    }

    /// <summary>
    /// Cấu hình GamePlayManager trước khi spawn (CÁCH A)
    /// </summary>
    private void ConfigureGamePlayManager(int archers, int monsters, int liches, int bosses, int demons)
    {
        // Reset static counters trong GamePlayManager
        GamePlayManager.archers = 0;
        GamePlayManager.monsteres = 0;
        GamePlayManager.lich = 0;
        GamePlayManager.boss = 0;
        GamePlayManager.demon = 0;

        // Cấu hình level.enemyType [archers, monsters, liches, boss, demon]
        // Theo thứ tự trong RandomEnemy: index 0=skeleton, 1=archer, 2=monster, 3=lich, 4=boss
        // Nhưng GamePlayManager.level.enemyType có 5 phần tử
        if (GamePlayManager.level.enemyType == null || GamePlayManager.level.enemyType.Length != 5)
        {
            GamePlayManager.level.enemyType = new int[5];
        }
        
        // Đặt theo thứ tự: [skeleton, archer, monster, lich, boss] + demon riêng
        // Trong GamePlayManager: enemyType[0]=archer, [1]=monster, [2]=lich, [3]=boss, [4]=demon
        GamePlayManager.level.enemyType[0] = 0; // skeleton (không dùng trong dungeon này)
        GamePlayManager.level.enemyType[1] = archers;
        GamePlayManager.level.enemyType[2] = monsters;
        GamePlayManager.level.enemyType[3] = liches;
        GamePlayManager.level.enemyType[4] = bosses + demons; // boss và demon dùng cùng slot

        Debug.Log($"[DungeonWave] GamePlayManager configured: A={archers} M={monsters} L={liches} B={bosses} D={demons}");
    }

    /// <summary>
    /// Spawn 1 enemy từ EnemyNew prefab (CÁCH A)
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
        
        // Thiết lập SelectEnemyPos với vị trí spawn trước khi gọi RandomEnemy.Enable()
        // Tạo một mảng các Transform tạm để RandomEnemy sử dụng
        SetupSelectEnemyPos(spawnPos);
        
        // Thêm script theo dõi enemy
        EnemyWaveTracker tracker = enemy.AddComponent<EnemyWaveTracker>();
        tracker.waveManager = this;
        
        // GỌI RANDOMENEMY.ENABLE() ĐỂ KÍCH HOẠT ENEMY BÊN TRONG
        RandomEnemy randomEnemy = enemy.GetComponent<RandomEnemy>();
        if (randomEnemy != null)
        {
            // Reset childNumber để nó chọn đúng vị trí
            // (childNumber được set trong RandomEnemy.Awake() từ sibling index)
            
            randomEnemy.Enable();
            Debug.Log($"[DungeonWave] Đã gọi RandomEnemy.Enable()");
        }
        else
        {
            Debug.LogWarning($"[DungeonWave] EnemyNew không có RandomEnemy component!");
        }

        // Xác định enemy type để tính EXP (sẽ được xác định bởi RandomEnemy)
        // Đợi một chút để RandomEnemy chọn xong
        StartCoroutine(TrackEnemyType(enemy, tracker));

        Debug.Log($"[DungeonWave] Spawned EnemyNew at {spawnPos}");
    }
    
    /// <summary>
    /// Thiết lập SelectEnemyPos với vị trí spawn
    /// </summary>
    private void SetupSelectEnemyPos(Vector3 spawnPos)
    {
        // Số vị trí spawn - khớp với kích thước mảng trong SelectEnemyPos
        int count = 10;

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
