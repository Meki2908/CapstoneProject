using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Serializable relay join data cho file-based sharing giữa host và clone.
/// Host pre-fetch JoinAllocation rồi lưu file → Clone đọc file bypass Relay API 404.
/// </summary>
[System.Serializable]
public class RelayJoinData
{
    public string JoinCode;
    public string AllocationIdBytes; // Base64
    public string Key;              // Base64
    public string ConnectionData;   // Base64
    public string HostConnectionData; // Base64
    public string Region;
    public string ServerHost;
    public ushort ServerPort;
    public bool IsSecure;
}

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }
    public static string CurrentJoinCode { get; private set; }
    public static bool IsInitialized { get; private set; }

    private static Allocation _pendingHostAllocation;
    private static JoinAllocation _pendingClientAllocation;
    private static RelayJoinData _fileBasedJoinData;

    /// <summary>True khi có ít nhất 1 client đã kết nối thành công qua Relay.</summary>
    public static bool HasAtLeastOneClient { get; private set; }

    [SerializeField] private int maxConnections = 3;
    [SerializeField] private int joinRetryCount = 3;
    [SerializeField] private float joinRetryDelay = 1f;

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
                // Unity Hub truyền -cloudEnvironment production cho main editor,
                // nhưng ParrelSync clone KHÔNG có flag này → clone dùng environment khác
                // → Relay API scope join code theo environment → 404 "join code not found"
                // Fix: ép tất cả dùng "production"
                const string ENV_NAME = "production";

