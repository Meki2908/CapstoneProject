using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }
    public static string CurrentJoinCode { get; private set; }
    public static bool IsInitialized { get; private set; }

    private static Allocation _pendingHostAllocation;
    private static JoinAllocation _pendingClientAllocation;

    /// <summary>True khi có ít nhất 1 client đã kết nối thành công qua Relay.</summary>
    public static bool HasAtLeastOneClient { get; private set; }

    [SerializeField] private int maxConnections = 3;
    [SerializeField] private int joinRetryCount = 5;
    [SerializeField] private float joinRetryDelay = 2f;

    public event Action<string> OnJoinCodeCreated;
    public event Action<string> OnRelayError;
    public event Action OnAtLeastOneClientJoined;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[RelayManager] Awake() — instance created, DontDestroyOnLoad applied.");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ═══════════════════════════════════════════════════════
    // INIT
    // ═══════════════════════════════════════════════════════

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            Debug.Log("[RelayManager] Already initialized, skipping.");
            return;
        }

        try
        {
            Debug.Log("[RelayManager] Initializing Unity Services...");

            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
#if UNITY_EDITOR
                if (ParrelSync.ClonesManager.IsClone())
                {
                    string customArg = ParrelSync.ClonesManager.GetArgument();
                    string profile = string.IsNullOrEmpty(customArg) ? "clone" : customArg;
                    var initOptions = new InitializationOptions();
                    initOptions.SetProfile(profile);
                    await UnityServices.InitializeAsync(initOptions);
                    Debug.Log($"[RelayManager] CLONE init. Profile: {profile}");
                }
                else
                {
                    await UnityServices.InitializeAsync();
                    Debug.Log("[RelayManager] MAIN init. Default profile.");
                }
#else
                await UnityServices.InitializeAsync();
                Debug.Log("[RelayManager] Build init.");
#endif
            }
            else
            {
                Debug.Log("[RelayManager] UnityServices already initialized.");
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[RelayManager] Signed in. PlayerId: {AuthenticationService.Instance.PlayerId}");
            }
            else
            {
                Debug.Log($"[RelayManager] Already signed in. PlayerId: {AuthenticationService.Instance.PlayerId}");
            }

            // DNS diagnostic
            LogDnsResolution();

            IsInitialized = true;
            Debug.Log("[RelayManager] Initialization complete.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] Init failed: {e}");
            OnRelayError?.Invoke($"Init failed: {e.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════
    // HOST: CREATE
    // ═══════════════════════════════════════════════════════

    public async Task<string> CreateRelayAsync()
    {
        try
        {
            if (!IsInitialized)
            {
                Debug.Log("[RelayManager] Not initialized, calling InitializeAsync...");
                await InitializeAsync();
            }

            Debug.Log($"[RelayManager] Creating relay allocation... maxConnections={maxConnections}");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            Debug.Log($"[RelayManager] Allocation created. AllocationId: {allocation.AllocationId}");

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[RelayManager] Join code generated: {joinCode}");

            _pendingHostAllocation = allocation;
            _pendingClientAllocation = null;
            CurrentJoinCode = joinCode;

            Debug.Log($"[RelayManager] HOST setup complete. PlayerId: {AuthenticationService.Instance.PlayerId}");
            Debug.Log($"[RelayManager] Share this code with clients: {joinCode}");

            OnJoinCodeCreated?.Invoke(joinCode);
            return joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] CreateRelay failed: {e}");
            OnRelayError?.Invoke($"Create failed: {e.Message}");
            return null;
        }
    }

    // ═══════════════════════════════════════════════════════
    // CLIENT: JOIN
    // ═══════════════════════════════════════════════════════

    public async Task<bool> JoinRelayAsync(string joinCode)
    {
        try
        {
            if (!IsInitialized)
            {
                Debug.Log("[RelayManager] Not initialized, calling InitializeAsync...");
                await InitializeAsync();
            }

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Debug.LogError("[RelayManager] Join code is EMPTY!");
                OnRelayError?.Invoke("Join code is empty!");
                return false;
            }

            string cleanCode = joinCode.Trim().ToUpper();
            Debug.Log($"[RelayManager] ═══════════════════════════════");
            Debug.Log($"[RelayManager] Joining with code: '{cleanCode}' (length={cleanCode.Length})");
            Debug.Log($"[RelayManager] Auth PlayerId: {AuthenticationService.Instance.PlayerId}");
            Debug.Log($"[RelayManager] ═══════════════════════════════");

            JoinAllocation joinAlloc = null;
            Exception lastEx = null;

            for (int attempt = 1; attempt <= joinRetryCount; attempt++)
            {
                try
                {
                    Debug.Log($"[RelayManager] SDK Join attempt {attempt}/{joinRetryCount}...");
                    joinAlloc = await RelayService.Instance.JoinAllocationAsync(cleanCode);
                    Debug.Log($"[RelayManager] ✅ Join SUCCESS on attempt {attempt}!");
                    Debug.Log($"[RelayManager]    Region: {joinAlloc.Region}");
                    Debug.Log($"[RelayManager]    AllocationId: {joinAlloc.AllocationId}");
                    break;
                }
                catch (Exception retryEx)
                {
                    lastEx = retryEx;
                    Debug.LogWarning($"[RelayManager] Attempt {attempt} failed: {retryEx.Message}");

                    if (attempt < joinRetryCount)
                    {
                        Debug.Log($"[RelayManager] Retrying in {joinRetryDelay}s...");
                        await Task.Delay((int)(joinRetryDelay * 1000));
                    }
                }
            }

            if (joinAlloc == null)
            {
                Debug.LogError($"[RelayManager] Join failed after {joinRetryCount} attempts. Last error: {lastEx?.Message}");
                OnRelayError?.Invoke($"Join failed: {lastEx?.Message}");
                return false;
            }

            _pendingClientAllocation = joinAlloc;
            _pendingHostAllocation = null;
            CurrentJoinCode = cleanCode;

            Debug.Log($"[RelayManager] CLIENT setup complete. Join code: {cleanCode}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] JoinRelay exception: {e}");
            OnRelayError?.Invoke($"Join failed: {e.Message}");
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════
    // APPLY RELAY DATA TO UNITY TRANSPORT
    // ═══════════════════════════════════════════════════════

    public static bool ApplyPendingRelayData(NetworkManager networkManager)
    {
        if (networkManager == null)
        {
            Debug.LogError("[RelayManager] ApplyPendingRelayData: networkManager is null!");
            return false;
        }

        var utp = networkManager.GetComponent<UnityTransport>();
        if (utp == null)
        {
            Debug.LogError("[RelayManager] ApplyPendingRelayData: UnityTransport not found!");
            return false;
        }

        // ── HOST ──
        if (_pendingHostAllocation != null)
        {
            var alloc = _pendingHostAllocation;
            var endpoint = GetEndpoint(alloc.ServerEndpoints);

            Debug.Log($"[RelayManager] Applying HOST relay data:");
            Debug.Log($"[RelayManager]   ServerEndpoint: {endpoint.Host}:{endpoint.Port} ({endpoint.ConnectionType})");
            Debug.Log($"[RelayManager]   AllocationIdBytes: {BitConverter.ToString(alloc.AllocationIdBytes)}");

            utp.SetHostRelayData(
                endpoint.Host,
                (ushort)endpoint.Port,
                alloc.AllocationIdBytes,
                alloc.Key,
                alloc.ConnectionData,
                true
            );

            Debug.Log("[RelayManager] ✅ HOST relay data applied to UnityTransport.");
            _pendingHostAllocation = null;
            return true;
        }

        // ── CLIENT ──
        if (_pendingClientAllocation != null)
        {
            var alloc = _pendingClientAllocation;
            var endpoint = GetEndpoint(alloc.ServerEndpoints);

            Debug.Log($"[RelayManager] Applying CLIENT relay data:");
            Debug.Log($"[RelayManager]   ServerEndpoint: {endpoint.Host}:{endpoint.Port} ({endpoint.ConnectionType})");
            Debug.Log($"[RelayManager]   AllocationIdBytes: {BitConverter.ToString(alloc.AllocationIdBytes)}");
            Debug.Log($"[RelayManager]   Region: {alloc.Region}");

            utp.SetClientRelayData(
                endpoint.Host,
                (ushort)endpoint.Port,
                alloc.AllocationIdBytes,
                alloc.Key,
                alloc.ConnectionData,
                alloc.HostConnectionData,
                true
            );

            Debug.Log("[RelayManager] ✅ CLIENT relay data applied to UnityTransport.");
            _pendingClientAllocation = null;
            return true;
        }

        Debug.LogWarning("[RelayManager] ApplyPendingRelayData: No pending allocation found!");
        return false;
    }

    public static void ClearAll()
    {
        Debug.Log("[RelayManager] ClearAll() called.");
        _pendingHostAllocation = null;
        _pendingClientAllocation = null;
        CurrentJoinCode = null;
        HasAtLeastOneClient = false;
    }

    /// <summary>
    /// Host gọi sau khi xác nhận client đã kết nối (từ NetworkManager callback hoặc lobby).
    /// Khi có client join, host có thể bắt đầu game.
    /// </summary>
    public static void MarkClientJoined()
    {
        if (HasAtLeastOneClient) return;
        HasAtLeastOneClient = true;
        Debug.Log("[RelayManager] ✅ At least one client has joined!");
        Instance?.OnAtLeastOneClientJoined?.Invoke();
    }

    // ═══════════════════════════════════════════════════════
    // DIAGNOSTICS
    // ═══════════════════════════════════════════════════════

    private void LogDnsResolution()
    {
        try
        {
            string host = "relay-allocations.services.api.unity.com";
            var addresses = Dns.GetHostAddresses(host);
            var sb = new StringBuilder();
            sb.Append($"[RelayManager-DNS] {host} resolves to: ");
            foreach (var addr in addresses)
                sb.Append($"{addr} ");
            Debug.Log(sb.ToString());
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager-DNS] Resolution failed: {e.Message}");
        }
    }

    private static RelayServerEndpoint GetEndpoint(System.Collections.Generic.List<RelayServerEndpoint> endpoints)
    {
        if (endpoints == null || endpoints.Count == 0)
        {
            Debug.LogError("[RelayManager] No server endpoints in allocation!");
            return null;
        }

        foreach (var ep in endpoints)
            if (ep.ConnectionType == "dtls") return ep;
        foreach (var ep in endpoints)
            if (ep.ConnectionType == "udp") return ep;
        return endpoints[0];
    }
}
