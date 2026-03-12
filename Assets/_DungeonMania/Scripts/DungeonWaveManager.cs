using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using Unity.Cinemachine;

public class DungeonWaveManager : MonoBehaviour
{
    [Header("=== DUNGEON SETTINGS ===")]
    [Tooltip("Tên dungeon để hiển thị")]
    public string dungeonName = "Dungeon 1";
    [Tooltip("Tổng số wave trong dungeon")]
    public int totalWaves = 5;
    [Tooltip("Map type: 0=Desert, 1=Swamp, 2=Hell")]
    public int mapType = 0;
    
    [Header("=== SCENE TRANSITION ===")]
    [Tooltip("Tên scene map chính để quay về")]
    public string mainMapSceneName = "Map_Chinh";
    [Tooltip("Thời gian chờ trước khi tự động quay về map khi THẮNG (giây)")]
    public float returnDelayOnWin = 5f;
    [Tooltip("Thời gian chờ trước khi tự động quay về map khi THUA (giây)")]
    public float returnDelayOnLose = 3f;

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
    
    [Tooltip("Số lượng Monster (Orc, Troll, Guul) mỗi wave")]
    public int[] monsterCount = { 0, 0, 0, 0, 0 };
    
    [Tooltip("Tổng số enemy mỗi wave (tự tính)")]
    public int[] totalEnemiesPerWave;

    [Header("=== PREFAB (CÁCH A) ===")]
    [Tooltip("Prefab EnemyNew (chứa tất cả enemy bên trong)")]
    public GameObject enemyNewPrefab;

    [Header("=== ITEM DROP CONFIG (theo từng Enemy Type) ===")]
    [Tooltip("Có drop EXP orb không")]
    public bool dropExpOrb = true;

    [Tooltip("Số item tối đa rơi mỗi enemy")]
    public int maxDropsPerEnemy = 3;

    [Tooltip("Drop table cho Skeleton/Archer")]
    public List<DungeonDropEntry> skeletDrops = new List<DungeonDropEntry>();

    [Tooltip("Drop table cho Monster (Orc, Troll, Guul)")]
    public List<DungeonDropEntry> monsterDrops = new List<DungeonDropEntry>();

    [Tooltip("Drop table cho Lich")]
    public List<DungeonDropEntry> lichDrops = new List<DungeonDropEntry>();

    [Tooltip("Drop table cho Boss (Stoneogre, Golem, Minotaur, Ifrit)")]
    public List<DungeonDropEntry> bossDrops = new List<DungeonDropEntry>();

    [Tooltip("Drop table cho Demon")]
    public List<DungeonDropEntry> demonDrops = new List<DungeonDropEntry>();