#if UNITY_EDITOR
                bool isMppmClone = false;
                string mppmProfile = null;

                // ═══════════════════════════════════════════════════════════
                // MPPM (Multiplayer Play Mode) virtual player detection
                // Dùng reflection vì VirtualProjectsEditor ở Editor-only assembly
                // ═══════════════════════════════════════════════════════════
                try
                {
                    var vpType = System.Type.GetType(
                        "Unity.Multiplayer.Playmode.VirtualProjects.Editor.VirtualProjectsEditor, Unity.Multiplayer.Playmode.VirtualProjects.Editor");
                    if (vpType != null)
                    {
                        var isCloneProp = vpType.GetProperty("IsClone", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (isCloneProp != null)
                        {
                            isMppmClone = (bool)isCloneProp.GetValue(null);
                        }
                        if (isMppmClone)
                        {
                            var idProp = vpType.GetProperty("CloneIdentifier", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                            if (idProp != null)
                            {
                                mppmProfile = $"mppm_{idProp.GetValue(null)}";
                            }
                        }
                    }
                }
                catch (System.Exception)
                {
                    // MPPM package không có hoặc API thay đổi — bỏ qua
                }

                if (isMppmClone && !string.IsNullOrEmpty(mppmProfile))
                {
                    var initOptions = new InitializationOptions();
                    initOptions.SetProfile(mppmProfile);
                    initOptions.SetEnvironmentName(ENV_NAME);
                    await UnityServices.InitializeAsync(initOptions);
                    Debug.Log($"[RelayManager] MPPM CLONE init. Profile: {mppmProfile}, Env: {ENV_NAME}");
                }
                else if (ParrelSync.ClonesManager.IsClone())
                {
                    string customArg = ParrelSync.ClonesManager.GetArgument();
                    string profile = string.IsNullOrEmpty(customArg) ? "clone" : customArg;
                    var initOptions = new InitializationOptions();
                    initOptions.SetProfile(profile);
                    initOptions.SetEnvironmentName(ENV_NAME);
                    await UnityServices.InitializeAsync(initOptions);
                    Debug.Log($"[RelayManager] PARRELSYNC CLONE init. Profile: {profile}, Env: {ENV_NAME}");
                }
                else
                {
                    var initOptions = new InitializationOptions();
                    initOptions.SetEnvironmentName(ENV_NAME);
                    await UnityServices.InitializeAsync(initOptions);
                    Debug.Log($"[RelayManager] MAIN init. Default profile, Env: {ENV_NAME}");
                }
#else
                var initOptions = new InitializationOptions();
                initOptions.SetEnvironmentName(ENV_NAME);
                await UnityServices.InitializeAsync(initOptions);
                Debug.Log($"[RelayManager] Build init. Env: {ENV_NAME}");
#endif
            }
            else
            {
                Debug.Log("[RelayManager] UnityServices already initialized.");
            }

            // Auth check với fallback: khi Editor restart Play Mode mà không domain reload,
            // UnityServices.State vẫn Initialized nhưng AuthenticationService singleton bị destroy.
            try
            {
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log($"[RelayManager] Signed in. PlayerId: {AuthenticationService.Instance.PlayerId}");
                }
                else
                {
                    Debug.Log($"[RelayManager] Already signed in. PlayerId: {AuthenticationService.Instance.PlayerId}");
                }
            }
            catch (ServicesInitializationException)
            {
                // Auth singleton bị stale — force re-init
                Debug.LogWarning("[RelayManager] Auth singleton stale, forcing re-initialization...");
                const string RE_ENV = "production";
#if UNITY_EDITOR
                if (ParrelSync.ClonesManager.IsClone())
                {
                    string customArg = ParrelSync.ClonesManager.GetArgument();
                    string profile = string.IsNullOrEmpty(customArg) ? "clone" : customArg;
                    var opts = new InitializationOptions();
                    opts.SetProfile(profile);
                    opts.SetEnvironmentName(RE_ENV);
                    await UnityServices.InitializeAsync(opts);
                }
                else
                {
                    var opts = new InitializationOptions();
                    opts.SetEnvironmentName(RE_ENV);
                    await UnityServices.InitializeAsync(opts);
                }
#else
                var opts = new InitializationOptions();
                opts.SetEnvironmentName(RE_ENV);
                await UnityServices.InitializeAsync(opts);
#endif
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[RelayManager] Re-init signed in. PlayerId: {AuthenticationService.Instance.PlayerId}");
            }

            // Diagnostic: verify project identity matches between host & client
            Debug.Log($"[RelayManager] CloudProjectId: {UnityEngine.Application.cloudProjectId}");
            Debug.Log($"[RelayManager] UnityServices State: {UnityServices.State}");

            // JWT decode: xem environment_id và project_id trong token
            try
            {
                string token = AuthenticationService.Instance.AccessToken;
                if (!string.IsNullOrEmpty(token))
                {
                    string[] parts = token.Split('.');
                    if (parts.Length >= 2)
                    {
                        string payload = parts[1];
                        // Fix base64 padding
                        switch (payload.Length % 4)
                        {
                            case 2: payload += "=="; break;
                            case 3: payload += "="; break;
                        }
                        string json = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(payload));
                        Debug.Log($"[RelayManager] JWT payload: {json}");
                    }
                }
            }
            catch (System.Exception ex) { Debug.Log($"[RelayManager] JWT decode error: {ex.Message}"); }

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

            // Log Relay API BasePath for comparison with clone
            try
            {
                var ri = RelayService.Instance;
                var f = ri.GetType().GetField("m_RelayService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f != null) { var s = f.GetValue(ri); var cp = s.GetType().GetProperty("Configuration"); if (cp != null) { var c = cp.GetValue(s); var bp = c.GetType().GetProperty("BasePath"); if (bp != null) Debug.Log($"[RelayManager] HOST Relay API BasePath: {bp.GetValue(c)}"); } }
            } catch { }

            Debug.Log($"[RelayManager] Creating relay allocation... maxConnections={maxConnections}");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            Debug.Log($"[RelayManager] Allocation created. AllocationId: {allocation.AllocationId}");
            Debug.Log($"[RelayManager] Allocation Region: {allocation.Region}");
            Debug.Log($"[RelayManager] Allocation ConnectionData length: {allocation.ConnectionData.Length}");
            Debug.Log($"[RelayManager] Allocation RelayServer: {allocation.RelayServer.IpV4}:{allocation.RelayServer.Port}");
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[RelayManager] Join code generated: {joinCode}");
            // ════════════════════════════════════════════════════════════
            // PRE-FETCH: Host gọi JoinAllocationAsync (đã chứng minh hoạt động),
            // lưu relay data ra file → clone đọc file, bypass Relay API 404 bug.
            // ════════════════════════════════════════════════════════════
#if UNITY_EDITOR
            try
            {
                var clientJoin = await RelayService.Instance.JoinAllocationAsync(joinCode);
                Debug.Log($"[RelayManager] ✅ Pre-fetched client allocation. Region: {clientJoin.Region}");

                var joinData = new RelayJoinData
                {
                    JoinCode = joinCode,
                    AllocationIdBytes = System.Convert.ToBase64String(clientJoin.AllocationIdBytes),
                    Key = System.Convert.ToBase64String(clientJoin.Key),
                    ConnectionData = System.Convert.ToBase64String(clientJoin.ConnectionData),
                    HostConnectionData = System.Convert.ToBase64String(clientJoin.HostConnectionData),
                    Region = clientJoin.Region
                };

                var ep = GetEndpoint(clientJoin.ServerEndpoints);
                joinData.ServerHost = ep.Host;
                joinData.ServerPort = (ushort)ep.Port;
                joinData.IsSecure = ep.ConnectionType == "dtls";

                string json = JsonUtility.ToJson(joinData, true);
                string projectDir = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);
                if (ParrelSync.ClonesManager.IsClone())
                    projectDir = ParrelSync.ClonesManager.GetOriginalProjectPath();
                string filePath = System.IO.Path.Combine(projectDir, "relay_join_data.json");
                System.IO.File.WriteAllText(filePath, json);
                Debug.Log($"[RelayManager] Relay join data saved to: {filePath}");
            }
            catch (System.Exception prefetchEx)
            {
                Debug.LogWarning($"[RelayManager] Pre-fetch failed (non-fatal): {prefetchEx.Message}");
            }
