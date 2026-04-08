using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using MovementSystem;

/// <summary>
/// Được scene gán trước khi StartHost; <see cref="NetworkPlayerSpawnSnap"/> đọc khi spawn.
/// </summary>
public static class PlayerSpawnConfig
{
    public static Transform SpawnPoint { get; set; }
}

/// <summary>
/// Gắn cùng GameObject có <see cref="NetworkManager"/> trong scene gameplay (vd. Testboss).
/// Khi vào từ menu (cờ pending), gọi <see cref="NetworkManager.StartHost"/> để spawn player prefab đã cấu hình.
/// </summary>
[DefaultExecutionOrder(100)]
public class NetworkHostBootstrap : MonoBehaviour
{
    [Tooltip("Kéo empty object trong scene — player host sẽ spawn đúng vị trí/góc này.")]
    [SerializeField] private Transform playerSpawnPoint;

    bool _disconnectHooked;

    private void Start()
    {
        if (playerSpawnPoint != null)
            PlayerSpawnConfig.SpawnPoint = playerSpawnPoint;

        if (!MultiplayerManager.PendingStartHostAfterSceneLoad)
            return;

        MultiplayerManager.ClearPendingHostFlag();

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[NetworkHostBootstrap] Không tìm thấy NetworkManager.Singleton.");
            return;
        }

        if (NetworkManager.Singleton.IsServer)
            return;

        // Apply Relay data if using internet relay
        bool usedRelay = RelayManager.ApplyPendingRelayData(NetworkManager.Singleton);

        if (!usedRelay)
        {
            // LAN only: offset port for ParrelSync clone
            ParrelSyncTransportPort.ApplyClonePortOffsetIfNeeded(NetworkManager.Singleton);
        }

        Debug.Log($"[NetworkHostBootstrap] Relay={usedRelay}");

        // Hook disconnect/connect recovery TRƯỚC StartHost
        TryHookDisconnectRecovery();

        if (!NetworkManager.Singleton.StartHost())
            Debug.LogError("[NetworkHostBootstrap] StartHost() failed.");
        else
            Debug.Log("[NetworkHostBootstrap] StartHost() thành công.");
    }

    // Static event để thông báo bên ngoài khi client kết nối trong gameplay
    private static event Action<ulong> _onHostClientConnected;

    public static event Action<ulong> OnAnyClientConnectedInGameplay
    {
        add => _onHostClientConnected += value;
        remove => _onHostClientConnected -= value;
    }

    void OnEnable()
    {
        TryHookDisconnectRecovery();
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton != null && _disconnectHooked)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            _disconnectHooked = false;
        }
    }

    void TryHookDisconnectRecovery()
    {
        if (NetworkManager.Singleton == null || _disconnectHooked) return;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        _disconnectHooked = true;
    }

    void OnClientDisconnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        // Ignore host local shutdown path; only recover when a remote client leaves.
        if (clientId == nm.LocalClientId) return;
        StartCoroutine(RecoverHostGameplayNextFrame());
    }

    void OnClientConnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        // Bỏ qua chính host
        if (clientId == nm.LocalClientId) return;

        Debug.Log($"[NetworkHostBootstrap] Client {clientId} connected! Marking client joined.");
        RelayManager.MarkClientJoined();
        _onHostClientConnected?.Invoke(clientId);
    }

    System.Collections.IEnumerator RecoverHostGameplayNextFrame()
    {
        // Wait one frame so NGO despawn/ownership cleanup finishes first.
        yield return null;

        if (!CursorUIPriority.IsUiOverlayActive)
        {
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            CameraCursor.ApplyGameplayCursorAfterUiClosed();
        }

        // Re-enable local host input maps in case they were left disabled by a UI flow.
        PlayerInput localInput = null;
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.SpawnManager != null)
        {
            var localObj = nm.SpawnManager.GetLocalPlayerObject();
            if (localObj != null)
                localInput = localObj.GetComponentInChildren<PlayerInput>(true);
        }
        if (localInput == null)
            localInput = FindFirstObjectByType<PlayerInput>();

        if (localInput != null && localInput.actions != null)
        {
            var playerMap = localInput.actions.FindActionMap("Player", false);
            if (playerMap != null) playerMap.Enable();
            var skillMap = localInput.actions.FindActionMap("Skill", false);
            if (skillMap != null) skillMap.Enable();
        }

        var cams = FindObjectsByType<CinemachineInputAxisController>(FindObjectsSortMode.None);
        for (int i = 0; i < cams.Length; i++)
            if (cams[i] != null) cams[i].enabled = true;
    }
}