    [System.Serializable]
    public class DungeonDropEntry
    {
        [Tooltip("Kéo Item ScriptableObject vào đây")]
        public Item item;
        [Tooltip("Số lượng tối thiểu")]
        public int minQuantity = 1;
        [Tooltip("Số lượng tối đa")]
        public int maxQuantity = 1;
    }

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
    private int currentMonsterCount = 0;
    
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
            else
            {
                Debug.LogError("[DungeonWave] PLAYER NOT FOUND! Enemy sẽ không tìm được target!");
            }
        }

        // LOG: Kiểm tra tất cả references
        Debug.Log($"[DungeonWave] === REFERENCE CHECK ===\n" +
            $"  player: {(player != null ? player.name : "NULL!")}\n" +
            $"  enemyNewPrefab: {(enemyNewPrefab != null ? enemyNewPrefab.name : "NULL!")}\n" +
            $"  waveNotificationUI: {(waveNotificationUI != null ? "OK" : "NULL")}\n" +
            $"  waveNameText: {(waveNameText != null ? "OK" : "NULL")}\n" +
            $"  countdownUI: {(countdownUI != null ? "OK" : "NULL")}\n" +
            $"  countdownText: {(countdownText != null ? "OK" : "NULL")}\n" +
            $"  statusText: {(statusText != null ? "OK" : "NULL")}");

        // Tính tổng số enemy mỗi wave
        CalculateTotalEnemiesPerWave();

        // THIẾT LẬP CÁC REFERENCES CẦN THIẾT CHO DUNGEONMANIA (SAU KHI TÌM ĐƯỢC PLAYER)
        SetupPlayerReference();

        // ĐẢM BẢO tất cả parent objects của UI đều active (GUI_Dungeon có thể bị tắt mặc định)
        EnsureUIParentsActive();

        // Ẩn tất cả UI
        HideAllUI();

        // Reset reward tracking cho dungeon mới
        if (DungeonRewardUI.Instance != null)
        {
            DungeonRewardUI.Instance.ClearTrackedItems();
        }

        // Bắt đầu dungeon
        StartDungeon();
    }

    /// <summary>
    /// Đảm bảo tất cả parent objects trong hierarchy của UI elements đều active
    /// GUI_Dungeon hoặc Panels_GUI_Play có thể bị tắt mặc định trong Inspector
    /// Nếu parent bị tắt, SetActive(true) trên child sẽ KHÔNG hiện UI
    /// </summary>
    private void EnsureUIParentsActive()
    {
        // Collect tất cả UI references để check parent chain
        GameObject[] uiElements = new GameObject[] {
            waveNotificationUI,
            countdownUI,
            dungeonCompleteUI,
            dungeonFailedUI,
            waveNameText != null ? waveNameText.gameObject : null,
            countdownText != null ? countdownText.gameObject : null,
            expRewardText != null ? expRewardText.gameObject : null,
            statusText != null ? statusText.gameObject : null
        };

        foreach (GameObject ui in uiElements)
        {
            if (ui == null) continue;

            // Đi ngược lên parent chain và bật tất cả
            Transform parent = ui.transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf)
                {
                    Debug.Log($"[DungeonWave] Activating disabled parent: {parent.name}");
                    parent.gameObject.SetActive(true);
                }

                // Dừng khi gặp Canvas (không bật parent ngoài Canvas)
                if (parent.GetComponent<Canvas>() != null)
                    break;

                parent = parent.parent;
            }
        }

        Debug.Log("[DungeonWave] UI parent chain activated");

        // === CANVAS FIX AND DIAGNOSTIC ===
        FixAndDiagnoseCanvas();
    }

    /// <summary>
    /// Tìm và SỬA CÁC VẤN ĐỀ CANVAS phổ biến gây UI active nhưng không hiển thị:
    /// 1. Canvas renderMode = ScreenSpaceCamera nhưng camera NULL → chuyển sang Overlay
    /// 2. CanvasGroup alpha = 0 → set alpha = 1
    /// 3. Canvas disabled → enabled
    /// 4. Sorting order quá thấp → tăng lên
    /// </summary>
    private void FixAndDiagnoseCanvas()
    {
        if (waveNotificationUI == null) return;

        // Tìm Canvas chứa UI
        Canvas canvas = waveNotificationUI.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"[DungeonWave] === CANVAS DIAGNOSTIC ===\n" +
                $"  Canvas: {canvas.gameObject.name}\n" +
                $"  renderMode: {canvas.renderMode}\n" +
                $"  sortingOrder: {canvas.sortingOrder}\n" +
                $"  enabled: {canvas.enabled}\n" +
                $"  worldCamera: {(canvas.worldCamera != null ? canvas.worldCamera.name : "NULL")}");

            // === FIX 1: Canvas renderMode ===
            // Nếu Canvas dùng ScreenSpaceCamera nhưng KHÔNG có camera → không render gì
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
            {
                Debug.LogWarning("[DungeonWave] FIX: Canvas is ScreenSpaceCamera but camera is NULL! Switching to ScreenSpaceOverlay");
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            // === FIX 2: Canvas bị disabled ===
            if (!canvas.enabled)
            {
                Debug.LogWarning("[DungeonWave] FIX: Canvas was disabled! Enabling...");
                canvas.enabled = true;
            }

            // === FIX 3: Sorting order - đảm bảo dungeon UI render trên các UI khác ===
            if (canvas.sortingOrder < 10)
            {
                Debug.Log($"[DungeonWave] FIX: Setting Canvas sortingOrder from {canvas.sortingOrder} to 100");
                canvas.sortingOrder = 100;
            }

            // === FIX 4: Kiểm tra và fix CanvasGroup alpha ===
            CanvasGroup[] groups = waveNotificationUI.GetComponentsInParent<CanvasGroup>(true);
            foreach (var group in groups)
            {
                if (group.alpha < 0.01f)
                {
                    Debug.LogWarning($"[DungeonWave] FIX: CanvasGroup on '{group.gameObject.name}' has alpha={group.alpha}! Setting to 1");
                    group.alpha = 1f;
                }
                Debug.Log($"[DungeonWave] CanvasGroup on '{group.gameObject.name}': alpha={group.alpha}");
            }
        }
        else
        {
            Debug.LogWarning("[DungeonWave] NO Canvas found in parent chain! Creating overlay Canvas...");
            // Tạo Canvas mới nếu không tìm thấy
            Canvas newCanvas = waveNotificationUI.transform.root.gameObject.AddComponent<Canvas>();
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            newCanvas.sortingOrder = 100;
            waveNotificationUI.transform.root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Check RectTransform
        RectTransform rect = waveNotificationUI.GetComponent<RectTransform>();
        if (rect != null)
        {
            Debug.Log($"[DungeonWave] WaveNotification: pos={rect.anchoredPosition}, size={rect.sizeDelta}, scale={rect.localScale}");
            
            // === FIX 5: Scale bằng 0 ===
            if (rect.localScale.x < 0.01f || rect.localScale.y < 0.01f)
            {
                Debug.LogWarning("[DungeonWave] FIX: WaveNotification scale is nearly 0! Setting to 1,1,1");
                rect.localScale = Vector3.one;
            }
        }

        // Check text
        if (waveNameText != null)
        {
            Debug.Log($"[DungeonWave] WaveNameText: color={waveNameText.color}, fontSize={waveNameText.fontSize}, enabled={waveNameText.enabled}");
            // Fix alpha = 0 trên text
            if (waveNameText.color.a < 0.01f)
            {
                Debug.LogWarning("[DungeonWave] FIX: WaveNameText color alpha is 0! Setting to white");
                waveNameText.color = Color.white;
            }
        }
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
        
        // Đăng ký nhận sự kiện player chết để hiện UI thua + quay về map
        if (player != null)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph == null) ph = player.GetComponentInChildren<PlayerHealth>();
            if (ph != null)
            {
                ph.OnPlayerDied -= OnPlayerDied; // Tránh đăng ký 2 lần
                ph.OnPlayerDied += OnPlayerDied;
                Debug.Log("[DungeonWave] Subscribed to PlayerHealth.OnPlayerDied");
            }
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

        // Debug keys removed (were: K=kill all, N=next wave, 1-5=jump to wave)
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
            int monsters = i < monsterCount.Length ? monsterCount[i] : 0;
            totalEnemiesPerWave[i] = skelets + liches + bosses + demons + monsters;
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

        Debug.Log($"[DungeonWave] === Starting Wave Sequence for Wave {currentWave} ===");

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
            // NULL CHECK: Tránh crash nếu UI bị mất reference sau merge
            if (countdownText != null)
            {
                countdownText.text = Mathf.Ceil(timer).ToString();
            }
            yield return new WaitForSeconds(1f);
            timer--;
        }

        HideCountdown();
        isCountingDown = false;

        // === BẮT ĐẦU WAVE ===
        Debug.Log($"[DungeonWave] Spawning Wave {currentWave}...");
        SpawnWave(currentWave);
        OnWaveStarted?.Invoke(currentWave);
        Debug.Log($"[DungeonWave] Wave {currentWave} spawned. enemiesAlive={enemiesAlive}, isWaveActive={isWaveActive}");
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
        if (waveDelayTime > 0 && statusText != null)
        {
            statusText.text = $"Wave {currentWave} hoàn thành! Chuẩn bị wave {currentWave + 1}...";
            statusText.gameObject.SetActive(true);
            yield return new WaitForSeconds(waveDelayTime);
            statusText.gameObject.SetActive(false);
        }
        else if (waveDelayTime > 0)
        {
            // statusText null nhưng vẫn cần delay
            yield return new WaitForSeconds(waveDelayTime);
        }

        // Hồi đầy máu cho player khi sang wave mới
        HealPlayerFull();

        // Bắt đầu wave tiếp theo
        StartCoroutine(StartWaveSequence());
    }

    /// <summary>
    /// Hồi đầy máu player giữa các wave
    /// </summary>
    private void HealPlayerFull()
    {
        if (player == null) return;

        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph == null) ph = player.GetComponentInChildren<PlayerHealth>();
        
        if (ph != null)
        {
            ph.ResetHealth();
            Debug.Log("[DungeonWave] Player đã được hồi đầy máu cho wave mới!");
        }
    }

    /// <summary>
    /// Khi player chết
    /// </summary>
    [Header("=== DEATH ANIMATION ===")]
    [Tooltip("Thời gian chờ animation chết của player trước khi hiện GUI Failed (khớp với DieState.dieDuration)")]
    public float deathAnimationDelay = 3f;

    public void OnPlayerDied()
    {
        if (!isDungeonActive || isDungeonComplete) return;

        Debug.Log("[DungeonWave] Player đã chết! Đợi animation chết xong...");
        
        isDungeonActive = false;

        // === NGAY LẬP TỨC: Chặn input nhưng KHÔNG tắt CharacterController ===
        // DieState.PhysicsUpdate() cần CharacterController để chạy gravity cho animation chết
        if (player != null)
        {
            // Tắt PlayerInput → player không nhận input di chuyển/tấn công
            UnityEngine.InputSystem.PlayerInput pi = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (pi != null)
            {
                pi.enabled = false;
                Debug.Log("[DungeonWave] PlayerInput DISABLED");
            }
        }

        // Chặn xoay camera (mouse) nhưng camera vẫn follow player cho animation chết
        DisableCameraRotation();
        
        // Đợi animation chết chạy xong rồi mới hiện GUI
        StartCoroutine(DelayedDungeonFailed());
    }

    /// <summary>
    /// Đợi animation chết chạy xong rồi mới dừng enemy + hiện GUI Failed
    /// </summary>
    private IEnumerator DelayedDungeonFailed()
    {
        // Đợi animation chết player chạy xong (3s = DieState.dieDuration)
        yield return new WaitForSeconds(deathAnimationDelay);

        Debug.Log("[DungeonWave] Animation chết xong → hiện GUI Failed");

        // Animation xong → giờ mới tắt CharacterController (an toàn)
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
        }

        // Dừng tất cả enemy
        StopAllEnemies();

        // Hiển thị UI thua
        ShowDungeonFailed();
        
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

        // Khóa player không cho di chuyển/đánh khi GUI Complete hiện
        if (player != null)
        {
            UnityEngine.InputSystem.PlayerInput pi = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (pi != null) pi.enabled = false;

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
        }

        // Delay 3 giây rồi mới hiện GUI
        StartCoroutine(DelayedShowCompleteUI());
    }

    private System.Collections.IEnumerator DelayedShowCompleteUI()
    {
        yield return new WaitForSeconds(3f);

        // Ẩn UI_HP và UI_Inventory của player
        HidePlayerUIOnComplete();

        // Hiển thị UI thắng
        ShowDungeonComplete();

        // Hiển thị Reward Panel
        if (DungeonRewardUI.Instance != null)
        {
            DungeonRewardUI.Instance.ShowRewardPanel();
        }

        OnDungeonCompleted?.Invoke();
    }

    /// <summary>
    /// Ẩn UI_HP và UI_Inventory khi dungeon hoàn thành
    /// </summary>
    private void HidePlayerUIOnComplete()
    {
        string[] uiNames = { "UI_HP_Invetory", "UI_HP_Inventory", "UI_HP", "UI_Invetory", "UI_Inventory" };

        // Cách 1: Tìm trong player hierarchy (recursive)
        Transform searchRoot = player;
        if (searchRoot == null)
        {
            GameObject pr = GameObject.Find("PlayerRoot");
            if (pr != null) searchRoot = pr.transform;
        }

        if (searchRoot != null)
        {
            foreach (string n in uiNames)
            {
                Transform t = FindInChildren(searchRoot, n);
                if (t != null) { t.gameObject.SetActive(false); Debug.Log($"[DungeonWave] Hidden {n} (in player)"); return; }
            }
        }

        // Cách 2: Fallback — tìm toàn scene
        foreach (string n in uiNames)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) { go.SetActive(false); Debug.Log($"[DungeonWave] Hidden {n} (scene)"); return; }
        }

        Debug.LogWarning("[DungeonWave] Could not find UI_HP_Invetory to hide!");
    }

    /// <summary>
    /// Tìm child theo tên trong toàn bộ hierarchy (recursive)
    /// </summary>
    private Transform FindInChildren(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindInChildren(child, name);
            if (found != null) return found;
        }
        return null;
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
        currentMonsterCount = waveIdx < monsterCount.Length ? monsterCount[waveIdx] : 0;

        int totalEnemies = currentSkeletCount + currentLichCount + currentBossCount + currentDemonCount + currentMonsterCount;
        enemiesAlive = totalEnemies;

        Debug.Log($"[DungeonWave] Wave {waveIndex}: Skelet={currentSkeletCount} Monster={currentMonsterCount} Lich={currentLichCount} Boss={currentBossCount} Demon={currentDemonCount} (Tổng: {totalEnemies})");

        // === CẤU HÌNH GAMEPLAY MANAGER (CÁCH A) ===
        // enemyType[0] = skelet, [1] = monster, [2] = lich, [3] = boss, [4] = demon
        ConfigureGamePlayManager(currentSkeletCount, currentMonsterCount, currentLichCount, currentBossCount, currentDemonCount);

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

        Debug.Log($"[DungeonWave] GamePlayManager configured: Skelet={skelets} Monster={monsters} Lich={liches} Boss={bosses} Demon={demons}");
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

                // Thêm ItemDropSpawner để enemy rơi item khi chết
                if (activeEnemy.GetComponent<ItemDropSpawner>() == null)
                {
                    var spawner = activeEnemy.AddComponent<ItemDropSpawner>();
                    
                    // Chọn drop table theo enemy type
                    var enemyScript = activeEnemy.GetComponent<EnemyScript>();
                    List<DungeonDropEntry> selectedDrops = GetDropTableForEnemy(enemyScript);
                    
                    if (selectedDrops != null && selectedDrops.Count > 0)
                    {
                        var drops = new List<ItemDropSpawner.ItemDropEntry>();
                        foreach (var entry in selectedDrops)
                        {
                            if (entry.item == null) continue;
                            // Tự tính dropChance từ item.rarity
                            float chance = GetDropChanceByRarity(entry.item.rarity);
                            drops.Add(new ItemDropSpawner.ItemDropEntry
                            {
                                item = entry.item,
                                dropChance = chance,
                                minQuantity = entry.minQuantity,
                                maxQuantity = entry.maxQuantity
                            });
                        }
                        spawner.SetDropTable(drops, dropExpOrb, maxDropsPerEnemy);
                    }
                    
                    string typeName = enemyScript != null ? enemyScript.enemyType.ToString() : "unknown";
                    Debug.Log($"[DungeonWave] Added ItemDropSpawner to {activeEnemy.name} (type={typeName}, {selectedDrops?.Count ?? 0} items)");
                }

                break; // Chỉ thêm vào một enemy đang active
            }
        }
    }

    /// <summary>
    /// Chọn drop table theo enemy type
    /// </summary>
    private List<DungeonDropEntry> GetDropTableForEnemy(EnemyScript enemyScript)
    {
        if (enemyScript == null) return skeletDrops; // Fallback

        switch (enemyScript.enemyType)
        {
            case EnemyScript.EnemyType.skelet:
            case EnemyScript.EnemyType.archer:
                return skeletDrops;
            case EnemyScript.EnemyType.monster:
                return monsterDrops;
            case EnemyScript.EnemyType.lich:
                return lichDrops;
            case EnemyScript.EnemyType.boss:
                return bossDrops;
            case EnemyScript.EnemyType.demon:
                return demonDrops;
            default:
                return skeletDrops;
        }
    }

    /// <summary>
    /// Tự tính drop chance dựa trên item rarity
    /// </summary>
    private float GetDropChanceByRarity(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:    return 0.60f; // 60%
            case Rarity.Uncommon:  return 0.45f; // 45%
            case Rarity.Epic:      return 0.30f; // 30%
            case Rarity.Legendary: return 0.15f; // 15%
            case Rarity.Mythic:    return 0.05f; // 5%
            default:               return 0.30f;
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
        if (!isDungeonActive)
        {
            Debug.LogWarning($"[DungeonWave] OnEnemyKilled called but isDungeonActive=false! Ignoring.");
            return;
        }

        enemiesAlive--;
        totalExpGained += expValue;

        Debug.Log($"[DungeonWave] Enemy type {enemyType} died. Còn lại: {enemiesAlive}. EXP: {expValue}. isWaveActive={isWaveActive}");

        // Safety: Kiểm tra ngay khi enemy chết (không đợi Update)
        if (enemiesAlive <= 0 && isWaveActive && !isCountingDown)
        {
            // === FIX: Set isWaveActive = false NGAY để Update() không gọi OnWaveComplete() lần nữa ===
            isWaveActive = false;
            Debug.Log($"[DungeonWave] All enemies dead! Triggering OnWaveComplete from OnEnemyKilled");
            OnWaveComplete();
        }
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
            // Đảm bảo parent chain active
            EnsureParentActive(waveNotificationUI);
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

            Debug.Log($"[DungeonWave] UI: Showing wave notification: {waveName} (activeInHierarchy={waveNotificationUI.activeInHierarchy})");
            StartCoroutine(HideWaveNotificationAfterDelay(2f));
        }
        else
        {
            Debug.LogWarning("[DungeonWave] UI: waveNotificationUI is NULL!");
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
            // Đảm bảo parent chain active
            EnsureParentActive(countdownUI);
            countdownUI.SetActive(true);
            if (countdownText != null)
                countdownText.text = Mathf.Ceil(time).ToString();
            Debug.Log($"[DungeonWave] UI: Showing countdown (activeInHierarchy={countdownUI.activeInHierarchy})");
        }
    }

    /// <summary>
    /// Đảm bảo parent chain của một GameObject cụ thể đều active
    /// </summary>
    private void EnsureParentActive(GameObject obj)
    {
        if (obj == null) return;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            if (!parent.gameObject.activeSelf)
            {
                parent.gameObject.SetActive(true);
            }
            if (parent.GetComponent<Canvas>() != null) break;
            parent = parent.parent;
        }
    }

    /// <summary>
    /// Force Canvas chứa UI lên trên mọi UI khác + đảm bảo có GraphicRaycaster để click được
    /// </summary>
    private void EnsureCanvasOnTop(GameObject uiObj, int order)
    {
        if (uiObj == null) return;

        // Tìm Canvas gần nhất
        Canvas canvas = uiObj.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = order;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Đảm bảo có GraphicRaycaster để button clickable
            if (canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            Debug.Log($"[DungeonWave] Canvas '{canvas.gameObject.name}' → sortingOrder={order}, Overlay mode");
        }
        else
        {
            // Không có Canvas → tạo mới trên parent gốc
            Canvas newCanvas = uiObj.transform.root.gameObject.GetComponent<Canvas>();
            if (newCanvas == null)
                newCanvas = uiObj.transform.root.gameObject.AddComponent<Canvas>();
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            newCanvas.sortingOrder = order;

            if (newCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                newCanvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            Debug.LogWarning($"[DungeonWave] Created Canvas on '{newCanvas.gameObject.name}' → sortingOrder={order}");
        }
    }

    private void HideCountdown()
    {
        if (countdownUI) countdownUI.SetActive(false);
    }

    private void ShowDungeonComplete()
    {
        // Unlock cursor để player click button trên UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Tắt toàn bộ camera — đứng yên khi GUI hiện
        DisableCameraFull();

        if (dungeonCompleteUI)
        {
            EnsureParentActive(dungeonCompleteUI);
            dungeonCompleteUI.SetActive(true);
            
            // Force Canvas lên trên mọi UI khác
            EnsureCanvasOnTop(dungeonCompleteUI, 500);
            
            if (expRewardText)
                expRewardText.text = $"EXP: +{totalExpGained}";
            
            Debug.Log($"[DungeonWave] UI: Showing dungeon complete (activeInHierarchy={dungeonCompleteUI.activeInHierarchy})");
        }

        // Ẩn các UI có thể chặn click
        if (waveNotificationUI) waveNotificationUI.SetActive(false);
        if (countdownUI) countdownUI.SetActive(false);
    }

    private void ShowDungeonFailed()
    {
        // Unlock cursor để player click button trên UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Tắt toàn bộ camera — đứng yên khi GUI hiện
        DisableCameraFull();

        if (dungeonFailedUI)
        {
            // Đảm bảo parent chain active
            EnsureParentActive(dungeonFailedUI);
            dungeonFailedUI.SetActive(true);

            // Force Canvas lên trên mọi UI khác (Notification=100, Wave=100)
            EnsureCanvasOnTop(dungeonFailedUI, 500);

            Debug.Log($"[DungeonWave] UI: Showing dungeon failed (activeInHierarchy={dungeonFailedUI.activeInHierarchy})");
        }

        // Ẩn các UI có thể chặn click
        if (waveNotificationUI) waveNotificationUI.SetActive(false);
        if (countdownUI) countdownUI.SetActive(false);
    }
    
    /// <summary>
    /// Tự động quay về map chính sau thời gian chờ
    /// </summary>
    private IEnumerator AutoReturnToMap(float delay)
    {
        Debug.Log($"[DungeonWave] Sẽ quay về {mainMapSceneName} sau {delay}s...");
        yield return new WaitForSecondsRealtime(delay); // Dùng RealTime để không bị ảnh hưởng bởi Time.timeScale
        ReturnToMainMap();
    }

    // ===== PUBLIC METHODS =====

    /// <summary>
    /// Quay về main map (gọi từ button)
    /// </summary>
    public void ReturnToMainMap()
    {
        Debug.Log($"[DungeonWave] Đang quay về {mainMapSceneName}...");
        ResetPlayerControls(); // Bật lại input + controller trước khi chuyển scene
        EnableCameraInput();
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMapSceneName);
    }

    /// <summary>
    /// Restart dungeon (chơi lại)
    /// </summary>
    public void RestartDungeon()
    {
        Debug.Log("[DungeonWave] Restart dungeon...");
        ResetPlayerControls(); // Bật lại input + controller trước khi restart
        EnableCameraInput();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Bật lại CharacterController + PlayerInput cho player
    /// Phòng trường hợp player dùng DontDestroyOnLoad hoặc respawn tại chỗ
    /// </summary>
    private void ResetPlayerControls()
    {
        if (player == null) return;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = true;

        UnityEngine.InputSystem.PlayerInput pi = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (pi != null) pi.enabled = true;

        Debug.Log("[DungeonWave] Player controls RESET");
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

    /// <summary>
    /// [MỨC NHẸ] Chỉ chặn XOAY camera (mouse input) — camera vẫn FOLLOW player
    /// Dùng khi: animation chết đang chạy, cần camera theo dõi player nhưng không xoay
    /// </summary>
    private void DisableCameraRotation()
    {
        // Tắt tất cả CinemachineInputProvider + input actions → chặn mouse xoay
        var providers = FindObjectsByType<CinemachineInputProvider>(FindObjectsSortMode.None);
        foreach (var provider in providers)
        {
            provider.XYAxis.action?.Disable();
            provider.ZAxis.action?.Disable();
            provider.enabled = false;
        }

        // Tắt CameraCursor để nó không re-enable camera khi nhấn ALT
        var cameraCursors = FindObjectsByType<MovementSystem.CameraCursor>(FindObjectsSortMode.None);
        foreach (var cc in cameraCursors)
        {
            cc.enabled = false;
        }

        // KHÔNG tắt CinemachineBrain/CinemachineCamera → camera vẫn follow player
        Debug.Log($"[DungeonWave] Camera ROTATION disabled (providers={providers.Length}) — camera vẫn follow player");
    }

    /// <summary>
    /// [MỨC NẶNG] Chặn TOÀN BỘ camera — camera đứng yên hoàn toàn
    /// Dùng khi: GUI Complete/Failed đang hiện, không cần camera di chuyển nữa
    /// </summary>
    private void DisableCameraFull()
    {
        // 1. Tắt CinemachineBrain — camera hoàn toàn đứng yên
        var brain = FindFirstObjectByType<CinemachineBrain>();
        if (brain != null)
        {
            brain.enabled = false;
        }

        // 2. Tắt tất cả CinemachineCamera
        var cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (var cam in cameras)
        {
            cam.enabled = false;
        }

        // 3. Tắt input (nếu chưa tắt từ DisableCameraRotation)
        var providers = FindObjectsByType<CinemachineInputProvider>(FindObjectsSortMode.None);
        foreach (var provider in providers)
        {
            provider.XYAxis.action?.Disable();
            provider.ZAxis.action?.Disable();
            provider.enabled = false;
        }

        var cameraCursors = FindObjectsByType<MovementSystem.CameraCursor>(FindObjectsSortMode.None);
        foreach (var cc in cameraCursors)
        {
            cc.enabled = false;
        }

        Debug.Log("[DungeonWave] Camera FULLY disabled — camera đứng yên hoàn toàn");
    }

    /// <summary>
    /// Bật lại toàn bộ camera input khi quay về map hoặc restart
    /// </summary>
    private void EnableCameraInput()
    {
        var brain = FindFirstObjectByType<CinemachineBrain>();
        if (brain != null) brain.enabled = true;

        var cameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        foreach (var cam in cameras) cam.enabled = true;

        var providers = FindObjectsByType<CinemachineInputProvider>(FindObjectsSortMode.None);
        foreach (var provider in providers)
        {
            provider.enabled = true;
            provider.XYAxis.action?.Enable();
            provider.ZAxis.action?.Enable();
        }

        var cameraCursors = FindObjectsByType<MovementSystem.CameraCursor>(FindObjectsSortMode.None);
        foreach (var cc in cameraCursors) cc.enabled = true;

        Debug.Log("[DungeonWave] Camera input ENABLED");
    }

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
    public void GetEnemyCounts(out int skeletOut, out int monsterOut, out int lichOut, out int bossOut, out int demonOut)
    {
        int waveIdx = Mathf.Clamp(currentWave - 1, 0, totalWaves - 1);
        
        skeletOut = waveIdx < skeletCount.Length ? skeletCount[waveIdx] : 0;
        monsterOut = waveIdx < monsterCount.Length ? monsterCount[waveIdx] : 0;
        lichOut = waveIdx < lichCount.Length ? lichCount[waveIdx] : 0;
        bossOut = waveIdx < bossCount.Length ? bossCount[waveIdx] : 0;
        demonOut = waveIdx < demonCount.Length ? demonCount[waveIdx] : 0;
    }

    /// <summary>
    /// Lấy số lượng enemy còn lại có thể spawn (dùng trong RandomEnemy - KHÔNG reset counters)
    /// </summary>
    public void GetRemainingEnemyCounts(out int skeletOut, out int monsterOut, out int lichOut, out int bossOut, out int demonOut)
    {
        skeletOut = Mathf.Max(0, currentSkeletCount - GamePlayManager.archers);
        monsterOut = Mathf.Max(0, currentMonsterCount - GamePlayManager.monsteres);
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
