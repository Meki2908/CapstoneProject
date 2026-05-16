using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using Unity.Cinemachine;
using UnityEngine.Playables;
using Fusion;

public class DungeonWaveManager : MonoBehaviour
{
    [Header("=== DUNGEON SETTINGS ===")]
    [Tooltip("Dungeon display name")]
    public string dungeonName = "Dungeon 1";
    [Tooltip("Total waves in dungeon")]
    public int totalWaves = 5;
    [Tooltip("Map type: 0=Desert, 1=Swamp, 2=Hell")]
    public int mapType = 0;
    
    [Header("=== BALANCE DATA ===")]
    [Tooltip("Drag EnemyBalanceData.asset here")]
    public EnemyStatTable balanceData;
    
    [Header("=== SCENE TRANSITION ===")]
    [Tooltip("Main map scene name to return to")]
    public string mainMapSceneName = "Map_Chinh";
    [Tooltip("Delay before auto-return on WIN (seconds)")]
    public float returnDelayOnWin = 5f;
    [Tooltip("Delay before auto-return on LOSE (seconds)")]
    public float returnDelayOnLose = 3f;

    [Header("=== SPAWN SETTINGS ===")]
    [Tooltip("Enemy spawn radius around player")]
    public float spawnRadius = 20f;
    [Tooltip("Minimum spawn distance from player")]
    public float minDistanceFromPlayer = 5f;
    [Tooltip("Obstacle layer mask (walls, rocks...)")]
    public LayerMask obstacleLayer;
    [Tooltip("Max attempts to find valid spawn position")]
    public int spawnAttemptCount = 10;

    [Header("=== WAVE TIMING ===")]
    [Tooltip("Countdown time before wave starts (seconds)")]
    public float waveCountdownTime = 5f;
    [Tooltip("Rest time between waves (seconds)")]
    public float waveDelayTime = 3f;
    [Tooltip("How long host keeps waiting for all clients to be scene-ready before starting intro.")]
    public float readyGateTimeoutSeconds = 45f;

    [Header("=== INTRO TIMELINE (before wave 1) ===")]
    [Tooltip("PlayableDirector for intro cinematic. Leave empty to skip.")]
    public PlayableDirector preEnterDungeonTimeline;
    [Tooltip("If enabled: wait for intro timeline to finish before wave notification + countdown (wave 1 only).")]
    public bool waitForPreEnterTimelineBeforeWave1 = true;
    [Tooltip("If enabled: call Play() from code on dungeon enter (use when not Play On Awake or needs reset).")]
    public bool playPreEnterTimelineFromCode = false;
    [Tooltip("Tự tạo nút Skip góc phải trên khi đang chờ intro timeline (wave 1). Tắt nếu tự làm UI và gọi SkipPreEnterTimeline().")]
    [SerializeField] private bool showRuntimeSkipPreEnterButton = true;
    [SerializeField] private string skipPreEnterButtonLabel = "Skip";

    [Header("=== ENEMY COUNT PER WAVE ===")]
    [Tooltip("Skelet count per wave [wave1, wave2, wave3, wave4, wave5]")]
    public int[] skeletCount = { 3, 4, 5, 4, 0 };
    
    [Tooltip("Lich count per wave")]
    public int[] lichCount = { 0, 0, 0, 1, 0 };
    
    [Tooltip("Stoneogre count per wave")]
    public int[] stoneogreCount = { 0, 0, 0, 0, 0 };
    
    [Tooltip("Golem count per wave")]
    public int[] golemCount = { 0, 0, 0, 0, 0 };
    
    [Tooltip("Minotaur count per wave")]
    public int[] minotaurCount = { 0, 0, 0, 0, 0 };
    
    [Tooltip("Ifrit count per wave")]
    public int[] ifritCount = { 0, 0, 0, 0, 0 };
    
    [Tooltip("Demon count per wave")]
    public int[] demonCount = { 0, 0, 0, 0, 1 };
    
    [Tooltip("Monster (Orc, Troll, Guul) count per wave")]
    public int[] monsterCount = { 0, 0, 0, 0, 0 };
    
    [Tooltip("Total enemies per wave (auto-calculated)")]
    public int[] totalEnemiesPerWave;

    [Header("=== PREFAB ===")]
    [Tooltip("EnemyNew prefab (contains all enemy types)")]
    public GameObject enemyNewPrefab;

    [Header("=== ITEM DROP CONFIG (per Enemy Type) ===")]
    [Tooltip("Enable EXP orb drops")]
    public bool dropExpOrb = true;

    [Tooltip("Item orb prefab (null = auto-generate glowing sphere)")]
    public GameObject itemOrbPrefab;

    [Tooltip("Fusion NetworkObject prefab for loot broadcaster (recommended: LootBroadcaster_Prefab). If null, drops may not broadcast in online mode.")]
    public GameObject lootBroadcasterPrefab;

    [Tooltip("Fusion NetworkObject prefab for dungeon flow sync controller.")]
    public GameObject dungeonFlowControllerPrefab;

    [Tooltip("Drop table for Skeleton/Archer")]
    public List<DungeonDropEntry> skeletDrops = new List<DungeonDropEntry>();
    [Tooltip("Max items dropped per Skeleton/Archer (0 = unlimited)")]
    public int skeletMaxDrops = 2;
    [Tooltip("EXP per Skeleton/Archer kill")]
    public int skeletExp = 100;

    [Tooltip("Drop table for Monster (Orc, Troll, Guul)")]
    public List<DungeonDropEntry> monsterDrops = new List<DungeonDropEntry>();
    [Tooltip("Max items dropped per Monster (0 = unlimited)")]
    public int monsterMaxDrops = 3;
    [Tooltip("EXP per Monster kill")]
    public int monsterExp = 300;

    [Tooltip("Drop table for Lich")]
    public List<DungeonDropEntry> lichDrops = new List<DungeonDropEntry>();
    [Tooltip("Max items dropped per Lich (0 = unlimited)")]
    public int lichMaxDrops = 3;
    [Tooltip("EXP per Lich kill")]
    public int lichExp = 350;

    [Tooltip("Drop table for generic Boss (fallback)")] 
    public List<DungeonDropEntry> bossDrops = new List<DungeonDropEntry>();
    [Tooltip("Max items dropped per Boss (0 = unlimited)")]
    public int bossMaxDrops = 5;
    [Tooltip("EXP per Boss kill")]
    public int bossExp = 1500;

    [Tooltip("Drop table for Demon")]
    public List<DungeonDropEntry> demonDrops = new List<DungeonDropEntry>();
    [Tooltip("Max items dropped per Demon (0 = unlimited)")]
    public int demonMaxDrops = 5;
    [Tooltip("EXP per Demon kill")]
    public int demonExp = 3000;

    [Tooltip("Drop table for Stoneogre")]
    public List<DungeonDropEntry> stoneogreDrops = new List<DungeonDropEntry>();
    [Tooltip("Max items dropped per Stoneogre (0 = unlimited)")]
    public int stoneogreMaxDrops = 5;
    [Tooltip("EXP per Stoneogre kill")]
    public int stoneogreExp = 1500;

    [Tooltip("Drop table for Golem")]
    public List<DungeonDropEntry> golemDrops = new List<DungeonDropEntry>();
    [Tooltip("Max items dropped per Golem (0 = unlimited)")]
    public int golemMaxDrops = 5;
    [Tooltip("EXP per Golem kill")]
    public int golemExp = 1800;

    [Tooltip("Drop table for Minotaur")]
    public List<DungeonDropEntry> minotaurDrops = new List<DungeonDropEntry>();
    [Tooltip("Max items dropped per Minotaur (0 = unlimited)")]
    public int minotaurMaxDrops = 5;
    [Tooltip("EXP per Minotaur kill")]
    public int minotaurExp = 2000;

    [Tooltip("Drop table for Ifrit")]
    public List<DungeonDropEntry> ifritDrops = new List<DungeonDropEntry>();
    [Tooltip("Max items dropped per Ifrit (0 = unlimited)")]
    public int ifritMaxDrops = 5;
    [Tooltip("EXP per Ifrit kill")]
    public int ifritExp = 2500;

    [System.Serializable]
    public class DungeonDropEntry
    {
        [Tooltip("Drag Item ScriptableObject here")]
        public Item item;
        [Tooltip("Minimum drop quantity")]
        public int minQuantity = 1;
        [Tooltip("Maximum drop quantity")]
        public int maxQuantity = 1;
    }

    [Header("=== REFERENCES ===")]
    [Tooltip("Player object")]
    public Transform player;
    [Tooltip("UI Wave Notification")]
    public GameObject waveNotificationUI;
    [Tooltip("Wave name display text")]
    public TextMeshProUGUI waveNameText;
    [Tooltip("Countdown UI panel")]
    public GameObject countdownUI;
    [Tooltip("Countdown display text")]
    public TextMeshProUGUI countdownText;
    [Tooltip("Dungeon Complete UI panel")]
    public GameObject dungeonCompleteUI;
    [Tooltip("Dungeon Failed UI panel")]
    public GameObject dungeonFailedUI;
    [Tooltip("EXP reward display text")]
    public TextMeshProUGUI expRewardText;
    [Tooltip("Status notification text")]
    public TextMeshProUGUI statusText;

    [Header("=== WAVE FRAME PANEL ===")]
    [Tooltip("Panel chứa khung trang trí wave (hiện khi có wave notification / countdown / status)")]
    public GameObject waveFramePanel;

    // ===== PRIVATE VARIABLES =====
    private int currentWave = 0;
    private int enemiesAlive = 0;
    private bool isWaveActive = false;
    private bool isCountingDown = false;
    private bool isDungeonActive = false;
    private bool isDungeonComplete = false;
    private bool showDebugLog = true;
    private bool isWaveCompleting = false; // Guard: tránh gọi OnWaveComplete() 2 lần
    private Coroutine failsafeCoroutine;
    private GameObject _preEnterSkipUiRoot;
    readonly List<PlayerHealth> _subscribedPlayerHealths = new List<PlayerHealth>();
    bool _partyFailedTriggered;
    DungeonFlowNetworkController _flowController;
    DungeonFlowNetworkController.DungeonFlowPhase _lastMirroredPhase = DungeonFlowNetworkController.DungeonFlowPhase.None;
    int _lastMirroredWave = -1;
    
    // Trackers for enemy spawn (tránh gọi GetEnemyCounts nhiều lần)
    private int currentSkeletCount = 0;
    private int currentLichCount = 0;
    private int currentStoneogreCount = 0;
    private int currentGolemCount = 0;
    private int currentMinotaurCount = 0;
    private int currentIfritCount = 0;
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

    void OnDestroy()
    {
        // FIX: Unsubscribe toàn bộ hook OnPlayerDied
        for (int i = 0; i < _subscribedPlayerHealths.Count; i++)
        {
            PlayerHealth ph = _subscribedPlayerHealths[i];
            if (ph != null)
                ph.OnPlayerDied -= OnPlayerDied;
        }
        _subscribedPlayerHealths.Clear();

        // Cleanup singleton
        if (Instance == this)
            Instance = null;
    }

