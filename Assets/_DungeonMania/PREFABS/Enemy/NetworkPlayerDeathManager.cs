using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using TMPro;

/// <summary>
/// Gắn trên player prefab (root, cùng NetworkObject).
/// Quản lý death state, respawn, và spectate trong multiplayer.
///
/// Flow:
///   1. PlayerHealth.Die() → RPC gọi ServerAuthority
///   2. Server: set [Networked] NetIsDead = true + NetDeathTime
///   3. Tất cả clients: nhận change → local player enter spectate mode
///   4. Spectate camera follow người chơi khác gần nhất
///   5. Khi đủ điều kiện (timer / tất cả enemy chết): RPC respawn cho tất cả
///   6. Respawn: reset HP + teleport tới spawn point + enter gameplay
/// </summary>
[DefaultExecutionOrder(200)]
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerDeathManager : NetworkBehaviour
{
    // ── Networked Properties ──
    [Networked] public NetworkBool NetIsDead { get; set; }
    [Networked] public NetworkBool NetIsSpectating { get; set; }
    [Networked] public float NetDeathTime { get; set; }

    // ── Config ──
    [Header("Respawn Settings")]
    [Tooltip("Thời gian chờ respawn (giây) sau khi chết.")]
    [SerializeField] private float respawnDelay = 8f;
    [Tooltip("Thời gian chờ respawn nếu TẤT CẢ người chơi đều chết (dungeon fail nhanh hơn).")]
    [SerializeField] private float allDeadRespawnDelay = 5f;
    [Tooltip("Respawn khi tất cả enemy đã bị tiêu diệt (bất kể timer).")]
    [SerializeField] private bool respawnOnWaveComplete = true;

    [Header("Spectate Settings")]
    [Tooltip("Chiều cao camera spectate trên đầu target.")]
    [SerializeField] private float spectateCameraHeight = 3f;
    [Tooltip("Khoảng cách camera spectate phía sau target.")]
    [SerializeField] private float spectateCameraDistance = 8f;
    [Tooltip("Smooth factor cho camera spectate.")]
    [SerializeField] private float spectateSmoothSpeed = 5f;

    [Header("References (auto-find nếu null)")]
    [SerializeField] private CinemachineCamera gameplayCamera;
    [SerializeField] private GameObject spectateCameraGO;
    [SerializeField] private TextMeshProUGUI spectateStatusText;
    [SerializeField] private TextMeshProUGUI respawnCountdownText;
    [SerializeField] private GameObject spectateUI;

    // ── Private State ──
    private ChangeDetector _changeDetector;
    private bool _wasDead = false;
    private bool _isLocalSpectating = false;
    private NetworkObject _currentSpectateTarget;
    private int _spectateTargetIndex = 0;
    private List<NetworkObject> _alivePlayers = new List<NetworkObject>();
    private float _localRespawnTimer = 0f;
    private bool _respawnTriggered = false;

    // ── Events ──
    public event Action OnLocalPlayerDied;
    public event Action OnLocalPlayerRespawned;
    public event Action<NetworkObject> OnSpectateTargetChanged;

    // ── Singleton for easy access ──
    public static NetworkPlayerDeathManager LocalInstance { get; private set; }

    // ═══════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
        _wasDead = NetIsDead;

        if (HasInputAuthority)
        {
            LocalInstance = this;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
            LocalInstance = null;
    }

    public override void FixedUpdateNetwork()
    {
        // ─── Server: kiểm tra respawn timer ───
        if (HasStateAuthority && NetIsDead)
        {
            _localRespawnTimer -= Runner.DeltaTime; // BUG-4 fix: dùng simulation delta
            if (_localRespawnTimer <= 0f && !_respawnTriggered)
            {
                TryAutoRespawn();
            }
        }
    }

    public override void Render()
    {
        if (_changeDetector == null) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(NetIsDead):
                    HandleDeathStateChanged();
                    break;
                case nameof(NetIsSpectating):
                    HandleSpectateStateChanged();
                    break;
            }
        }

        // ─── Local: update spectate camera ───
        if (_isLocalSpectating)
        {
            UpdateSpectateCamera();
            HandleSpectateInput();
        }
    }

    // ═══════════════════════════════════════════════════
    // PUBLIC API
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Gọi từ PlayerHealth.Die() khi máu = 0.
    /// Client gửi RPC tới server để set death state.
    /// </summary>
    public void RequestPlayerDeath()
    {
        if (NetIsDead) return; // Đã chết rồi

        if (HasStateAuthority)
        {
            // Host: set trực tiếp
            SetDeathStateRPC(true);
        }
        else
        {
            // Client: gửi RPC
            RPC_RequestDeath();
        }
    }

    /// <summary>
    /// Gọi từ Host khi muốn respawn một player cụ thể.
    /// Hoặc gọi không tham số để respawn tất cả người chơi đã chết.
    /// </summary>
    public void RequestRespawn(PlayerRef targetPlayer = default)
    {
        if (!HasStateAuthority) return;

        if (targetPlayer == default)
            RPC_RespawnAll();
        else
            RPC_RespawnTarget(targetPlayer);
    }

    /// <summary>
    /// Kiểm tra player cục bộ có đang spectate không.
    /// </summary>
    public bool IsLocalSpectating => _isLocalSpectating;

    /// <summary>
    /// Kiểm tra player cục bộ có đang chết không.
    /// </summary>
    public bool IsLocalDead => NetIsDead && HasInputAuthority;

    /// <summary>
    /// Lấy danh sách người chơi còn sống (để spectate).
    /// </summary>
    public IReadOnlyList<NetworkObject> GetAlivePlayers() => _alivePlayers;

    // ═══════════════════════════════════════════════════
    // RPC — CLIENT → SERVER
    // ═══════════════════════════════════════════════════

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SetSpectateState(bool isSpectating)
    {
        NetIsSpectating = isSpectating;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestDeath(RpcInfo info = default)
    {
        Debug.Log($"[DeathManager] RPC_RequestDeath from {info.Source}");
        SetDeathStateRPC(true);
    }

    // BUG-3 fix: Chỉ chạy trên StateAuthority, state tự sync qua ChangeDetector
    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    void SetDeathStateRPC(bool isDead, RpcInfo info = default)
    {
        NetIsDead = isDead;
        if (isDead)
        {
            NetDeathTime = Runner.SimulationTime; // BUG-9 fix: dùng simulation time
            _localRespawnTimer = respawnDelay;

            Debug.Log($"[DeathManager] Player {info.Source} died! Respawn in {_localRespawnTimer}s");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_RespawnAll(RpcInfo info = default)
    {
        Debug.Log("[DeathManager] Respawning all dead players!");
        // BUG-2 fix: StateAuthority ghi [Networked], tất cả chỉ xử lý local visual
        if (HasStateAuthority)
        {
            NetIsDead = false;
            NetIsSpectating = false;
        }
        DoLocalRespawn();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_RespawnTarget(PlayerRef target, RpcInfo info = default)
    {
        if (target == Runner.LocalPlayer)
        {
            Debug.Log("[DeathManager] Respawn targeted for local player!");
            if (HasStateAuthority)
            {
                NetIsDead = false;
                NetIsSpectating = false;
            }
            DoLocalRespawn();
        }
    }

    // ═══════════════════════════════════════════════════
    // INTERNAL LOGIC
    // ═══════════════════════════════════════════════════

    private void HandleDeathStateChanged()
    {
        if (NetIsDead && !_wasDead)
        {
            // ─── Vừa mới chết ───
            _wasDead = true;
            _respawnTriggered = false;
            _localRespawnTimer = respawnDelay;

            if (HasInputAuthority)
            {
                Debug.Log("[DeathManager] 👤 Local player died — entering spectate mode");
                EnterSpectateMode();
                OnLocalPlayerDied?.Invoke();
            }
        }
        else if (!NetIsDead && _wasDead)
        {
            // ─── Vừa mới sống lại ───
            _wasDead = false;

            if (HasInputAuthority)
            {
                Debug.Log("[DeathManager] ✅ Local player respawned!");
                ExitSpectateMode();
                OnLocalPlayerRespawned?.Invoke();
            }
        }
    }

    private void HandleSpectateStateChanged()
    {
        if (NetIsSpectating && !_isLocalSpectating)
        {
            if (HasInputAuthority)
                EnterSpectateMode();
        }
        else if (!NetIsSpectating && _isLocalSpectating)
        {
            if (HasInputAuthority)
                ExitSpectateMode();
        }
    }

    /// <summary>
    /// Bắt đầu chế độ spectate — camera follow người chơi khác.
    /// </summary>
    private void EnterSpectateMode()
    {
        _isLocalSpectating = true;
        // BUG-1c fix: Dùng RPC thay vì ghi trực tiếp [Networked]
        if (HasStateAuthority)
            NetIsSpectating = true;
        else
            RPC_SetSpectateState(true);
        _spectateTargetIndex = 0;

        // ─── Ẩn gameplay camera ───
        if (gameplayCamera != null)
            gameplayCamera.enabled = false;

        // ─── Tạo/render spectate camera ───
        SetupSpectateCamera();
        UpdateAlivePlayersList();
        SelectNextSpectateTarget();

        // ─── Hiện spectate UI ───
        if (spectateUI != null)
            spectateUI.SetActive(true);

        // ─── Ẩn local player controls (chỉ cần NetworkPlayerLocalOwnership xử lý) ───
        // Player đã chết → NetworkPlayerLocalOwnership sẽ tắt input
        // Để mesh vẫn hiện để người khác thấy (player nằm đó)
        Debug.Log("[DeathManager] Spectate mode ON — following other player");
    }

    /// <summary>
    /// Thoát chế độ spectate — quay về gameplay bình thường.
    /// </summary>
    private void ExitSpectateMode()
    {
        _isLocalSpectating = false;
        // BUG-1c fix: Dùng RPC thay vì ghi trực tiếp [Networked]
        if (HasStateAuthority)
            NetIsSpectating = false;
        else
            RPC_SetSpectateState(false);

        // ─── Bật lại gameplay camera ───
        if (gameplayCamera != null)
            gameplayCamera.enabled = true;

        // ─── Tắt spectate camera ───
        if (spectateCameraGO != null)
            spectateCameraGO.SetActive(false);

        // ─── Ẩn spectate UI ───
        if (spectateUI != null)
            spectateUI.SetActive(false);

        // ─── Reset respawn state ───
        _respawnTriggered = false;

        Debug.Log("[DeathManager] Spectate mode OFF — back to gameplay");
    }

    private void SetupSpectateCamera()
    {
        if (spectateCameraGO != null)
        {
            spectateCameraGO.SetActive(true);
            return;
        }

        // BUG-11 fix: Disable existing AudioListeners before creating new one
        foreach (var existingAL in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
        {
            if (existingAL.gameObject != spectateCameraGO)
                existingAL.enabled = false;
        }

        // Tạo spectate camera nếu chưa có
        var camGO = new GameObject("[SpectateCamera]");
        camGO.transform.SetParent(transform);
        spectateCameraGO = camGO;

        var cam = camGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 500f;

        // Audio listener
        camGO.AddComponent<AudioListener>();

        Debug.Log("[DeathManager] Created spectate camera");
    }

    private void UpdateSpectateCamera()
    {
        if (spectateCameraGO == null || _currentSpectateTarget == null) return;

        Transform target = _currentSpectateTarget.transform;

        // Tính vị trí camera đằng sau + trên target
        Vector3 targetPos = target.position + Vector3.up * spectateCameraHeight
            + (-target.forward * spectateCameraDistance);

        // Smooth lerp camera position
        spectateCameraGO.transform.position = Vector3.Lerp(
            spectateCameraGO.transform.position, targetPos,
            Time.deltaTime * spectateSmoothSpeed);

        // Camera nhìn vào target
        Vector3 lookAtPos = target.position + Vector3.up * 1.5f;
        spectateCameraGO.transform.LookAt(lookAtPos);

        // Cập nhật UI
        UpdateSpectateUI();
    }

    private void HandleSpectateInput()
    {
        // Tab / V: chuyển người spectate
        if (Keyboard.current[Key.Tab].wasPressedThisFrame || Keyboard.current.vKey.wasPressedThisFrame)
        {
            SelectNextSpectateTarget();
        }

        // Mouse: xoay view quanh target
        float mouseX = Mouse.current.delta.x.ReadValue() * 0.3f;
        if (Mouse.current.delta.x.ReadValue() != 0)
        {
            if (_currentSpectateTarget != null)
            {
                // Xoay vị trí camera quanh target
                Transform target = _currentSpectateTarget.transform;
                Vector3 offset = spectateCameraGO.transform.position - target.position;
                float dist = offset.magnitude;
                float angle = Mathf.Atan2(offset.x, offset.z);
                angle += mouseX * Time.deltaTime * 2f;
                offset = new Vector3(Mathf.Sin(angle) * dist, offset.y, Mathf.Cos(angle) * dist);
                spectateCameraGO.transform.position = target.position + offset;
            }
        }
    }

    private void SelectNextSpectateTarget()
    {
        UpdateAlivePlayersList();

        if (_alivePlayers.Count == 0)
        {
            Debug.Log("[DeathManager] No alive players to spectate!");
            return;
        }

        _spectateTargetIndex = (_spectateTargetIndex + 1) % _alivePlayers.Count;
        _currentSpectateTarget = _alivePlayers[_spectateTargetIndex];

        // Nếu target là chính mình → bỏ qua
        if (_currentSpectateTarget != null && HasInputAuthority)
        {
            var netStats = _currentSpectateTarget.GetComponent<NetworkPlayerStats>();
            // Đơn giản: nếu chỉ còn 1 người alive → spectate chính mình (sẽ được respawn)
            if (_alivePlayers.Count == 1)
            {
                // Vẫn spectate chính mình
            }
        }

        Debug.Log($"[DeathManager] Spectating player: {_currentSpectateTarget?.name ?? "null"} ({_spectateTargetIndex + 1}/{_alivePlayers.Count})");
        OnSpectateTargetChanged?.Invoke(_currentSpectateTarget);
    }

    private void UpdateAlivePlayersList()
    {
        _alivePlayers.Clear();

        if (Runner == null || !Runner.IsRunning) return;

        foreach (var player in Runner.ActivePlayers)
        {
            var runnerObj = Runner.GetPlayerObject(player);
            if (runnerObj == null) continue;

            // Kiểm tra player có đang sống không
            var deathMgr = runnerObj.GetComponent<NetworkPlayerDeathManager>();
            if (deathMgr != null && deathMgr.NetIsDead) continue; // Đã chết → skip

            // Thêm vào danh sách (trừ chính mình nếu đang spectate)
            if (!HasInputAuthority || runnerObj != Object)
            {
                _alivePlayers.Add(runnerObj);
            }
        }
    }

    private void UpdateSpectateUI()
    {
        if (spectateStatusText != null)
        {
            string targetName = "Unknown";
            if (_currentSpectateTarget != null)
            {
                var nameComp = _currentSpectateTarget.GetComponent<NetworkPlayerName>();
                targetName = nameComp != null ? nameComp.NetName : _currentSpectateTarget.name;
            }

            string controls = "\n[TAB] or [V]: Đổi người theo dõi\n[Mouse]: Xoay camera";
            spectateStatusText.text = $"BẠN ĐÃ CHẾT!\nĐang theo dõi: {targetName}\n{controls}";
        }

        if (respawnCountdownText != null)
        {
            float timeLeft = Mathf.Max(0f, _localRespawnTimer);
            respawnCountdownText.text = $"Hồi sinh sau: {Mathf.Ceil(timeLeft)}s";
        }
    }

    /// <summary>
    /// Server kiểm tra điều kiện respawn tự động.
    /// </summary>
    private void TryAutoRespawn()
    {
        if (!HasStateAuthority || _respawnTriggered) return;

        _respawnTriggered = true;

        // ─── Kiểm tra điều kiện respawn ───

        // Điều kiện 1: Tất cả enemy đã chết (wave complete / dungeon complete)
        bool allEnemiesDead = CheckAllEnemiesDead();

        // Điều kiện 2: Timer đã hết
        bool timerExpired = _localRespawnTimer <= 0f;

        // Điều kiện 3: Có người chơi khác còn sống (spectate → respawn)
        bool hasAliveAlly = CheckHasAliveAllies();

        if (respawnOnWaveComplete && allEnemiesDead)
        {
            Debug.Log("[DeathManager] All enemies dead → respawning all players!");
            RPC_RespawnAll();
            return;
        }

        if (timerExpired && hasAliveAlly)
        {
            Debug.Log($"[DeathManager] Timer expired ({respawnDelay}s) + ally alive → respawning!");
            RPC_RespawnAll();
            return;
        }

        if (!hasAliveAlly)
        {
            // Tất cả chết → dungeon fail (DungeonWaveManager xử lý)
            Debug.Log("[DeathManager] All players dead! Dungeon will fail.");
            _respawnTriggered = false; // Reset để thử lại
            return;
        }
    }

    private bool CheckAllEnemiesDead()
    {
        // Kiểm tra DungeonWaveManager
        if (DungeonWaveManager.Instance != null)
        {
            return DungeonWaveManager.Instance.EnemiesAlive <= 0
                && !DungeonWaveManager.Instance.IsWaveActive;
        }

        // Fallback: kiểm tra EnemyScript
        var enemies = FindObjectsByType<EnemyScript>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (e.alive) return false;
        }
        return true;
    }

    private bool CheckHasAliveAllies()
    {
        if (Runner == null) return false;

        foreach (var player in Runner.ActivePlayers)
        {
            // BUG-8 fix: Skip chính player đang chết, không phải Runner.LocalPlayer
            if (player == Object.InputAuthority) continue;

            var runnerObj = Runner.GetPlayerObject(player);
            if (runnerObj == null) continue;

            var deathMgr = runnerObj.GetComponent<NetworkPlayerDeathManager>();
            if (deathMgr != null && !deathMgr.NetIsDead)
                return true; // Có ít nhất 1 đồng minh còn sống
        }
        return false;
    }

    /// <summary>
    /// Thực hiện respawn cho player cục bộ.
    /// </summary>
    private void DoLocalRespawn()
    {
        // BUG-2 fix: [Networked] đã được set trong RPC_RespawnAll/RPC_RespawnTarget
        // Ở đây chỉ xử lý local visual/gameplay state
        _respawnTriggered = true;

        // ─── Reset HP ───
        var health = GetComponentInChildren<PlayerHealth>(true);
        if (health != null)
        {
            health.ResetHealth();
            Debug.Log("[DeathManager] HP reset after respawn");
        }

        // ─── Teleport tới spawn point ───
        var spawnPt = PlayerSpawnConfig.SpawnPoint;
        Vector3 pos = spawnPt != null ? spawnPt.position : Vector3.zero;
        Quaternion rot = spawnPt != null ? spawnPt.rotation : Quaternion.identity;

        // Nếu multiplayer → dùng spawn point riêng cho player này
        if (MultiplayerManager.Runner != null && MultiplayerManager.Runner.IsRunning)
        {
            int idx = GetPlayerSpawnIndex();
            pos = GetPlayerSpawnPosition(idx, pos);
        }

        // Dùng TeleportTo để reset interpolation state
        var spawnSnap = GetComponent<NetworkPlayerSpawnSnap>();
        if (spawnSnap != null)
        {
            spawnSnap.TeleportTo(pos, rot);
        }
        else
        {
            transform.SetPositionAndRotation(pos, rot);
        }
        Debug.Log($"[DeathManager] Teleported to spawn: {pos}");

        // ─── Reset DieState → StandingState ───
        var character = GetComponentInChildren<Character>();
        if (character != null && character.dieState != null)
        {
            character.movementSM.ChangeState(character.standing);
            Debug.Log("[DeathManager] State changed from DieState → StandingState");
        }

        // ─── Reset input ───
        var playerInput = GetComponentInChildren<PlayerInput>();
        if (playerInput != null)
            playerInput.enabled = true;

        var cc = GetComponentInChildren<CharacterController>();
        if (cc != null)
            cc.enabled = true;

        // ─── Unlock cursor ───
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[DeathManager] ✅ Respawn complete!");
    }

    /// <summary>
    /// Lấy chỉ số spawn point của player này (để không trùng với người khác).
    /// </summary>
    private int GetPlayerSpawnIndex()
    {
        if (Runner == null) return 0;
        int idx = 0;
        foreach (var p in Runner.ActivePlayers)
        {
            if (p == Object.InputAuthority)
                return idx;
            idx++;
        }
        return 0;
    }

    /// <summary>
    /// Tính vị trí spawn cho player dựa trên index (tránh đứng chồng lên nhau).
    /// </summary>
    private Vector3 GetPlayerSpawnPosition(int playerIndex, Vector3 basePos)
    {
        // Xếp người chơi thành vòng tròn quanh spawn point
        float angle = playerIndex * (360f / 4f); // Tối đa 4 người
        float radius = 3f;
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);
        return basePos + offset;
    }
}
