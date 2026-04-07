using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Singleton quản lý Unity Relay lifecycle:
/// 1. Init UGS + anonymous sign-in
/// 2. Host: CreateAllocation → GetJoinCode → SetRelayServerData → StartHost
/// 3. Client: JoinAllocation(joinCode) → SetRelayServerData → StartClient
///
/// Gọi từ <see cref="MultiplayerManager"/> trước khi LoadScene.
/// Relay data được lưu static để <see cref="NetworkHostBootstrap"/> /
/// <see cref="NetworkClientBootstrap"/> đọc sau khi scene load.
/// </summary>
public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    /// <summary>Join code hiện tại (host tạo, client nhập).</summary>
    public static string CurrentJoinCode { get; private set; }

    /// <summary>Relay allocation data cho host — bootstrap đọc sau khi load scene.</summary>
    public static RelayServerData? PendingHostRelayData { get; private set; }

    /// <summary>Relay allocation data cho client — bootstrap đọc sau khi load scene.</summary>
    public static RelayServerData? PendingClientRelayData { get; private set; }

    /// <summary>True khi UGS đã init + sign-in xong.</summary>
    public static bool IsInitialized { get; private set; }

    [Tooltip("Max số player (không tính host). Mặc định 3 = 4 người chơi tổng.")]
    [SerializeField] private int maxConnections = 3;

    public event Action<string> OnJoinCodeCreated;
    public event Action<string> OnRelayError;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ───────────────────── PUBLIC API ─────────────────────

    /// <summary>Init UGS + anonymous sign-in. Gọi 1 lần, idempotent.</summary>
    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
                Debug.Log("[RelayManager] UGS initialized.");
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[RelayManager] Signed in. Player ID: {AuthenticationService.Instance.PlayerId}");
            }

            IsInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] Init failed: {e.Message}");
            OnRelayError?.Invoke($"Init failed: {e.Message}");
        }
    }

    /// <summary>
    /// HOST: Tạo Relay allocation + join code. Lưu data vào static để bootstrap đọc.
    /// Trả về joinCode, null nếu lỗi.
    /// </summary>
    public async Task<string> CreateRelayAsync()
    {
        try
        {
            if (!IsInitialized) await InitializeAsync();

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Lưu relay data cho bootstrap
            var relayData = new RelayServerData(allocation, "dtls");
            PendingHostRelayData = relayData;
            PendingClientRelayData = null;
            CurrentJoinCode = joinCode;

            Debug.Log($"[RelayManager] Relay created. Join Code: {joinCode}");
            OnJoinCodeCreated?.Invoke(joinCode);

            return joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] CreateRelay failed: {e.Message}");
            OnRelayError?.Invoke($"Create failed: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// CLIENT: Join relay bằng join code. Lưu data vào static để bootstrap đọc.
    /// Trả về true nếu thành công.
    /// </summary>
    public async Task<bool> JoinRelayAsync(string joinCode)
    {
        try
        {
            if (!IsInitialized) await InitializeAsync();

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Debug.LogError("[RelayManager] Join code trống!");
                OnRelayError?.Invoke("Join code trống!");
                return false;
            }

            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim().ToUpper());

            var relayData = new RelayServerData(joinAlloc, "dtls");
            PendingClientRelayData = relayData;
            PendingHostRelayData = null;
            CurrentJoinCode = joinCode;

            Debug.Log($"[RelayManager] Joined relay. Code: {joinCode}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] JoinRelay failed: {e.Message}");
            OnRelayError?.Invoke($"Join failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Áp dụng pending relay data lên UnityTransport. Gọi bởi bootstrap trước StartHost/StartClient.
    /// Trả về true nếu đã áp relay. false = dùng LAN bình thường.
    /// </summary>
    public static bool ApplyPendingRelayData(NetworkManager networkManager)
    {
        if (networkManager == null) return false;

        var utp = networkManager.GetComponent<UnityTransport>();
        if (utp == null) return false;

        if (PendingHostRelayData.HasValue)
        {
            utp.SetRelayServerData(PendingHostRelayData.Value);
            Debug.Log("[RelayManager] Applied HOST relay data to UnityTransport.");
            PendingHostRelayData = null;
            return true;
        }

        if (PendingClientRelayData.HasValue)
        {
            utp.SetRelayServerData(PendingClientRelayData.Value);
            Debug.Log("[RelayManager] Applied CLIENT relay data to UnityTransport.");
            PendingClientRelayData = null;
            return true;
        }

        return false;
    }

    /// <summary>Reset tất cả state (khi quay về menu).</summary>
    public static void ClearAll()
    {
        PendingHostRelayData = null;
        PendingClientRelayData = null;
        CurrentJoinCode = null;
    }
}