    IEnumerator Start()
    {
        // 1) Resolve player reference on every peer (client can miss spawner-side assignment at scene start)
        float timeout = Time.realtimeSinceStartup + 8f;
        while (player == null && Time.realtimeSinceStartup < timeout)
        {
            TryResolvePlayerReference();
            if (player != null)
                break;
            yield return null;
        }
        if (player == null)
            Debug.LogWarning("[DungeonWave] Start: cannot resolve player after timeout; UI flow will continue.");

        // 2. SETUP CÁC THỨ CÒN LẠI
        ApplyBalanceConfig();
        CalculateTotalEnemiesPerWave();
        SetupPlayerReference();
        EnsureUIParentsActive();
        StripCursorOverlayFromGameplayPanels();

        if (ItemPickupNotification.Instance == null)
        {
            var notifGO = new GameObject("ItemPickupNotification");
            notifGO.AddComponent<ItemPickupNotification>();
        }

        HideAllUI();
        CursorUIPriority.EndAllUiOverlays();

        if (DungeonRewardUI.Instance != null)
        {
            DungeonRewardUI.Instance.ClearTrackedItems();
        }

        // Bắt đầu dungeon logic chỉ trên host/state authority.
        // Client sẽ mirror UI theo DungeonFlowNetworkController.
        if (IsWaveStateAuthority())
            StartDungeon();
        else
        {
            PrepareClientMirrorFlow();
            Debug.Log("[DungeonWave] Client mirror mode active: waiting host-synced dungeon flow.");
        }
    }

    void PrepareClientMirrorFlow()
    {
        if (!IsOnlineMultiplayerSession())
            return;

        // Prevent local PlayOnAwake intro from running ahead of host phase sync.
        if (preEnterDungeonTimeline != null)
        {
            preEnterDungeonTimeline.Stop();
            preEnterDungeonTimeline.time = 0d;
            preEnterDungeonTimeline.Evaluate();
        }

        isDungeonActive = false;
        isWaveActive = false;
        isCountingDown = false;
    }

    bool TryResolvePlayerReference()
    {
        if (player != null)
            return true;

        // Prefer local character on each peer for local UI/hints.
        if (Character.LocalCharacter != null && Character.LocalCharacter.gameObject.activeInHierarchy)
        {
            player = Character.LocalCharacter.transform;
            return true;
        }

        Character[] characters = FindObjectsByType<Character>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < characters.Length; i++)
        {
            Character c = characters[i];
            if (c == null || !c.gameObject.activeInHierarchy)
                continue;
            player = c.transform;
            return true;
        }

        return false;
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

            // sortingOrder: lấy từ scene, không tự set bằng code

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
    /// Xóa CursorUiOverlayWhenActive khỏi tất cả panel gameplay-only của dungeon wave.
    /// Các panel này (wave notification, countdown, status, waveFrame) KHÔNG cần hiện cursor
    /// — chúng chỉ là HUD thông tin, không phải interactive menu.
    /// Mỗi lần SetActive(true) sẽ gọi OnEnable() → BeginUiOverlay() → cursor hiện
    /// → CinemachineInputAxisController bị tắt. Stripping ngăn điều này.
    /// LƯU Ý: dungeonCompleteUI/dungeonFailedUI ĐƯỢC GIỮ NGUYÊN vì chúng có buttons
    /// tương tác và đã được quản lý bởi BeginUiOverlay() trực tiếp trong ShowDungeonComplete/Failed.
    /// </summary>
    private void StripCursorOverlayFromGameplayPanels()
    {
        GameObject[] gameplayOnlyPanels =
        {
            waveNotificationUI,
            countdownUI,
            waveFramePanel,
            statusText    != null ? statusText.gameObject    : null,
            waveNameText  != null ? waveNameText.gameObject  : null,
            countdownText != null ? countdownText.gameObject : null,
        };

        int removed = 0;
        foreach (var panel in gameplayOnlyPanels)
        {
            if (panel == null) continue;
            var overlays = panel.GetComponentsInChildren<CursorUiOverlayWhenActive>(includeInactive: true);
            foreach (var ov in overlays)
            {
                Destroy(ov);
                removed++;
            }
        }

        if (removed > 0)
            Debug.Log($"[DungeonWave] Stripped {removed} CursorUiOverlayWhenActive từ gameplay-only panels.");
        else
            Debug.Log("[DungeonWave] StripCursorOverlayFromGameplayPanels: không tìm thấy overlay component (OK).");
    }

    /// <summary>
    /// Thiết lập "player" (lowercase) reference cho EnemyScript
    /// EnemyScript tìm object "player" để làm target
    /// </summary>
    private void SetupPlayerReference()
    {
        TryResolvePlayerReference();

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
        
        // Đăng ký nhận sự kiện chết cho toàn party (không fail ngay khi 1 người chết).
        for (int i = 0; i < _subscribedPlayerHealths.Count; i++)
        {
            PlayerHealth oldPh = _subscribedPlayerHealths[i];
            if (oldPh != null) oldPh.OnPlayerDied -= OnPlayerDied;
        }
        _subscribedPlayerHealths.Clear();

        PlayerHealth[] allHealths = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < allHealths.Length; i++)
        {
            PlayerHealth ph = allHealths[i];
            if (ph == null || !ph.gameObject.CompareTag("Player"))
                continue;
            ph.OnPlayerDied -= OnPlayerDied;
            ph.OnPlayerDied += OnPlayerDied;
            _subscribedPlayerHealths.Add(ph);
        }
        Debug.Log($"[DungeonWave] Subscribed PlayerHealth.OnPlayerDied count={_subscribedPlayerHealths.Count}");