#endif

            _pendingHostAllocation = allocation;
            _pendingClientAllocation = null;
            CurrentJoinCode = joinCode;

            string playerId = "N/A";
            string hostTokenPrefix = "N/A";
            try {
                playerId = AuthenticationService.Instance.PlayerId;
                string token = AuthenticationService.Instance.AccessToken;
                hostTokenPrefix = token != null && token.Length > 20 ? token.Substring(0, 20) + "..." : token;
            } catch { }
            Debug.Log($"[RelayManager] HOST setup complete. PlayerId: {playerId}");
            Debug.Log($"[RelayManager] HOST Token prefix: {hostTokenPrefix}");
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
            string pid = "N/A";
            string tokenPrefix = "N/A";
            try {
                pid = AuthenticationService.Instance.PlayerId;
                string token = AuthenticationService.Instance.AccessToken;
                tokenPrefix = token != null && token.Length > 20 ? token.Substring(0, 20) + "..." : token;
            } catch { }
            Debug.Log($"[RelayManager] ═══════════════════════════════");
            Debug.Log($"[RelayManager] Joining with code: '{cleanCode}' (length={cleanCode.Length})");
            Debug.Log($"[RelayManager] Auth PlayerId: {pid}");
            Debug.Log($"[RelayManager] Token prefix: {tokenPrefix}");
            Debug.Log($"[RelayManager] CloudProjectId: {UnityEngine.Application.cloudProjectId}");

            // Log Relay SDK internal API base path
            try
            {
                var relayImpl = RelayService.Instance;
                var implType = relayImpl.GetType();
                var sdkField = implType.GetField("m_RelayService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (sdkField != null)
                {
                    var sdk = sdkField.GetValue(relayImpl);
                    var configProp = sdk.GetType().GetProperty("Configuration");
                    if (configProp != null)
                    {
                        var config = configProp.GetValue(sdk);
                        var baseProp = config.GetType().GetProperty("BasePath");
                        if (baseProp != null)
                            Debug.Log($"[RelayManager] Relay API BasePath: {baseProp.GetValue(config)}");
                    }
                }
            }
            catch (System.Exception ex) { Debug.Log($"[RelayManager] Could not read BasePath: {ex.Message}"); }

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
                    Debug.LogWarning($"[RelayManager] Attempt {attempt} failed: {retryEx.GetType().Name}: {retryEx.Message}");
                    if (retryEx.InnerException != null)
                        Debug.LogWarning($"[RelayManager]   Inner: {retryEx.InnerException.GetType().Name}: {retryEx.InnerException.Message}");

                    if (attempt < joinRetryCount)
                    {
                        Debug.Log($"[RelayManager] Retrying in {joinRetryDelay}s...");
                        await Task.Delay((int)(joinRetryDelay * 1000));
                    }
                }
            }

            if (joinAlloc == null)
            {
#if UNITY_EDITOR
                // ═══════════════════════════════════════════════════════
                // FALLBACK: Đọc relay data từ file (đã được Host pre-fetch)
                // Bypass Relay API 404 bug giữa 2 editor cùng máy
                // ═══════════════════════════════════════════════════════
                Debug.LogWarning($"[RelayManager] SDK Join failed. Trying file-based fallback...");
                string projectDir = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);
                if (ParrelSync.ClonesManager.IsClone())
                    projectDir = ParrelSync.ClonesManager.GetOriginalProjectPath();
                string filePath = System.IO.Path.Combine(projectDir, "relay_join_data.json");

                if (System.IO.File.Exists(filePath))
                {
                    string json = System.IO.File.ReadAllText(filePath);
                    var data = JsonUtility.FromJson<RelayJoinData>(json);

                    if (data != null && data.JoinCode == cleanCode)
                    {
                        Debug.Log($"[RelayManager] ✅ File fallback: found matching relay data for code '{cleanCode}'");

                        // Apply trực tiếp lên static pending data
                        _fileBasedJoinData = data;
                        _pendingHostAllocation = null;
                        CurrentJoinCode = cleanCode;

                        Debug.Log($"[RelayManager] CLIENT setup complete (file fallback). Join code: {cleanCode}");
                        return true;
                    }
                    else
                    {
                        Debug.LogError($"[RelayManager] File data code mismatch: file='{data?.JoinCode}' vs expected='{cleanCode}'");
                    }
                }
                else
                {
                    Debug.LogError($"[RelayManager] Fallback file not found: {filePath}");
                }