        Debug.Log("[DungeonWave] Player reference ready for enemies");
    }

    bool IsOnlineMultiplayerSession()
    {
    var localChar = Character.LocalCharacter;
    if (localChar != null &&
        localChar.Runner != null &&
        localChar.Runner.IsRunning &&
        localChar.Runner.GameMode != Fusion.GameMode.Single)
    {
        return true;
    }

    var runner = FindFirstObjectByType<Fusion.NetworkRunner>();
    return runner != null && runner.IsRunning && runner.GameMode != Fusion.GameMode.Single;
    }

    bool IsWaveStateAuthority()
    {
        if (!IsOnlineMultiplayerSession())
            return true;

        var runner = FindFirstObjectByType<Fusion.NetworkRunner>();
        return runner != null && runner.IsRunning && runner.IsServer;
    }

    int CountAlivePlayers(out int totalPlayers)
    {
        totalPlayers = 0;
        int alive = 0;
        PlayerHealth[] allHealths = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < allHealths.Length; i++)
        {
            PlayerHealth ph = allHealths[i];
            if (ph == null || !ph.gameObject.CompareTag("Player"))
                continue;
            totalPlayers++;
            if (ph.IsAlive) alive++;
        }
        return alive;
    }

    void SyncAliveStateToPartyRuntime()
    {
        int totalPlayers;
        int alive = CountAlivePlayers(out totalPlayers);
        bool allDead = totalPlayers > 0 && alive <= 0;
        Character.LocalCharacter?.TryHostSyncDungeonAliveState(alive, totalPlayers, allDead);
    }

    void Update()
    {
        // === TEST: F9 = Win ngay lập tức ===
        #if UNITY_EDITOR
        if ((!IsOnlineMultiplayerSession() || IsWaveStateAuthority()) &&
            UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.f9Key.wasPressedThisFrame &&
            isDungeonActive && !isDungeonComplete)
        {
            Debug.Log("[DungeonWave] ⚡ TEST WIN — F9 pressed!");
            CompleteDungeon();
            return;
        }
        #endif

        if (!isDungeonActive) return;

        if (!IsWaveStateAuthority())
            return;

        SyncAliveStateToPartyRuntime();
        if (!_partyFailedTriggered)
        {
            int totalPlayers;
            int alive = CountAlivePlayers(out totalPlayers);
            if (totalPlayers > 0 && alive <= 0)
            {
                OnPlayerDied();
                return;
            }
        }

        // Kiểm tra enemy còn sống
        if (isWaveActive && !isWaveCompleting && enemiesAlive <= 0 && !isCountingDown)
        {
            OnWaveComplete();
        }
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
            int stoneogres = i < stoneogreCount.Length ? stoneogreCount[i] : 0;
            int golems = i < golemCount.Length ? golemCount[i] : 0;
            int minotaurs = i < minotaurCount.Length ? minotaurCount[i] : 0;
            int ifrits = i < ifritCount.Length ? ifritCount[i] : 0;
            int demons = i < demonCount.Length ? demonCount[i] : 0;
            int monsters = i < monsterCount.Length ? monsterCount[i] : 0;
            totalEnemiesPerWave[i] = skelets + liches + stoneogres + golems + minotaurs + ifrits + demons + monsters;
        }
    }

    /// <summary>
    /// Bắt đầu dungeon
    /// </summary>
    public void StartDungeon()
    {
        if (IsOnlineMultiplayerSession() && !IsWaveStateAuthority())
        {
            Debug.Log("[DungeonWave] StartDungeon ignored on non-authority client.");
            return;
        }

        isDungeonActive = true;
        isDungeonComplete = false;
        _partyFailedTriggered = false;
        currentWave = 0;
        totalExpGained = 0;
        DungeonPartyRuntime.ClearRetryState();
        Character.LocalCharacter?.ResetDungeonPartyFlowState();

        Debug.Log($"[DungeonWave] Bắt đầu dungeon: {dungeonName}");

        EnsureLootBroadcasterSpawnedIfNeeded();
        EnsureFlowControllerSpawnedIfNeeded();
        if (_flowController != null)
            _flowController.HostResetDungeonFlow();

        if (DungeonOSTManager.Instance != null)
            DungeonOSTManager.Instance.OnDungeonFlowStarted();

        // Bắt đầu wave đầu tiên
        StartCoroutine(StartWaveSequence());
    }

    private void EnsureLootBroadcasterSpawnedIfNeeded()
    {
        // In online mode, ItemDropSpawner relies on NetworkLootBroadcaster.Instance to broadcast drops.
        // If it's missing in the dungeon scene, spawn it once on the server/host.
        var runner = FindFirstObjectByType<Fusion.NetworkRunner>();
        if (runner == null || !runner.IsRunning || !runner.IsServer)
            return;

        if (NetworkLootBroadcaster.Instance != null)
            return;

        if (lootBroadcasterPrefab == null) return;

        try
        {
            runner.Spawn(lootBroadcasterPrefab, Vector3.zero, Quaternion.identity, null);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DungeonWave] EnsureLootBroadcaster: spawn failed. {ex}");
        }
    }

    private void EnsureFlowControllerSpawnedIfNeeded()
    {
        _flowController = DungeonFlowNetworkController.Instance;
        if (_flowController != null)
            return;

        var runner = FindFirstObjectByType<Fusion.NetworkRunner>();
        if (runner == null || !runner.IsRunning || !runner.IsServer || dungeonFlowControllerPrefab == null)
            return;

        try
        {
            var obj = runner.Spawn(dungeonFlowControllerPrefab, Vector3.zero, Quaternion.identity, null);
            if (obj != null)
                _flowController = obj.GetComponent<DungeonFlowNetworkController>();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DungeonWave] EnsureFlowController: spawn failed. {ex}");
        }
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

        if (_flowController == null)
            _flowController = DungeonFlowNetworkController.Instance;

        if (currentWave == 1 && IsOnlineMultiplayerSession() && _flowController == null)
        {
            Debug.LogWarning("[DungeonWave] DungeonFlowNetworkController missing in online dungeon. Falling back to unsynced local intro flow.");
            SceneTransitionManager.Instance?.FinishLoadingUI();
        }

        if (_flowController != null && currentWave == 1 && IsOnlineMultiplayerSession())
        {
            float gateTimeout = Mathf.Max(5f, readyGateTimeoutSeconds);
            _flowController.HostSetPhase(DungeonFlowNetworkController.DungeonFlowPhase.WaitingPeers, currentWave, gateTimeout);
            yield return _flowController.HostWaitForPartyReady(gateTimeout);
            SceneTransitionManager.Instance?.FinishLoadingUI();
        }

        if (_flowController != null && currentWave == 1)
        {
            float introDuration = GetPreEnterTimelineDurationSeconds();
            if (introDuration > 0f)
                _flowController.HostSetPhase(DungeonFlowNetworkController.DungeonFlowPhase.Intro, currentWave, introDuration);
        }

        // === CHỜ TIMELINE INTRO (Pre-enter desert) — chỉ wave 1, trước thông báo + countdown ===
        yield return WaitForPreEnterDungeonTimelineIfNeeded();

        // FIX BUG TARGET ẢO: Timeline Intro thường dùng 1 "Dummy Player" (bản sao). 
        // Trong lúc Start() ở frame đầu tiên, Dummy này đang Active nên bị DungeonWaveManager "bắt nhầm" làm player chính.
        // Cập nhật lại Player chính bằng cách tìm chính xác Character trên GameObject Active
        Character[] characters = FindObjectsByType<Character>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform realPlayerTransform = null;
        foreach (Character c in characters)
        {
            if (c != null && c.gameObject.activeInHierarchy)
            {
                realPlayerTransform = c.transform;
                break;
            }
        }

        if (realPlayerTransform != null && realPlayerTransform != player)
        {
            Debug.Log($"[DungeonWave] Refreshing Player Reference after Timeline! Old: {(player != null ? player.name : "null")} -> New: {realPlayerTransform.gameObject.name}");
            player = realPlayerTransform;
            SetupPlayerReference(); // Tạo lại object "player" (lowercase) cho EnemyScript
        }

        if (SceneTransitionManager.Instance != null && !IsOnlineMultiplayerSession())
            SceneTransitionManager.Instance.HideLoadingPanelIfAny();

        // === HIỂN THỊ THÔNG BÁO WAVE ===
        if (_flowController != null)
            _flowController.HostSetPhase(DungeonFlowNetworkController.DungeonFlowPhase.WaveBanner, currentWave, 2f);
        ShowWaveNotification(currentWave);
        
        // Đợi 2 giây để đọc thông báo
        yield return new WaitForSeconds(2f);

        // === ĐẾM NGƯỢC ===
        isCountingDown = true;
        if (_flowController != null)
            _flowController.HostSetPhase(DungeonFlowNetworkController.DungeonFlowPhase.Countdown, currentWave, waveCountdownTime);
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
        if (_flowController != null)
            _flowController.HostSetPhase(DungeonFlowNetworkController.DungeonFlowPhase.Combat, currentWave, 0f);
        Debug.Log($"[DungeonWave] Spawning Wave {currentWave}...");
        SpawnWave(currentWave);
        OnWaveStarted?.Invoke(currentWave);
        Debug.Log($"[DungeonWave] Wave {currentWave} spawned. enemiesAlive={enemiesAlive}, isWaveActive={isWaveActive}");
    }

    float GetPreEnterTimelineDurationSeconds()
    {
        if (!waitForPreEnterTimelineBeforeWave1 || preEnterDungeonTimeline == null)
            return 0f;
        if (double.IsNaN(preEnterDungeonTimeline.duration) || preEnterDungeonTimeline.duration <= 0d)
            return 0f;
        return (float)preEnterDungeonTimeline.duration;
    }

    /// <summary>
    /// Wave có spawn boss/miniboss (Demon, Stoneogre, Golem, Minotaur, Ifrit) không — dùng cho OST.
    /// </summary>
    private bool CurrentWaveHasBossEnemy(int waveIndex)
    {
        int idx = waveIndex - 1;
        if (idx < 0) return false;
        int d = idx < demonCount.Length ? demonCount[idx] : 0;
        int og = idx < stoneogreCount.Length ? stoneogreCount[idx] : 0;
        int gol = idx < golemCount.Length ? golemCount[idx] : 0;
        int mino = idx < minotaurCount.Length ? minotaurCount[idx] : 0;
        int ifr = idx < ifritCount.Length ? ifritCount[idx] : 0;
        return d > 0 || og > 0 || gol > 0 || mino > 0 || ifr > 0;
    }

    /// <summary>
    /// Đợi timeline "Pre-enter desert" (hoặc tương đương) phát xong trước khi hiện UI wave / countdown — chỉ áp dụng wave 1.
    /// </summary>
    private IEnumerator WaitForPreEnterDungeonTimelineIfNeeded()
    {
        if (!waitForPreEnterTimelineBeforeWave1) yield break;
        if (currentWave != 1) yield break;
        if (preEnterDungeonTimeline == null) yield break;

        var dir = preEnterDungeonTimeline;

        if (playPreEnterTimelineFromCode)
        {
            dir.Stop();
            dir.time = 0d;
            dir.Play();
        }

        // Cho Play On Awake / Play() khởi động graph
        yield return null;
        yield return null;

        if (dir.state != PlayState.Playing)
        {
            if (dir.duration > 0d && dir.time >= dir.duration - 0.0001d)
                Debug.Log("[DungeonWave] Pre-enter timeline đã kết thúc trước bước chờ — bỏ qua.");
            else
                Debug.LogWarning("[DungeonWave] Pre-enter timeline không ở trạng thái Playing — kiểm tra PlayableDirector / Play On Awake. Bỏ qua chờ.");
            // Force-enable camera dù timeline không chạy đúng
            MovementSystem.CameraCursor.ApplyGameplayCursorAfterUiClosed();
            CursorUIPriority.EndAllUiOverlays();
            yield break;
        }

        CreatePreEnterSkipButtonIfNeeded();

        bool completed = false;
        void OnStopped(PlayableDirector d)
        {
            completed = true;
        }

        dir.stopped += OnStopped;
        try
        {
            while (!completed)
                yield return null;
        }
        finally
        {
            dir.stopped -= OnStopped;
            DestroyPreEnterSkipButton();
        }

        Debug.Log("[DungeonWave] Pre-enter timeline hoàn tất — bắt đầu thông báo wave / đếm ngược.");

        // QUAN TRỌNG: Gỡ bỏ mọi kẹt state HUD/UI từ timeline.
        // Timeline PreEnterDungeonCutsceneController tắt/bật lại canvas khiến
        // _uiOverlayDepth và _hudVisibleCount bị tăng lên do onEnable,
        // khiến IsGameplayCursorLocked = false -> Camera bị vô hiệu.
        yield return null; // Chờ 1 frame để CinemachineBrain blend xong về PlayerCamera
        MovementSystem.CameraCursor.ApplyGameplayCursorAfterUiClosed();
        CursorUIPriority.EndAllUiOverlays();
        
        // --- BẠO LỰC XOÁ MỌI CẢN TRỞ CURSOR TỪ HUD KẸT TRONG TIMELINE ---
        if (MouseLockManager.Instance != null)
        {
            MouseLockManager.Instance.ClearAllLocksAndForceGameplay();
        }
        
        Debug.Log("[DungeonWave] Camera input force-enabled và UI locks cleared sau pre-enter timeline.");

    }

    /// <summary>
    /// Gọi từ nút Skip (runtime hoặc UI tự gắn) — nhảy đến cuối timeline và dừng (kích hoạt stopped).
    /// </summary>
    public void SkipPreEnterTimeline()
    {
        if (preEnterDungeonTimeline == null) return;
        if (preEnterDungeonTimeline.state != PlayState.Playing) return;
        preEnterDungeonTimeline.time = preEnterDungeonTimeline.duration;
        preEnterDungeonTimeline.Stop();
        // Force-enable camera ngay khi skip — không chờ coroutine yield (stopped event sẽ kích hoạt coroutine tiếp tục)
        MovementSystem.CameraCursor.ApplyGameplayCursorAfterUiClosed();
        CursorUIPriority.EndAllUiOverlays();
    }

    void CreatePreEnterSkipButtonIfNeeded()
    {
        if (!showRuntimeSkipPreEnterButton) return;
        DestroyPreEnterSkipButton();

        var root = new GameObject("PreEnterSkipUI");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        root.AddComponent<GraphicRaycaster>();
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var btnGo = new GameObject("SkipButton", typeof(RectTransform));
        btnGo.transform.SetParent(root.transform, false);
        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-32f, -32f);
        rt.sizeDelta = new Vector2(140f, 44f);

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.18f, 0.92f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(SkipPreEnterTimeline);

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(btnGo.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = skipPreEnterButtonLabel;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 22f;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        _preEnterSkipUiRoot = root;
    }

    void DestroyPreEnterSkipButton()
    {
        if (_preEnterSkipUiRoot != null)
        {
            Destroy(_preEnterSkipUiRoot);
            _preEnterSkipUiRoot = null;
        }
    }

    /// <summary>
    /// Khi wave hoàn thành (kill hết enemy)
    /// </summary>
    [Header("=== LOOT DELAY (FINAL WAVE) ===")]
    [Tooltip("Delay after killing all enemies in final wave to collect loot (seconds)")]
    public float lastWaveLootDelay = 6f;

    private void OnWaveComplete()
    {
        // Guard: tránh gọi 2 lần nếu nhiều enemy chết cùng frame
        if (isWaveCompleting) return;
        isWaveCompleting = true;
        isWaveActive = false;

        // TẮT RADAR QUÉT LỖI VÌ ĐÃ WIN
        if (failsafeCoroutine != null) { 
            StopCoroutine(failsafeCoroutine); 
            failsafeCoroutine = null; 
        }
        OnWaveCompleted?.Invoke(currentWave);

        Debug.Log($"[DungeonWave] Wave {currentWave} hoàn thành! (enemiesAlive={enemiesAlive})");

        if (DungeonOSTManager.Instance != null && CurrentWaveHasBossEnemy(currentWave))
            DungeonOSTManager.Instance.OnBossDefeated();

        // Kiểm tra nếu là wave cuối cùng
        if (currentWave >= totalWaves)
        {
            // Dungeon hoàn thành! Nhưng chờ để player nhặt đồ trước
            StartCoroutine(DelayedCompleteDungeon());
        }
        else
        {
            // Nghỉ giữa các wave rồi bắt đầu wave mới
            if (_flowController != null)
                _flowController.HostSetPhase(DungeonFlowNetworkController.DungeonFlowPhase.BetweenWaves, currentWave, waveDelayTime);
            StartCoroutine(DelayBeforeNextWave());
        }
    }

    /// <summary>
    /// Chờ lastWaveLootDelay giây sau khi giết hết enemy wave cuối
    /// để player kịp nhặt đồ rơi trước khi hiện UI thắng
    /// </summary>
    private IEnumerator DelayedCompleteDungeon()
    {
        Debug.Log($"[DungeonWave] Wave cuối hoàn thành! Chờ {lastWaveLootDelay}s để nhặt đồ...");

        if (statusText != null)
        {
            statusText.text = "Final wave cleared! Collecting loot...";
            statusText.gameObject.SetActive(true);
        }
        if (waveFramePanel) { EnsureParentActive(waveFramePanel); waveFramePanel.SetActive(true); }

        yield return new WaitForSeconds(lastWaveLootDelay);

        if (statusText != null)
            statusText.gameObject.SetActive(false);
        if (waveFramePanel) waveFramePanel.SetActive(false);

        CompleteDungeon();
    }

    /// <summary>
    /// Delay trước khi bắt đầu wave tiếp theo
    /// </summary>
    private IEnumerator DelayBeforeNextWave()
    {
        if (waveDelayTime > 0 && statusText != null)
        {
            statusText.text = $"Wave {currentWave} cleared! Preparing wave {currentWave + 1}...";
            statusText.gameObject.SetActive(true);
            if (waveFramePanel) { EnsureParentActive(waveFramePanel); waveFramePanel.SetActive(true); }
            yield return new WaitForSeconds(waveDelayTime);
            statusText.gameObject.SetActive(false);
            if (waveFramePanel) waveFramePanel.SetActive(false);
        }
        else if (waveDelayTime > 0)
        {
            // statusText null nhưng vẫn cần delay
            yield return new WaitForSeconds(waveDelayTime);
        }

        // REMOVED: Không hồi đầy máu giữa các wave nữa
        // HealPlayerFull();

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
    [Tooltip("Delay for player death animation before showing Failed GUI (match DieState.dieDuration)")]
    public float deathAnimationDelay = 3f;

    public void OnPlayerDied()
    {
        if (IsOnlineMultiplayerSession() && !IsWaveStateAuthority())
            return;

        if (!isDungeonActive || isDungeonComplete || _partyFailedTriggered) return;

        int totalPlayers;
        int alive = CountAlivePlayers(out totalPlayers);
        bool allDead = totalPlayers <= 1 || alive <= 0;

        // Trong co-op: còn người sống thì dungeon vẫn tiếp tục.
        if (IsOnlineMultiplayerSession() && !allDead)
        {
            Debug.Log($"[DungeonWave] Một người chơi đã chết, còn {alive}/{totalPlayers} người sống. Dungeon tiếp tục.");
            SyncAliveStateToPartyRuntime();
            return;
        }

        if (DungeonOSTManager.Instance != null)
            DungeonOSTManager.Instance.OnDungeonMusicEnd();

        Debug.Log("[DungeonWave] Player đã chết! Đợi animation chết xong...");
        
        _partyFailedTriggered = true;
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
        yield return new WaitForSecondsRealtime(deathAnimationDelay);

        Debug.Log("[DungeonWave] Animation chết xong → hiện GUI Failed");

        // Animation xong → giờ mới tắt CharacterController (an toàn)
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
        }

        // Dừng tất cả enemy
        StopAllEnemies();

        // Không ẩn Canvas_Menu / UI_HP+Inventory nữa — DDOL dễ bị inactive vĩnh viễn xuyên scene.
        // Panel thua dùng EnsurePanelOnTop + tắt GraphicRaycaster HUD (trong ShowDungeonFailed) để click được.

        // Hiển thị UI thua
        ShowDungeonFailed();

        if (_flowController != null)
            _flowController.HostSetPhase(DungeonFlowNetworkController.DungeonFlowPhase.Defeat, currentWave, 0f);

        Character.LocalCharacter?.TryHostFinalizeLootCompensation();
        
        OnDungeonFailed?.Invoke();
    }

    /// <summary>
    /// Khi hoàn thành dungeon (kill hết boss wave 5)
    /// </summary>
    private void CompleteDungeon()
    {
        if (IsOnlineMultiplayerSession() && !IsWaveStateAuthority())
            return;

        isDungeonComplete = true;
        isDungeonActive = false;

        if (DungeonOSTManager.Instance != null)
            DungeonOSTManager.Instance.OnDungeonMusicEnd();

        Debug.Log($"[DungeonWave] Dungeon {dungeonName} hoàn thành! Tổng EXP: {totalExpGained}");

        // Khóa player không cho di chuyển/đánh khi GUI Complete hiện
        if (player != null)
        {
            UnityEngine.InputSystem.PlayerInput pi = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (pi != null) pi.enabled = false;

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
        }

        // Delay 4 giây rồi mới hiện GUI
        StartCoroutine(DelayedShowCompleteUI());
    }

    private System.Collections.IEnumerator DelayedShowCompleteUI()
    {
        yield return new WaitForSecondsRealtime(4f);

        // Không ẩn HUD player (UI_HP+Inventory, Canvas_Menu) — restore xuyên scene không tin cậy.
        // Panel thắng dùng EnsurePanelOnTop để nằm trên HUD.

        // Hiển thị UI thắng
        ShowDungeonComplete();

        // Hiển thị Reward Panel
        if (DungeonRewardUI.Instance != null)
        {
            DungeonRewardUI.Instance.ShowRewardPanel();
            DungeonRewardUI.BringWinButtonsAboveReward(dungeonCompleteUI);
        }

        GameCursorManager.TryApplyNormalCursorTextureFromScene();

        Character.LocalCharacter?.TryHostFinalizeLootCompensation();
        if (_flowController != null)
            _flowController.HostSetPhase(DungeonFlowNetworkController.DungeonFlowPhase.Victory, currentWave, 0f);
        OnDungeonCompleted?.Invoke();
    }

    /// <summary>
    /// Gốc hierarchy UI player: PlayerRoot (Canvas_Menu, HUD...) — ưu tiên trước transform player.
    /// </summary>
    private Transform GetRootForPlayerUI()
    {
        GameObject pr = GameObject.Find("PlayerRoot");
        if (pr != null) return pr.transform;
        if (player != null) return player;
        return null;
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

    /// <summary>
    /// Bật lại UI player đã bị ẩn khi complete dungeon
    /// </summary>
    private void RestorePlayerUI()
    {
        Transform searchRoot = GetRootForPlayerUI();

        // Bật lại MỌI Canvas_Menu dưới PlayerRoot (không break — tránh chỉ bật canvas sắp bị destroy cùng scene dungeon).
        if (searchRoot != null)
        {
            foreach (Canvas c in searchRoot.GetComponentsInChildren<Canvas>(true))
            {
                if (c != null && c.gameObject.name == "Canvas_Menu" && !c.gameObject.activeSelf)
                {
                    c.gameObject.SetActive(true);
                    var raycaster = c.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                    if (raycaster != null) raycaster.enabled = true;
                    Debug.Log("[DungeonWave] Restored Canvas_Menu (player hierarchy)");
                }
            }
        }

        Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (Canvas c in allCanvases)
        {
            if (c == null || c.gameObject.name != "Canvas_Menu") continue;
            if (!c.gameObject.scene.IsValid()) continue;
            if (searchRoot != null && c.transform.IsChildOf(searchRoot)) continue;
            if (!c.gameObject.activeSelf)
            {
                c.gameObject.SetActive(true);
                var raycaster = c.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (raycaster != null) raycaster.enabled = true;
                Debug.Log("[DungeonWave] Restored Canvas_Menu (outside player root)");
            }
        }

        string[] uiNames = { 
            "UI_HP+Invetory_1.0", "UI_HP+Inventory_1.0",
            "UI_HP+Invetory_1", "UI_HP+Inventory_1",
            "UI_HP+Invetory", "UI_HP+Inventory",
            "UI_HP_Invetory", "UI_HP_Inventory", "UI_HP-Invetory", "UI_HP-Inventory",
            "UI_HP", "UI_Invetory", "UI_Inventory" 
        };

        if (searchRoot != null)
        {
            foreach (string n in uiNames)
            {
                Transform t = FindInChildren(searchRoot, n);
                if (t != null) 
                { 
                    t.gameObject.SetActive(true); 
                    var raycaster = t.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                    if (raycaster != null) raycaster.enabled = true;
                    Debug.Log($"[DungeonWave] Restored {n}"); 
                }
            }

            // Khớp Hide: bật lại mọi HUD con (không return sớm — trước đây AbilityIcons/SkillBar không được bật lại).
            foreach (Transform child in searchRoot.GetComponentsInChildren<Transform>(true))
            {
                string childName = child.name;
                if ((childName.StartsWith("UI_HP") && (childName.Contains("Invetory") || childName.Contains("Inventory"))) ||
                    childName == "AbilityIcons" || childName == "SkillBar" || childName == "UI_Skills")
                {
                    child.gameObject.SetActive(true);
                    var raycaster = child.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                    if (raycaster != null) raycaster.enabled = true;
                    Debug.Log($"[DungeonWave] Restored '{childName}' (fuzzy match)");
                }
            }
        }

        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (string n in uiNames)
            {
                Transform t = FindInChildren(root.transform, n);
                if (t != null)
                {
                    t.gameObject.SetActive(true);
                    Debug.Log($"[DungeonWave] Restored {n} (scene)");
                }
            }
        }

        // EnsurePanelOnTop() tắt GraphicRaycaster trên HUD mà không ẩn GameObject — Restore phải bật lại dù canvas vẫn active.
        ReenablePlayerHudRaycasters();
    }

    /// <summary>
    /// Bật lại raycast cho Canvas_Menu / UI_HP* sau win-lose (EnsurePanelOnTop đã tắt để click được nút dungeon).
    /// </summary>
    private static void ReenablePlayerHudRaycasters()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in canvases)
        {
            if (c == null) continue;
            string cName = c.gameObject.name;
            if (cName != "Canvas_Menu" &&
                !(cName.StartsWith("UI_HP") && (cName.Contains("Invetory") || cName.Contains("Inventory"))))
                continue;

            var ray = c.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (ray != null && !ray.enabled)
            {
                ray.enabled = true;
                Debug.Log($"[DungeonWave] Re-enabled GraphicRaycaster on '{cName}'");
            }
        }
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
        currentStoneogreCount = waveIdx < stoneogreCount.Length ? stoneogreCount[waveIdx] : 0;
        currentGolemCount = waveIdx < golemCount.Length ? golemCount[waveIdx] : 0;
        currentMinotaurCount = waveIdx < minotaurCount.Length ? minotaurCount[waveIdx] : 0;
        currentIfritCount = waveIdx < ifritCount.Length ? ifritCount[waveIdx] : 0;
        currentDemonCount = waveIdx < demonCount.Length ? demonCount[waveIdx] : 0;
        currentMonsterCount = waveIdx < monsterCount.Length ? monsterCount[waveIdx] : 0;

        int totalBoss = currentStoneogreCount + currentGolemCount + currentMinotaurCount + currentIfritCount;
        int totalEnemies = currentSkeletCount + currentLichCount + totalBoss + currentDemonCount + currentMonsterCount;
        enemiesAlive = totalEnemies;

        Debug.Log($"[DungeonWave] Wave {waveIndex}: Skelet={currentSkeletCount} Monster={currentMonsterCount} Lich={currentLichCount} Stoneogre={currentStoneogreCount} Golem={currentGolemCount} Minotaur={currentMinotaurCount} Ifrit={currentIfritCount} Demon={currentDemonCount} (Tổng: {totalEnemies})");

        bool canSpawnNetworkEnemies = IsWaveStateAuthority();
        if (!canSpawnNetworkEnemies)
        {
            // Client mirror path: keep local counters for UI/kill tracking,
            // but DO NOT rewrite enemiesAlive from spawnedCount=0.
            isWaveActive = true;
            isWaveCompleting = false;
            Debug.Log($"[DungeonWave] Mirror wave {waveIndex} on client. Expecting host-spawned enemies: {totalEnemies}");
            return;
        }

        // === CẤU HÌNH GAMEPLAY MANAGER (CÁCH A) ===
        ConfigureGamePlayManager(currentSkeletCount, currentMonsterCount, currentLichCount, currentDemonCount,
            currentStoneogreCount, currentGolemCount, currentMinotaurCount, currentIfritCount);

        // Spawn từng enemy một
        int spawnedCount = 0;
        for (int i = 0; i < totalEnemies; i++)
        {
            if (SpawnEnemyFromEnemyNew())
                spawnedCount++;
        }

        // Nếu spawn ít hơn dự kiến → cập nhật enemiesAlive
        if (spawnedCount < totalEnemies)
        {
            Debug.LogWarning($"[DungeonWave] Chỉ spawn được {spawnedCount}/{totalEnemies} enemies! Cập nhật enemiesAlive.");
            enemiesAlive = spawnedCount;
        }

        isWaveActive = true;
        isWaveCompleting = false; // Reset guard cho wave mới

        // BẬT RADAR QUÉT LỖI KHI BẮT ĐẦU WAVE
        if (failsafeCoroutine != null) StopCoroutine(failsafeCoroutine);
        failsafeCoroutine = StartCoroutine(WaveCompletionFailsafe());
    }

    /// <summary>
    /// Cấu hình GamePlayManager trước khi spawn (CÁCH A - HỆ THỐNG MỚI)
    /// </summary>
    private void ConfigureGamePlayManager(int skelets, int monsters, int liches, int demons,
        int stoneogres, int golems, int minotaurs, int ifrits)
    {
        // Reset static counters trong GamePlayManager
        GamePlayManager.archers = 0;
        GamePlayManager.monsteres = 0;
        GamePlayManager.lich = 0;
        GamePlayManager.boss = 0;
        GamePlayManager.demon = 0;
        GamePlayManager.stoneogre = 0;
        GamePlayManager.golem = 0;
        GamePlayManager.minotaur = 0;
        GamePlayManager.ifrit = 0;

        // Cấu hình level.enemyType (legacy compatibility)
        if (GamePlayManager.level.enemyType == null || GamePlayManager.level.enemyType.Length != 5)
        {
            GamePlayManager.level.enemyType = new int[5];
        }
        
        int totalBoss = stoneogres + golems + minotaurs + ifrits;
        GamePlayManager.level.enemyType[0] = skelets;
        GamePlayManager.level.enemyType[1] = monsters;
        GamePlayManager.level.enemyType[2] = liches;
        GamePlayManager.level.enemyType[3] = totalBoss; // Legacy boss total
        GamePlayManager.level.enemyType[4] = demons;

        Debug.Log($"[DungeonWave] GamePlayManager configured: Skelet={skelets} Monster={monsters} Lich={liches} Stoneogre={stoneogres} Golem={golems} Minotaur={minotaurs} Ifrit={ifrits} Demon={demons}");
    }

    /// <summary>
    /// Spawn 1 enemy từ EnemyNew prefab
    /// FIX: Không dùng static event broadcast nữa — gọi trực tiếp trên instance
    /// </summary>
    /// <summary>
    /// Spawn 1 enemy từ EnemyNew prefab (ĐÃ NÂNG CẤP CHUẨN FUSION v2)
    /// </summary>
    private bool SpawnEnemyFromEnemyNew()
    {
        if (enemyNewPrefab == null || player == null)
        {
            Debug.LogWarning($"[DungeonWave] Không thể spawn enemy: enemyNewPrefab null hoặc player null");
            return false;
        }

        // Tìm NetworkRunner hiện tại
        var runner = FindFirstObjectByType<Fusion.NetworkRunner>();
        if (runner == null || !runner.IsServer)
        {
            Debug.LogWarning("[DungeonWave] Lỗi: Không tìm thấy NetworkRunner hoặc bạn không phải Server!");
            return false;
        }

        Vector3 spawnPos = GetRandomSpawnPosition();

        // [QUAN TRỌNG NHẤT]: Dùng runner.Spawn thay vì Instantiate
        Fusion.NetworkObject enemyNetObj = runner.Spawn(enemyNewPrefab, spawnPos, Quaternion.identity, null);
        
        if (enemyNetObj == null) return false;
        GameObject enemy = enemyNetObj.gameObject;
        
        // === QUAN TRỌNG: Tắt tất cả enemy con trong prefab ===
        DisableAllChildEnemies(enemy);
        
        // Keep child variants anchored to replicated root transform in RandomEnemy.
        
        // Thêm EnemyWaveTracker
        EnemyWaveTracker tracker = enemy.AddComponent<EnemyWaveTracker>();
        tracker.waveManager = this;
        
        // Gọi Enable trực tiếp trên instance này
        RandomEnemy randomEnemy = enemy.GetComponent<RandomEnemy>();
        if (randomEnemy != null)
        {
            randomEnemy.EnableDirect();
        }

        // Delay setup until RandomEnemy has actually activated a child.
        StartCoroutine(DelayedEnemySetup(enemy));

        if (DungeonOSTManager.Instance != null)
            DungeonOSTManager.Instance.ScheduleBossPresenceCheckForSpawnedRoot(enemy);

        Debug.Log($"<color=green>[DungeonWave]</color> Đã Spawn Network Enemy tại {spawnPos}");
        return true;
    }

    private IEnumerator DelayedEnemySetup(GameObject enemy)
    {
        // Server-side guard (avoid double-init on clients)
        var runner = FindFirstObjectByType<Fusion.NetworkRunner>();
        if (runner == null || !runner.IsServer) yield break;

        if (enemy == null) yield break;
        RandomEnemy randomEnemy = enemy.GetComponent<RandomEnemy>();
        if (randomEnemy == null || randomEnemy.enemys == null) yield break;

        bool childIsActive = false;
        float timeoutTime = Time.realtimeSinceStartup + 0.5f;

        while (Time.realtimeSinceStartup < timeoutTime)
        {
            if (enemy == null) yield break;

            for (int i = 0; i < randomEnemy.enemys.Length; i++)
            {
                if (randomEnemy.enemys[i] != null && randomEnemy.enemys[i].activeSelf)
                {
                    childIsActive = true;
                    break;
                }
            }

            if (childIsActive) break;
            yield return null;
        }

        if (enemy == null) yield break;

        if (!childIsActive)
        {
            Debug.LogWarning($"[DungeonWave] [Failsafe] Timeout! RandomEnemy không bật child nào sau 0.5s cho {enemy.name}");
            yield break;
        }

        AddEnemyDeathBridgeToActiveEnemy(enemy);
        SetPlayerTargetAndChaseForActiveEnemy(enemy);
        ApplyDifficultyStats(enemy);
        ShowBossHealthBarIfNeeded(enemy);

        Debug.Log($"[DungeonWave] Hoàn tất Delayed Setup an toàn cho {enemy.name}");
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

                // Thêm hoặc lấy ItemDropSpawner (KHÔNG skip nếu đã có)
                var spawner = activeEnemy.GetComponent<ItemDropSpawner>();
                if (spawner == null)
                {
                    spawner = activeEnemy.AddComponent<ItemDropSpawner>();
                }
                    
                // Set orb prefab nếu có
                if (itemOrbPrefab != null)
                {
                    spawner.SetOrbPrefab(itemOrbPrefab);
                }
                    
                // Chọn drop table theo enemy type
                var enemyScript = activeEnemy.GetComponent<EnemyScript>();
                List<DungeonDropEntry> selectedDrops = GetDropTableForEnemy(enemyScript);
                    
                // Build drop list
                var drops = new List<ItemDropSpawner.ItemDropEntry>();
                if (selectedDrops != null && selectedDrops.Count > 0)
                {
                    foreach (var entry in selectedDrops)
                    {
                        if (entry.item == null) continue;
                        // useRandomRarity → 50% đồng đều, không phụ thuộc SO rarity
                        float chance = entry.item.useRandomRarity 
                            ? 0.5f 
                            : GetDropChanceByRarity(entry.item.rarity);
                        drops.Add(new ItemDropSpawner.ItemDropEntry
                        {
                            item = entry.item,
                            dropChance = chance,
                            minQuantity = entry.minQuantity,
                            maxQuantity = entry.maxQuantity
                        });
                    }
                }
                    
                // LUÔN gọi SetDropTable — cả khi drops rỗng (để set EXP + maxDrops)
                int maxDrops = GetMaxDropsForEnemy(enemyScript);
                int customExp = GetExpForEnemy(enemyScript);
                spawner.SetDropTable(drops, dropExpOrb, maxDrops, customExp);
                    
                string typeName = enemyScript != null ? enemyScript.enemyType.ToString() : "unknown";
                Debug.Log($"[DungeonWave] Setup ItemDropSpawner on {activeEnemy.name} (type={typeName}, {drops.Count} items, maxDrops={maxDrops}, exp={customExp})");

                break; // Chỉ thêm vào một enemy đang active
            }
        }
    }

    /// <summary>
    /// Chọn drop table theo enemy type
    /// </summary>
    private List<DungeonDropEntry> GetDropTableForEnemy(EnemyScript enemyScript)
    {
        if (enemyScript == null) return skeletDrops;

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
            case EnemyScript.EnemyType.stoneogre:
                return stoneogreDrops.Count > 0 ? stoneogreDrops : bossDrops;
            case EnemyScript.EnemyType.golem:
                return golemDrops.Count > 0 ? golemDrops : bossDrops;
            case EnemyScript.EnemyType.minotaur:
                return minotaurDrops.Count > 0 ? minotaurDrops : bossDrops;
            case EnemyScript.EnemyType.ifrit:
                return ifritDrops.Count > 0 ? ifritDrops : bossDrops;
            default:
                return skeletDrops;
        }
    }

    /// <summary>
    /// Lấy số item tối đa rơi theo enemy type
    /// </summary>
    private int GetMaxDropsForEnemy(EnemyScript enemyScript)
    {
        if (enemyScript == null) return skeletMaxDrops;

        switch (enemyScript.enemyType)
        {
            case EnemyScript.EnemyType.skelet:
            case EnemyScript.EnemyType.archer:
                return skeletMaxDrops;
            case EnemyScript.EnemyType.monster:
                return monsterMaxDrops;
            case EnemyScript.EnemyType.lich:
                return lichMaxDrops;
            case EnemyScript.EnemyType.boss:
                return bossMaxDrops;
            case EnemyScript.EnemyType.demon:
                return demonMaxDrops;
            case EnemyScript.EnemyType.stoneogre:
                return stoneogreMaxDrops;
            case EnemyScript.EnemyType.golem:
                return golemMaxDrops;
            case EnemyScript.EnemyType.minotaur:
                return minotaurMaxDrops;
            case EnemyScript.EnemyType.ifrit:
                return ifritMaxDrops;
            default:
                return skeletMaxDrops;
        }
    }

    /// <summary>
    /// Lấy EXP rơi theo enemy type (chỉnh trong Inspector)
    /// </summary>
    private int GetExpForEnemy(EnemyScript enemyScript)
    {
        if (enemyScript == null) return skeletExp;

        switch (enemyScript.enemyType)
        {
            case EnemyScript.EnemyType.skelet:
            case EnemyScript.EnemyType.archer:
                return skeletExp;
            case EnemyScript.EnemyType.monster:
                return monsterExp;
            case EnemyScript.EnemyType.lich:
                return lichExp;
            case EnemyScript.EnemyType.boss:
                return bossExp;
            case EnemyScript.EnemyType.demon:
                return demonExp;
            case EnemyScript.EnemyType.stoneogre:
                return stoneogreExp;
            case EnemyScript.EnemyType.golem:
                return golemExp;
            case EnemyScript.EnemyType.minotaur:
                return minotaurExp;
            case EnemyScript.EnemyType.ifrit:
                return ifritExp;
            default:
                return skeletExp;
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
            case Rarity.Rare:      return 0.38f; // 38%
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
    // FIX #4: Removed duplicate SetupSelectEnemyPos method (memory leak - no Destroy call)
    // Use SetupSelectEnemyPosForAllEnemies instead (has Destroy(parent, 5f) cleanup)

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
        if (IsOnlineMultiplayerSession() && !IsWaveStateAuthority())
            return;

        if (!isDungeonActive)
        {
            Debug.LogWarning($"[DungeonWave] OnEnemyKilled called but isDungeonActive=false! Ignoring.");
            return;
        }

        if (enemiesAlive <= 0)
            return;

        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        totalExpGained += expValue;

        Debug.Log($"[DungeonWave] Enemy type {enemyType} died. Còn lại: {enemiesAlive}. EXP: {expValue}. isWaveActive={isWaveActive}, isWaveCompleting={isWaveCompleting}");

        // Safety: Kiểm tra ngay khi enemy chết (không đợi Update)
        if (enemiesAlive <= 0 && isWaveActive && !isCountingDown && !isWaveCompleting)
        {
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
        if (waveFramePanel) waveFramePanel.SetActive(false);
    }

    private void ShowWaveNotification(int wave)
    {
        // Hiện wave frame panel
        if (waveFramePanel) { EnsureParentActive(waveFramePanel); waveFramePanel.SetActive(true); }

        if (waveNotificationUI)
        {
            // Đảm bảo parent chain active
            EnsureParentActive(waveNotificationUI);
            waveNotificationUI.SetActive(true);
            
            // Kiểm tra wave này có boss/demon thực tế không (dựa trên config đã apply)
            string waveName = "";
            int waveIdx = wave - 1;
            bool hasBoss = (waveIdx < stoneogreCount.Length && stoneogreCount[waveIdx] > 0)
                        || (waveIdx < golemCount.Length && golemCount[waveIdx] > 0)
                        || (waveIdx < minotaurCount.Length && minotaurCount[waveIdx] > 0)
                        || (waveIdx < ifritCount.Length && ifritCount[waveIdx] > 0);
            bool hasDemon = waveIdx < demonCount.Length && demonCount[waveIdx] > 0;
            
            if (hasDemon)
                waveName = "FINAL BOSS";
            else if (hasBoss)
                waveName = "BOSS WAVE";
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
        // Hiện wave frame panel
        if (waveFramePanel) { EnsureParentActive(waveFramePanel); waveFramePanel.SetActive(true); }

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
    /// Ensure Win/Lose panel canvas is always on top of all other canvases
    /// including DungeonRewardCanvas. Also disables GraphicRaycaster on player UI canvases.
    /// </summary>
    private void EnsurePanelOnTop(GameObject panelObj)
    {
        if (panelObj == null) return;

        // Set win/fail panel canvas above DungeonRewardCanvas
        Canvas panelCanvas = panelObj.GetComponent<Canvas>();
        if (panelCanvas == null)
            panelCanvas = panelObj.GetComponentInParent<Canvas>();

        if (panelCanvas != null)
        {
            // Set win/fail panel BELOW DungeonRewardCanvas
            // so reward items are visible on top of win/fail background
            int rewardSortOrder = 999;
            if (DungeonRewardUI.Instance != null)
            {
                rewardSortOrder = DungeonRewardUI.Instance.RewardCanvasSortOrder;
            }
            
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = rewardSortOrder - 1;
            Debug.Log($"[DungeonWave] Set '{panelCanvas.gameObject.name}' sortingOrder={panelCanvas.sortingOrder} (below reward={rewardSortOrder})");
        }

        // Disable GraphicRaycaster on player UI canvases to unblock panel clicks
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in allCanvases)
        {
            if (c == null || c == panelCanvas) continue;
            string cName = c.gameObject.name;
            if (cName == "Canvas_Menu" || 
                (cName.StartsWith("UI_HP") && (cName.Contains("Invetory") || cName.Contains("Inventory"))))
            {
                var raycaster = c.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (raycaster != null)
                {
                    raycaster.enabled = false;
                    Debug.Log($"[DungeonWave] Disabled GraphicRaycaster on '{cName}' to unblock panel clicks");
                }
            }
        }
    }

    /// <summary>
    /// Đảm bảo Canvas có GraphicRaycaster để button click được
    /// </summary>
    private void EnsureGraphicRaycaster(GameObject uiObj)
    {
        if (uiObj == null) return;
        Canvas canvas = uiObj.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log($"[DungeonWave] Added GraphicRaycaster to '{canvas.gameObject.name}'");
        }
    }

    private void HideCountdown()
    {
        if (countdownUI) countdownUI.SetActive(false);
        if (waveFramePanel) waveFramePanel.SetActive(false);
    }

    private void ShowDungeonComplete()
    {
        CursorUIPriority.BeginUiOverlay();

        // FREEZE GAME — quái dừng tấn công, animation dừng


        // Cursor được xử lý bởi CursorUIPriority.BeginUiOverlay() ở trên
        GameCursorManager.TryApplyNormalCursorTextureFromScene();


        // Tắt toàn bộ camera — đứng yên khi GUI hiện
        DisableCameraFull();

        if (dungeonCompleteUI)
        {
            EnsureParentActive(dungeonCompleteUI);
            dungeonCompleteUI.SetActive(true);
            SoundManager.PlayDungeonVictory();
            
            // Đảm bảo Canvas có GraphicRaycaster để button click được
            EnsureGraphicRaycaster(dungeonCompleteUI);

            // FIX: Đảm bảo panel Win luôn hiển thị trên cùng (trên Canvas_Menu, UI_HP...)
            EnsurePanelOnTop(dungeonCompleteUI);
            
            if (expRewardText)
                expRewardText.text = $"EXP: +{totalExpGained}";
            
            Debug.Log($"[DungeonWave] UI: Showing dungeon complete (activeInHierarchy={dungeonCompleteUI.activeInHierarchy})");
        }

        // Ẩn các UI có thể chặn click
        if (waveNotificationUI) waveNotificationUI.SetActive(false);
        if (countdownUI) countdownUI.SetActive(false);
        if (waveFramePanel) waveFramePanel.SetActive(false);
    }

    private void ShowDungeonFailed()
    {
        CursorUIPriority.BeginUiOverlay();

        // FREEZE GAME — quái dừng tấn công, animation dừng


        // Cursor được xử lý bởi CursorUIPriority.BeginUiOverlay() ở trên
        GameCursorManager.TryApplyNormalCursorTextureFromScene();


        // Tắt toàn bộ camera — đứng yên khi GUI hiện
        DisableCameraFull();

        if (dungeonFailedUI)
        {
            // Đảm bảo parent chain active
            EnsureParentActive(dungeonFailedUI);
            dungeonFailedUI.SetActive(true);
            SoundManager.PlayDungeonDefeat();

            // Đảm bảo Canvas có GraphicRaycaster để button click được
            EnsureGraphicRaycaster(dungeonFailedUI);

            // FIX: Đảm bảo panel Failed luôn hiển thị trên cùng (trên Canvas_Menu, UI_HP...)
            EnsurePanelOnTop(dungeonFailedUI);

            Debug.Log($"[DungeonWave] UI: Showing dungeon failed (activeInHierarchy={dungeonFailedUI.activeInHierarchy})");
        }

        // Ẩn các UI có thể chặn click
        if (waveNotificationUI) waveNotificationUI.SetActive(false);
        if (countdownUI) countdownUI.SetActive(false);
        if (waveFramePanel) waveFramePanel.SetActive(false);
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
        if (IsOnlineMultiplayerSession())
        {
            var c = Character.LocalCharacter;
            if (c != null && !c.IsHostAuthorityForParty())
            {
                c.TryRequestDungeonReturnMap();
                return;
            }
            // Host bấm Return: broadcast lệnh return cho toàn party.
            if (c != null && c.IsHostAuthorityForParty())
            {
                c.TryRequestDungeonReturnMap();
                return;
            }

            ExecuteReturnToMainMapLocal(allowNetworkReturn: true);
            return;
        }

        ExecuteReturnToMainMapLocal();
    }

    public void ForceReturnToMainMapForParty()
    {
        ExecuteReturnToMainMapLocal(allowNetworkReturn: true);
    }

    void ExecuteReturnToMainMapLocal(bool allowNetworkReturn = false)
    {
        Debug.Log($"[DungeonWave] Đang quay về {mainMapSceneName}...");

        SoundManager.StopDungeonResultMusic();
        CursorUIPriority.EndAllUiOverlays();
        
        // Cleanup: ẩn reward panel nếu đang mở
        if (DungeonRewardUI.Instance != null)
        {
            DungeonRewardUI.Instance.HideRewardPanel();
        }

        // Restore player UI (HP/Inventory) bị ẩn khi complete
        RestorePlayerUI();

        ResetPlayerControls();
        EnableCameraInput();
        Time.timeScale = 1f;

        if (allowNetworkReturn && IsOnlineMultiplayerSession())
        {
            var runnerOnline = FindFirstObjectByType<Fusion.NetworkRunner>();
            if (runnerOnline != null && runnerOnline.IsRunning)
            {
                if (runnerOnline.IsServer)
                {
                    int buildIndex = ResolveBuildIndexFromSceneNameOrPath(mainMapSceneName);
                    if (buildIndex >= 0)
                    {
                        if (SceneTransitionManager.Instance != null)
                            SceneTransitionManager.Instance.StartNetworkingLoadingUI();
                        Debug.Log($"[DungeonWave] Host returning to main map via Runner.LoadScene index={buildIndex}");
                        runnerOnline.LoadScene(SceneRef.FromIndex(buildIndex));
                        return;
                    }
                    Debug.LogWarning($"[DungeonWave] Main map scene '{mainMapSceneName}' not found in Build Settings, fallback to local return path.");
                }
                else
                {
                    if (SceneTransitionManager.Instance != null)
                        SceneTransitionManager.Instance.StartNetworkingLoadingUI();
                    Debug.Log("[DungeonWave] Client waiting host return-to-map via Fusion scene sync.");
                    return;
                }
            }
        }

        // === BƯỚC QUAN TRỌNG: TẮT FUSION RUNNER ===
        Debug.Log($"[RETURN-MAP] activeScene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} " +
                  $"mainMapSceneName={mainMapSceneName} timeScale={Time.timeScale}");

        var runner = FindFirstObjectByType<Fusion.NetworkRunner>();
        Debug.Log($"[RETURN-MAP] runnerFound={(runner != null)} " +
                  $"isRunning={(runner != null && runner.IsRunning)} " +
                  $"mode={(runner != null ? runner.GameMode.ToString() : "null")} " +
                  $"sceneManager={(runner != null ? (runner.SceneManager != null ? runner.SceneManager.GetType().Name : "null") : "null")}");

        if (runner != null && runner.IsRunning)
        {
            Debug.Log("[DungeonWave] Đang tắt NetworkRunner để giải phóng Session...");
            // Shutdown runner để dọn dẹp các NetworkObject (Player, Quái mạng)
            // false: không tự động load scene mặc định của Fusion (vì ta tự load bên dưới)
            Debug.Log("[RETURN-MAP] calling runner.Shutdown(false, Ok) ...");
            runner.Shutdown(false, Fusion.ShutdownReason.Ok);
        }
        else
        {
            Debug.LogWarning("[RETURN-MAP] runner missing or not running -> no shutdown executed.");
        }
        // ==========================================

        // Dùng SceneTransitionManager (có loading screen)
        Debug.Log($"[RETURN-MAP] loading main map via SceneTransitionManager?={(SceneTransitionManager.Instance != null)}");
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.GoToScene(
                mainMapSceneName,
                "Đang quay về bản đồ...",
                interruptIfTransitioning: true,
                waitForNetworkSpawn: true
            );
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMapSceneName);
        }
    }

    static int ResolveBuildIndexFromSceneNameOrPath(string sceneNameOrPath)
    {
        if (string.IsNullOrWhiteSpace(sceneNameOrPath))
            return -1;

        int directIndex = UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(sceneNameOrPath);
        if (directIndex >= 0)
            return directIndex;

        string normalized = sceneNameOrPath.Trim();
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrWhiteSpace(path))
                continue;

            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.Equals(path, normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Restart dungeon (chơi lại)
    /// </summary>
    public void RestartDungeon()
    {
        if (IsOnlineMultiplayerSession())
        {
            var c = Character.LocalCharacter;
            if (c != null)
            {
                c.TryRequestDungeonRetryVote();
                return;
            }
        }

        ExecuteRestartDungeonLocal();
    }

    public void ForceRestartDungeonForParty()
    {
        ExecuteRestartDungeonLocal(allowNetworkReload: true);
    }

    void ExecuteRestartDungeonLocal(bool allowNetworkReload = false)
    {
        Debug.Log("[DungeonWave] Restart dungeon...");

        DungeonPreEnterSession.SkipHudHideOnNextPreEnterTimeline = true;
        SoundManager.StopDungeonResultMusic();
        CursorUIPriority.EndAllUiOverlays();

        // Cleanup reward UI
        if (DungeonRewardUI.Instance != null)
        {
            DungeonRewardUI.Instance.HideRewardPanel();
        }

        // Restore player UI
        RestorePlayerUI();

        ResetPlayerControls();
        EnableCameraInput();
        Time.timeScale = 1f;

        if (allowNetworkReload && IsOnlineMultiplayerSession())
        {
            var runnerOnline = FindFirstObjectByType<Fusion.NetworkRunner>();
            if (runnerOnline != null && runnerOnline.IsRunning)
            {
                if (runnerOnline.IsServer)
                {
                    int buildIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
                    if (buildIndex >= 0)
                    {
                        if (SceneTransitionManager.Instance != null)
                            SceneTransitionManager.Instance.StartNetworkingLoadingUI();
                        Debug.Log($"[DungeonWave] Host restarting dungeon via Runner.LoadScene index={buildIndex}");
                        runnerOnline.LoadScene(SceneRef.FromIndex(buildIndex));
                        return;
                    }
                    Debug.LogWarning("[DungeonWave] Active scene build index invalid, fallback to local reload path.");
                }
                else
                {
                    if (SceneTransitionManager.Instance != null)
                        SceneTransitionManager.Instance.StartNetworkingLoadingUI();
                    Debug.Log("[DungeonWave] Client waiting host restart via Fusion scene sync.");
                    return;
                }
            }
        }

        // === OFFLINE/LOCAL fallback: restart by local scene reload ===
        var runner = FindFirstObjectByType<Fusion.NetworkRunner>();
        if (runner != null && runner.IsRunning)
        {
            Debug.Log("[DungeonWave] Local restart fallback: shutting down running NetworkRunner.");
            runner.Shutdown(false, Fusion.ShutdownReason.Ok);
        }

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.GoToScene(
                currentScene,
                "Đang khởi động lại dungeon...",
                interruptIfTransitioning: true,
                waitForNetworkSpawn: true
            );
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene);
        }
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

        // KHÔNG tắt CinemachineBrain/CinemachineCamera → camera vẫn follow player
        // KHÔNG tắt CameraCursor → để nó vẫn sync CinemachineInputAxisController theo MouseLockManager
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

        // KHÔNG tắt CameraCursor — để nó tiếp tục sync CinemachineInputAxisController theo trạng thái
        // cursor của MouseLockManager. Camera đứng yên vì Brain/Camera/Providers đã bị tắt.
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

        // CameraCursor không bị tắt trong DisableCameraFull/Rotation nên không cần enable lại.
        // Nó sẽ tự sync CinemachineInputAxisController khi MouseLockManager ẩn cursor.
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
    public void GetEnemyCounts(out int skeletOut, out int monsterOut, out int lichOut, out int bossOut, out int demonOut,
        out int stoneogreOut, out int golemOut, out int minotaurOut, out int ifritOut)
    {
        int waveIdx = Mathf.Clamp(currentWave - 1, 0, totalWaves - 1);
        
        skeletOut = waveIdx < skeletCount.Length ? skeletCount[waveIdx] : 0;
        monsterOut = waveIdx < monsterCount.Length ? monsterCount[waveIdx] : 0;
        lichOut = waveIdx < lichCount.Length ? lichCount[waveIdx] : 0;
        bossOut = 0; // Legacy — giờ dùng type riêng
        demonOut = waveIdx < demonCount.Length ? demonCount[waveIdx] : 0;
        stoneogreOut = waveIdx < stoneogreCount.Length ? stoneogreCount[waveIdx] : 0;
        golemOut = waveIdx < golemCount.Length ? golemCount[waveIdx] : 0;
        minotaurOut = waveIdx < minotaurCount.Length ? minotaurCount[waveIdx] : 0;
        ifritOut = waveIdx < ifritCount.Length ? ifritCount[waveIdx] : 0;
    }

    /// <summary>
    /// Lấy số lượng enemy còn lại có thể spawn (dùng trong RandomEnemy - KHÔNG reset counters)
    /// </summary>
    public void GetRemainingEnemyCounts(out int skeletOut, out int monsterOut, out int lichOut, out int bossOut, out int demonOut,
        out int stoneogreOut, out int golemOut, out int minotaurOut, out int ifritOut)
    {
        skeletOut = Mathf.Max(0, currentSkeletCount - GamePlayManager.archers);
        monsterOut = Mathf.Max(0, currentMonsterCount - GamePlayManager.monsteres);
        lichOut = Mathf.Max(0, currentLichCount - GamePlayManager.lich);
        bossOut = 0; // Legacy
        demonOut = Mathf.Max(0, currentDemonCount - GamePlayManager.demon);
        stoneogreOut = Mathf.Max(0, currentStoneogreCount - GamePlayManager.stoneogre);
        golemOut = Mathf.Max(0, currentGolemCount - GamePlayManager.golem);
        minotaurOut = Mathf.Max(0, currentMinotaurCount - GamePlayManager.minotaur);
        ifritOut = Mathf.Max(0, currentIfritCount - GamePlayManager.ifrit);
    }

    // ===== BALANCE / DIFFICULTY SYSTEM =====

    /// <summary>
    /// Đọc DungeonConfig.SelectedDifficulty + mapType (từ Inspector) → override wave counts từ EnemyStatTable.
    /// Gọi trong Start() TRƯỚC CalculateTotalEnemiesPerWave().
    /// mapType trên Inspector là nguồn chính (đã set đúng cho từng scene).
    /// Đồng đội chỉ cần set DungeonConfig.SelectedDifficulty trước LoadScene.
    /// </summary>
    private void ApplyBalanceConfig()
    {
        if (balanceData == null)
        {
            Debug.LogWarning("[DungeonWave] balanceData chưa gán! Dùng wave counts mặc định từ Inspector.");
            return;
        }

        // Dùng mapType từ Inspector (đã set đúng cho từng scene dungeon)
        // Sync ngược lại DungeonConfig để ApplyDifficultyStats() dùng đúng
        DungeonConfig.SelectedMapType = mapType;
        
        DungeonDifficulty diff = DungeonConfig.SelectedDifficulty;

        MapDifficultyConfig config = balanceData.GetConfig(mapType, diff);
        if (config == null)
        {
            Debug.LogWarning($"[DungeonWave] Không tìm thấy config map={mapType} diff={diff} trong balanceData!");
            return;
        }

        // Override wave counts
        WaveConfig wc = config.waveConfig;
        if (wc != null)
        {
            skeletCount    = wc.skeletCount ?? skeletCount;
            monsterCount   = wc.monsterCount ?? monsterCount;
            stoneogreCount = wc.stoneogreCount ?? stoneogreCount;
            golemCount     = wc.golemCount ?? golemCount;
            minotaurCount  = wc.minotaurCount ?? minotaurCount;
            ifritCount     = wc.ifritCount ?? ifritCount;
            lichCount      = wc.lichCount ?? lichCount;
            demonCount     = wc.demonCount ?? demonCount;
            Debug.Log($"[DungeonWave] Wave counts overridden từ balanceData (map={mapType}, diff={diff})");
        }

        Debug.Log($"[DungeonWave] === BALANCE CONFIG APPLIED === map={mapType} ({(mapType == 0 ? "Desert" : mapType == 1 ? "Swamp" : "Hell")}), " +
                  $"difficulty={diff}, enemyStats={config.enemyStats?.Length ?? 0} entries");
    }

    /// <summary>
    /// Override HP/ATK/Armor/Accuracy cho enemy vừa spawn dựa trên EnemyStatTable + DungeonConfig.
    /// Gọi SAU KHI enemy bên trong đã được kích hoạt (có EnemyScript active).
    /// </summary>
    private void ApplyDifficultyStats(GameObject enemyRoot)
    {
        if (balanceData == null) return;

        int map = DungeonConfig.SelectedMapType;
        DungeonDifficulty diff = DungeonConfig.SelectedDifficulty;

        // Tìm EnemyScript active bên trong prefab EnemyNew
        EnemyScript es = enemyRoot.GetComponentInChildren<EnemyScript>(false);
        if (es == null) return;

        // Tìm stat entry cho loại enemy cụ thể
        EnemyStatEntry entry = balanceData.GetStats(map, diff, es.specificEnemyType);
        if (entry == null)
        {
            Debug.Log($"[DungeonWave] Không tìm thấy stat cho {es.specificEnemyType} (map={map}, diff={diff}) — dùng stats mặc định");
            return;
        }

        // Override EnemyScript Inspector fields
        es.attackDamage = entry.atk;
        es.armorValue = entry.armor;
        es.accuracy = entry.accuracy;

        // === FIX: SYNC runtime EnemyClass values (D() dùng enemy.attack.value để tính damage) ===
        if (es.enemy != null)
        {
            es.enemy.attack.value = entry.atk;
            es.enemy.armor.value = entry.armor;
            es.enemy.accuracy.value = entry.accuracy;
        }

        // Override TakeDamageTest HP
        TakeDamageTest hpScript = es.GetComponent<TakeDamageTest>();
        if (hpScript == null) hpScript = es.GetComponentInChildren<TakeDamageTest>();
        if (hpScript != null)
        {
            hpScript.MaxHealth = entry.hp;
            hpScript.CurrentHealth = entry.hp;
        }

        // Override EnemyClass.enemyHealth nếu có (legacy system)
        if (es.enemy != null)
        {
            es.enemy.helth.value = (int)entry.hp;
        }

        Debug.Log($"[DungeonWave] Stats applied: {es.specificEnemyType} → HP={entry.hp} ATK={entry.atk} Armor={entry.armor} Acc={entry.accuracy}");
    }

    /// <summary>
    /// Kiểm tra enemy vừa spawn có phải boss không → hiện BossHealthBarUI
    /// </summary>
    private void ShowBossHealthBarIfNeeded(GameObject enemyRoot)
    {
        // Tìm EnemyScript active bên trong
        EnemyScript es = enemyRoot.GetComponentInChildren<EnemyScript>(false);
        if (es == null || !es.isBoss) return;
        
        // Tìm TakeDamageTest trên boss
        TakeDamageTest hpScript = es.GetComponent<TakeDamageTest>();
        if (hpScript == null) hpScript = es.GetComponentInChildren<TakeDamageTest>();
        if (hpScript == null) return;
        
        // Hiện health bar
        BossHealthBarUI.EnsureInstance();
        if (BossHealthBarUI.Instance != null)
        {
            BossHealthBarUI.Instance.ShowBossHealth(hpScript);
            Debug.Log($"[DungeonWave] Boss health bar shown for: {es.enemyName}");
        }
    }

    /// <summary>
    /// RADAR QUÉT SINH MỆNH (FAILSAFE):
    /// Chống lỗi Boss chết/đổi phase/bị Timeline nuốt mất script báo cáo.
    /// </summary>
    private System.Collections.IEnumerator WaveCompletionFailsafe()
    {
        int consecutiveEmptyChecks = 0;

        while (isWaveActive)
        {
            // Cứ 2.5 giây quét một lần cho nhẹ máy
            yield return new WaitForSeconds(2.5f);
            
            if (isWaveActive && !isCountingDown && !isWaveCompleting && enemiesAlive > 0)
            {
                // Chỉ tìm những Enemy có GameObject đang hiện hữu ngoài map (để loại bỏ mấy cái skin bị ẩn và xác chết)
                var allEnemies = FindObjectsByType<EnemyScript>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                bool hasLivingEnemy = false;
                bool isFinalWave = currentWave >= totalWaves;
                
                foreach (var enemy in allEnemies)
                {
                    // Lấy script máu
                    TakeDamageTest hp = enemy.GetComponent<TakeDamageTest>();
                    if (hp == null) hp = enemy.GetComponentInChildren<TakeDamageTest>(true);
                    
                    // Nếu phát hiện CÓ MỘT CON QUÁI NÀO ĐÓ CÒN SỐNG (> 0 máu)
                    if (hp != null && hp.CurrentHealth > 0)
                    {
                        // Ở wave cuối, CHỈ quan tâm đến Boss. Các wave thường quan tâm mọi quái.
                        if (isFinalWave)
                        {
                            // Nếu wave cuối KHÔNG có boss-type nào trong scene (dungeon dễ / cấu hình đặc biệt),
                            // thì vẫn phải tính mọi quái để tránh auto-win sai.
                            bool hasAnyBossTypeInScene = false;
                            for (int i = 0; i < allEnemies.Length; i++)
                            {
                                var e = allEnemies[i];
                                if (e == null) continue;
                                
                                // === Boss-type predicate (match cả isBoss flag lẫn enum) ===
                                if (e.isBoss ||
                                    e.enemyType == EnemyScript.EnemyType.boss ||
                                    e.enemyType == EnemyScript.EnemyType.demon ||
                                    e.enemyType == EnemyScript.EnemyType.minotaur ||
                                    e.enemyType == EnemyScript.EnemyType.ifrit ||
                                    e.enemyType == EnemyScript.EnemyType.lich ||
                                    e.enemyType == EnemyScript.EnemyType.stoneogre || // Thêm con này
                                    e.enemyType == EnemyScript.EnemyType.golem)       // Thêm con này
                                {
                                    hasAnyBossTypeInScene = true;
                                    break;
                                }
                            }

                            if (!hasAnyBossTypeInScene)
                            {
                                hasLivingEnemy = true;
                                break;
                            }

                            // === ĐÃ SỬA: Bổ sung stoneogre và golem vào danh sách ===
                            if (enemy.isBoss ||
                                enemy.enemyType == EnemyScript.EnemyType.boss ||
                                enemy.enemyType == EnemyScript.EnemyType.demon ||
                                enemy.enemyType == EnemyScript.EnemyType.minotaur ||
                                enemy.enemyType == EnemyScript.EnemyType.ifrit ||
                                enemy.enemyType == EnemyScript.EnemyType.lich ||
                                enemy.enemyType == EnemyScript.EnemyType.stoneogre || // Thêm con này
                                enemy.enemyType == EnemyScript.EnemyType.golem)       // Thêm con này
                            {
                                hasLivingEnemy = true;
                                break;
                            }
                        }
                        else
                        {
                            hasLivingEnemy = true;
                            break;
                        }
                    }
                }

                if (!hasLivingEnemy)
                {
                    consecutiveEmptyChecks++;
                    
                    // Đòi hỏi phải có 2 lần quét liên tiếp (5 giây) thấy map trống không
                    // Để tránh việc kích hoạt nhầm lúc Boss đang tàng hình chuyển từ Phase 1 sang Phase 2
                    if (consecutiveEmptyChecks >= 2)
                    {
                        Debug.LogWarning($"[DungeonWave] FAILSAFE KÍCH HOẠT: Hệ thống kẹt enemiesAlive = {enemiesAlive} nhưng không còn quái sống! Ép Win Wave ngay lập tức.");
                        enemiesAlive = 0;
                        OnWaveComplete();
                        yield break;
                    }
                }
                else
                {
                    // Vẫn còn quái sống, reset bộ đếm
                    consecutiveEmptyChecks = 0;
                }
            }
        }
    }

    // === HỆ THỐNG ĐẺ ĐỒ LOCAL (Fusion Loot Broadcaster) ===
    public void SpawnLocalLoot(Vector3 position, int enemyTypeInt)
    {
        // Tạo một spawner cục bộ (không NetworkObject) để mỗi máy tự spawn loot cho vui (gacha).
        GameObject tempSpawnerObj = new GameObject("TempLocalSpawner");
        tempSpawnerObj.transform.position = position;
        var spawner = tempSpawnerObj.AddComponent<ItemDropSpawner>();

        if (itemOrbPrefab != null) spawner.SetOrbPrefab(itemOrbPrefab);

        List<DungeonDropEntry> selectedDrops = GetDropTableByInt(enemyTypeInt);
        int maxDrops = GetMaxDropsByInt(enemyTypeInt);
        int customExp = GetExpByInt(enemyTypeInt);

        var drops = new List<ItemDropSpawner.ItemDropEntry>();
        if (selectedDrops != null)
        {
            foreach (var entry in selectedDrops)
            {
                if (entry == null || entry.item == null) continue;
                float chance = entry.item.useRandomRarity ? 0.5f : GetDropChanceByRarity(entry.item.rarity);
                drops.Add(new ItemDropSpawner.ItemDropEntry
                {
                    item = entry.item,
                    dropChance = chance,
                    minQuantity = entry.minQuantity,
                    maxQuantity = entry.maxQuantity
                });
            }
        }

        spawner.SetDropTable(drops, dropExpOrb, maxDrops, customExp);
        spawner.ExecuteLocalSpawn(position);

        Destroy(tempSpawnerObj, 2f);
    }

    private List<DungeonDropEntry> GetDropTableByInt(int type)
    {
        return type switch
        {
            0 or 1 => skeletDrops,
            2 => monsterDrops,
            3 => lichDrops,
            4 => bossDrops,
            5 => demonDrops,
            6 => (stoneogreDrops != null && stoneogreDrops.Count > 0) ? stoneogreDrops : bossDrops,
            7 => (golemDrops != null && golemDrops.Count > 0) ? golemDrops : bossDrops,
            8 => (minotaurDrops != null && minotaurDrops.Count > 0) ? minotaurDrops : bossDrops,
            9 => (ifritDrops != null && ifritDrops.Count > 0) ? ifritDrops : bossDrops,
            _ => skeletDrops
        };
    }

    private int GetMaxDropsByInt(int type)
    {
        return type switch
        {
            0 or 1 => skeletMaxDrops,
            2 => monsterMaxDrops,
            3 => lichMaxDrops,
            4 => bossMaxDrops,
            5 => demonMaxDrops,
            6 => stoneogreMaxDrops,
            7 => golemMaxDrops,
            8 => minotaurMaxDrops,
            9 => ifritMaxDrops,
            _ => skeletMaxDrops
        };
    }

    private int GetExpByInt(int type)
    {
        return type switch
        {
            0 or 1 => skeletExp,
            2 => monsterExp,
            3 => lichExp,
            4 => bossExp,
            5 => demonExp,
            6 => stoneogreExp,
            7 => golemExp,
            8 => minotaurExp,
            9 => ifritExp,
            _ => skeletExp
        };
    }

    public bool HasWaveStateAuthority()
    {
        return IsWaveStateAuthority();
    }

    public void MirrorApplyHostPhase(DungeonFlowNetworkController.DungeonFlowPhase phase, int wave, float elapsedSeconds, float phaseDurationSeconds)
    {
        if (IsWaveStateAuthority())
            return;

        if (phase == DungeonFlowNetworkController.DungeonFlowPhase.None)
            return;

        bool phaseChanged = _lastMirroredPhase != phase || _lastMirroredWave != wave;
        if (phaseChanged)
        {
            _lastMirroredPhase = phase;
            _lastMirroredWave = wave;
        }

        switch (phase)
        {
            case DungeonFlowNetworkController.DungeonFlowPhase.WaitingPeers:
                isDungeonActive = false;
                break;
            case DungeonFlowNetworkController.DungeonFlowPhase.Intro:
                isDungeonActive = true;
                currentWave = Mathf.Max(currentWave, wave);
                if (phaseChanged)
                    SceneTransitionManager.Instance?.HideLoadingPanelIfAny();
                MirrorSyncIntroTimeline(elapsedSeconds, phaseDurationSeconds);
                break;
            case DungeonFlowNetworkController.DungeonFlowPhase.WaveBanner:
                isDungeonActive = true;
                currentWave = wave;
                if (phaseChanged)
                    ShowWaveNotification(wave);
                break;
            case DungeonFlowNetworkController.DungeonFlowPhase.Countdown:
            {
                isDungeonActive = true;
                currentWave = wave;
                isCountingDown = true;
                float remaining = Mathf.Max(0f, phaseDurationSeconds - elapsedSeconds);
                ShowCountdown(remaining);
                if (countdownText != null)
                    countdownText.text = Mathf.Ceil(remaining).ToString();
                break;
            }
            case DungeonFlowNetworkController.DungeonFlowPhase.Combat:
                isDungeonActive = true;
                currentWave = wave;
                isCountingDown = false;
                HideCountdown();
                isWaveActive = true;
                isWaveCompleting = false;
                break;
            case DungeonFlowNetworkController.DungeonFlowPhase.BetweenWaves:
                isDungeonActive = true;
                isWaveActive = false;
                break;
            case DungeonFlowNetworkController.DungeonFlowPhase.Victory:
                isDungeonActive = false;
                isDungeonComplete = true;
                if (phaseChanged)
                    ShowDungeonComplete();
                break;
            case DungeonFlowNetworkController.DungeonFlowPhase.Defeat:
                isDungeonActive = false;
                if (phaseChanged)
                    ShowDungeonFailed();
                break;
        }
    }

    void MirrorSyncIntroTimeline(float elapsedSeconds, float phaseDurationSeconds)
    {
        if (preEnterDungeonTimeline == null)
            return;

        var dir = preEnterDungeonTimeline;
        double maxDuration = dir.duration > 0d ? dir.duration : (double)phaseDurationSeconds;
        if (maxDuration <= 0d)
            return;

        double target = Mathf.Clamp(elapsedSeconds, 0f, (float)maxDuration);
        if (target >= maxDuration - 0.05d)
        {
            if (dir.state == PlayState.Playing)
            {
                dir.time = maxDuration;
                dir.Evaluate();
                dir.Stop();
            }
            return;
        }

        if (dir.state != PlayState.Playing)
        {
            dir.time = target;
            dir.Evaluate();
            dir.Play();
            return;
        }

        if (Mathf.Abs((float)(dir.time - target)) > 0.25f)
        {
            dir.time = target;
            dir.Evaluate();
        }
    }
}

/// <summary>
/// Script theo dõi enemy trong wave — chỉ lưu metadata
/// FIX #1/#3: Removed death tracking coroutine — EnemyDeathBridge handles death notification
/// to avoid double OnEnemyKilled calls (enemiesAlive bị trừ 2 lần)
/// </summary>
public class EnemyWaveTracker : MonoBehaviour
{
    public DungeonWaveManager waveManager;
    public int enemyType;
}