#endif
                Debug.LogError($"[RelayManager] Join failed after {joinRetryCount} attempts. Last error: {lastEx?.Message}");
                OnRelayError?.Invoke($"Join failed: {lastEx?.Message}");
                return false;
            }

            _pendingClientAllocation = joinAlloc;
            _pendingHostAllocation = null;
            _fileBasedJoinData = null;
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

        // ── CLIENT (file-based fallback) ──
        if (_fileBasedJoinData != null)
        {
            var data = _fileBasedJoinData;
            Debug.Log($"[RelayManager] Applying CLIENT relay data (file fallback):");
            Debug.Log($"[RelayManager]   ServerEndpoint: {data.ServerHost}:{data.ServerPort} (secure={data.IsSecure})");
            Debug.Log($"[RelayManager]   Region: {data.Region}");

            utp.SetClientRelayData(
                data.ServerHost,
                data.ServerPort,
                System.Convert.FromBase64String(data.AllocationIdBytes),
                System.Convert.FromBase64String(data.Key),
                System.Convert.FromBase64String(data.ConnectionData),
                System.Convert.FromBase64String(data.HostConnectionData),
                data.IsSecure
            );

            Debug.Log("[RelayManager] ✅ CLIENT relay data applied (file fallback).");
            _fileBasedJoinData = null;
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
        _fileBasedJoinData = null;
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
