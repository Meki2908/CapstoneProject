using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Text;
using UnityEngine.SceneManagement;
using Fusion.Photon.Realtime;

namespace Artsystack.ArtsystackGui
{
    public class FusionConnectionManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        [Header("Prefabs")]
        [Tooltip("Player Prefab to spawn when a client/host joins")]
        [SerializeField] private NetworkPrefabRef playerPrefab;

        [Header("Scene Settings")]
        [Tooltip("Build index of the Map_Chinh scene. Make sure it's added to Build Settings!")]
        [SerializeField] private int gameSceneIndex = 1;

        [Header("Debug")]
        [Tooltip("Extra logs for join/client StartGame (token length, result).")]
        [SerializeField] private bool fusionVerboseJoinLogging = false;

        [Tooltip("Console filter: JoinPassCrit — password-step client join + host OnConnectRequest.")]
        [SerializeField] private bool joinPasswordCritLogging = false;

        [Tooltip("Console filter: JoinPassCrit — singleton Awake/OnDestroy (why Instance null or object moved).")]
        [SerializeField] private bool fusionSingletonLifecycleCrit = false;

        [Header("Join safety")]
        [Tooltip("If client StartGame never completes (stuck join), force Shutdown after this many seconds so loading UI can clear and the session can recover.")]
        [SerializeField, Min(8f)] private float clientJoinStartGameTimeoutSeconds = 22f;

        [Header("Runtime disconnect recovery")]
        [Tooltip("Build index of menu scene (UI_Game). Used when a fatal disconnect happens while player is already in gameplay scenes.")]
        [SerializeField] private int menuSceneBuildIndex = 0;
        [Tooltip("When true, fatal disconnects auto-return to menu to avoid indefinite loading screens.")]
        [SerializeField] private bool autoReturnToMenuOnFatalDisconnect = true;
        [Tooltip("When true, always force-close networking loading UI on fatal disconnect.")]
        [SerializeField] private bool forceFinishLoadingUiOnFatalDisconnect = true;

        public static FusionConnectionManager Instance { get; private set; }

        /// <summary>
        /// If <see cref="Instance"/> is null but a manager still exists (inactive at boot, script order, etc.), re-bind the static.
        /// Call before Host/Join from UI.
        /// </summary>
        public static bool TryResolveInstance()
        {
            if (Instance != null)
                return true;

            var all = FindObjectsByType<FusionConnectionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
                return false;

            FusionConnectionManager best = null;
            foreach (var c in all)
            {
                if (c == null) continue;
                if (best == null)
                    best = c;
                if (c.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    best = c;
                    break;
                }
            }

            if (best == null)
                return false;

            Instance = best;
            if (best.gameObject.scene.name != "DontDestroyOnLoad")
            {
                best.transform.SetParent(null);
                DontDestroyOnLoad(best.gameObject);
            }
            Debug.LogWarning($"[FusionCM] TryResolveInstance: Instance was null; reassigned to \"{best.gameObject.name}\" (scene={best.gameObject.scene.name}).");
            return true;
        }

        public NetworkRunner Runner { get; private set; }

        public event Action<string> OnConnectionError;

        /// <summary>Fired after Host or Client <see cref="NetworkRunner.StartGame"/> succeeds (not Single).</summary>
        public event Action<RecentFusionSessionEntry> OnSessionConnected;

        /// <summary>Fired when Photon returns an updated session list (e.g. after <see cref="JoinLobbyAsync"/>).</summary>
        public event Action<List<SessionInfo>> OnSessionListUpdatedEvent;

        /// <summary>While true, <see cref="OnShutdown"/> / <see cref="OnConnectFailed"/> skip raising <see cref="OnConnectionError"/> (handled by join attempt result).</summary>
        bool _joinStartGameInFlight;

        /// <summary>Frame when a client <see cref="NetworkRunner.StartGame"/> returned <c>!Ok</c>; avoids duplicate errors from <see cref="OnShutdown"/>.</summary>
        int _joinFailureFrame = -1;

        /// <summary>
        /// Join wizard calls <see cref="TryJoinRoomAsync"/> with <c>suppressGlobalConnectionErrors: true</c>.
        /// Fusion may still invoke <see cref="OnConnectFailed"/> / <see cref="OnShutdown"/> a frame later, which would
        /// raise <see cref="OnConnectionError"/> and close the wizard before the password step is shown.
        /// </summary>
        float _joinWizardSuppressGlobalErrorsUntil;

        bool IsJoinWizardSuppressingGlobalErrors() => Time.unscaledTime < _joinWizardSuppressGlobalErrorsUntil;

        /// <summary>Mirrors last <see cref="TryJoinRoomAsync"/> <paramref name="startNetworkLoadingUi"/>; avoids <see cref="SceneTransitionManager.FinishLoadingUI"/> when no networking loading was started.</summary>
        bool _joinTryUsedNetworkingLoadingUi;

        /// <summary>Child object holds <see cref="NetworkRunner"/> so Fusion Shutdown(destroyGameObject:true) cannot delete this manager's GameObject.</summary>
        const string RunnerHostChildName = "FusionNetworkRunnerHost";

        /// <summary>Renamed during <see cref="PrepareRunnerForNewOperation"/> so <see cref="GetOrCreateRunnerHostTransform"/> does not find the dying child in the same frame.</summary>
        const string RunnerHostPendingDestroyName = "FusionRunner_PendingDestroy";

        /// <summary>Tracks <see cref="NetworkRunner.GetInstanceID"/> we called <see cref="NetworkRunner.AddCallbacks"/> for (runner lives on child, not on this component's GO).</summary>
        int _callbacksRegisteredForRunnerId = int.MinValue;
        bool _fatalDisconnectHandlingInProgress;
        bool _runnerRecoveryInProgress;

        private void Awake()
        {
            var scene = gameObject.scene.name;
            var parentBefore = transform.parent != null ? transform.parent.name : "(root)";
            int id = GetInstanceID();

            if (Instance == null)
            {
                Instance = this;
                // Root before DontDestroyOnLoad so the whole object survives scene unload (children of scene roots can behave badly).
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                LogSingletonCrit($"Awake WINNER id={id} name=\"{gameObject.name}\" scene={scene} parentWas={parentBefore} → SetParent(null)+DDOL activeSelf={gameObject.activeInHierarchy}");
            }
            else if (Instance != this)
            {
                int winnerId = Instance != null ? Instance.GetInstanceID() : 0;
                LogSingletonCrit($"Awake DUPLICATE id={id} name=\"{gameObject.name}\" scene={scene} parent={parentBefore} → Destroy (winner id={winnerId} name=\"{(Instance != null ? Instance.gameObject.name : "?")}\")");
                Destroy(gameObject);
            }
            else
            {
                LogSingletonCrit($"Awake SAME_INSTANCE id={id} (re-enter Awake on same object — unusual)");
            }
        }

        private void OnDestroy()
        {
            int id = GetInstanceID();
            if (Instance == this)
            {
                LogSingletonCrit($"OnDestroy SINGLETON id={id} name=\"{gameObject.name}\" → Instance=null (after this, FusionConnectionManager.Instance is null until new Awake)");
                Instance = null;
            }
            else
            {
                LogSingletonCrit($"OnDestroy NON-SINGLETON id={id} name=\"{gameObject.name}\" (Instance points elsewhere or null; no Instance clear)");
            }
        }
        // NOTE: Main-map bootstrap is handled by MapChinhBootstrapper + PlayerSpawner (Option B).
        // This manager is kept for menu flows (Host/Join/Single from UI_Game).

        Transform GetOrCreateRunnerHostTransform()
        {
            var t = transform.Find(RunnerHostChildName);
            if (t != null)
                return t;
            var go = new GameObject(RunnerHostChildName);
            t = go.transform;
            t.SetParent(transform, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            return t;
        }

        /// <summary>Scene manager must live on the same GameObject as <see cref="Runner"/> (child host or legacy parent).</summary>
        NetworkSceneManagerDefault GetSceneManagerForRunner()
        {
            if (Runner == null)
                return null;
            var sm = Runner.GetComponent<NetworkSceneManagerDefault>();
            if (sm == null)
                sm = Runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
            return sm;
        }

        void EnsureCallbacksRegisteredWithRunner()
        {
            if (Runner == null) return;
            int id = Runner.GetInstanceID();
            if (id == _callbacksRegisteredForRunnerId) return;
            Runner.AddCallbacks(this);
            _callbacksRegisteredForRunnerId = id;
        }

        private void EnsureRunnerExists()
        {
            var legacyRunner = gameObject.GetComponent<NetworkRunner>();
            if (legacyRunner != null && legacyRunner.gameObject == gameObject &&
                (legacyRunner.IsRunning || legacyRunner.IsCloudReady))
            {
                Runner = legacyRunner;
                GetSceneManagerForRunner();
                EnsureCallbacksRegisteredWithRunner();
                return;
            }

            var host = GetOrCreateRunnerHostTransform().gameObject;

            if (legacyRunner != null && legacyRunner.gameObject == gameObject)
            {
                if (!legacyRunner.IsRunning && !legacyRunner.IsCloudReady)
                {
                    Destroy(legacyRunner);
                    var legacySm = gameObject.GetComponent<NetworkSceneManagerDefault>();
                    if (legacySm != null)
                        Destroy(legacySm);
                    if (Runner == legacyRunner)
                        Runner = null;
                }
            }

            if (Runner == null || Runner.gameObject != host)
                Runner = host.GetComponent<NetworkRunner>();

            if (Runner == null)
            {
                Runner = host.AddComponent<NetworkRunner>();
                Runner.ProvideInput = true;
            }
            else
                Runner.ProvideInput = true;

            GetSceneManagerForRunner();
            EnsureCallbacksRegisteredWithRunner();
        }

        /// <summary>
        /// Always calls <see cref="NetworkRunner.Shutdown"/> before dropping a runner — never <see cref="Destroy"/>s an active
        /// <see cref="NetworkRunner"/> directly (avoids Fusion asserts / bad teardown). Child host uses <c>destroyGameObject: true</c>.
        /// </summary>
        async Task PrepareRunnerForNewOperation(bool logPassCrit = false)
        {
            if (Runner == null)
            {
                EnsureRunnerExists();
                return;
            }

            var oldRunner = Runner;
            Runner = null;
            _callbacksRegisteredForRunnerId = int.MinValue;

            bool runnerIsOnChild = oldRunner != null && oldRunner.gameObject != gameObject;

            if (runnerIsOnChild)
                oldRunner.gameObject.name = RunnerHostPendingDestroyName;

            if (logPassCrit)
                LogJoinPassCrit($"PrepareRunner: Shutdown (runnerOnChild={runnerIsOnChild} wasRunning={oldRunner != null && oldRunner.IsRunning} cloudReady={oldRunner != null && oldRunner.IsCloudReady})");

            try
            {
                await oldRunner.Shutdown(destroyGameObject: runnerIsOnChild, shutdownReason: ShutdownReason.Ok);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Fusion] PrepareRunnerForNewOperation Shutdown: {ex.Message}");
            }

            float deadline = Time.realtimeSinceStartup + 8f;
            while (oldRunner != null && (oldRunner.IsRunning || oldRunner.IsCloudReady) && Time.realtimeSinceStartup < deadline)
                await Task.Yield();

            if (logPassCrit)
                LogJoinPassCrit($"PrepareRunner: after wait oldRunnerNull={oldRunner == null} running={oldRunner != null && oldRunner.IsRunning} cloudReady={oldRunner != null && oldRunner.IsCloudReady}");

            if (!runnerIsOnChild && oldRunner != null)
            {
                var sm = gameObject.GetComponent<NetworkSceneManagerDefault>();
                if (sm != null)
                    Destroy(sm);
                Destroy(oldRunner);
            }

            EnsureRunnerExists();
        }

        /// <summary>True when there is no runner or it is not in a game session and not connected to Photon cloud (e.g. lobby).</summary>
        public bool IsNetworkingFullyIdle()
        {
            if (Runner == null)
                return true;
            return !Runner.IsRunning && !Runner.IsCloudReady;
        }

        void ReportFatalConnectionError(string message)
        {
            if (_fatalDisconnectHandlingInProgress)
                return;
            _fatalDisconnectHandlingInProgress = true;

            if (forceFinishLoadingUiOnFatalDisconnect && SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.FinishLoadingUI();

            OnConnectionError?.Invoke(message);

            if (autoReturnToMenuOnFatalDisconnect && menuSceneBuildIndex >= 0)
            {
                var active = SceneManager.GetActiveScene();
                if (active.buildIndex != menuSceneBuildIndex)
                {
                    Debug.LogWarning($"[Fusion] Fatal disconnect → loading menu scene index {menuSceneBuildIndex} from '{active.name}'");
                    SceneManager.LoadScene(menuSceneBuildIndex);
                }
            }

            _ = RecoverRunnerAfterFatalDisconnectAsync();
        }

        async Task RecoverRunnerAfterFatalDisconnectAsync()
        {
            if (_runnerRecoveryInProgress)
                return;
            _runnerRecoveryInProgress = true;
            try
            {
                var oldRunner = Runner;
                if (oldRunner != null && (oldRunner.IsRunning || oldRunner.IsCloudReady))
                {
                    bool destroyChild = oldRunner.gameObject != gameObject;
                    try
                    {
                        await oldRunner.Shutdown(destroyGameObject: destroyChild, shutdownReason: ShutdownReason.Error);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Fusion] RecoverRunnerAfterFatalDisconnect Shutdown: {ex.Message}");
                    }
                }

                Runner = null;
                _callbacksRegisteredForRunnerId = int.MinValue;
                EnsureRunnerExists();
            }
            finally
            {
                _runnerRecoveryInProgress = false;
                _fatalDisconnectHandlingInProgress = false;
            }
        }

        /// <summary>Join the default Photon session lobby so <see cref="OnSessionListUpdated"/> / <see cref="OnSessionListUpdatedEvent"/> receive public rooms.</summary>
        public async Task JoinLobbyAsync()
        {
            await PrepareRunnerForNewOperation();
            if (Runner == null)
                return;

            if (Runner.IsRunning)
            {
                // Do NOT fire OnSessionListUpdatedEvent with an empty list: MultiplayerMenuUI treats that as
                // "lobby returned zero rooms" and hides recent join rows. Leave lobby state unknown until runner is idle.
                Debug.LogWarning("[Fusion] JoinLobbyAsync skipped: runner already in a game session. " +
                                 "Recent join sessions will show as 'pending' until you leave Single/Host or shutdown Fusion.");
                return;
            }

            try
            {
                await Runner.JoinSessionLobby(SessionLobby.ClientServer, null, null, null, null, default, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Fusion] JoinLobbyAsync failed: {ex.Message}");
                // Same as above: empty list makes UI think Photon confirmed zero rooms — keep unknown (null) instead.
            }
        }

        /// <summary>
        /// Empty password must use <c>null</c>, not <c>new byte[0]</c>: Fusion native connect can throw
        /// <c>Trying to allocate &lt;= bytes: 0</c> in <c>Native.Malloc</c> when token length is 0.
        /// Host <see cref="OnConnectRequest"/> still treats null / empty as &quot;no token&quot;.
        /// </summary>
        static byte[] BuildPasswordConnectionToken(string password)
        {
            if (string.IsNullOrEmpty(password)) return null;
            return Encoding.UTF8.GetBytes(password);
        }

        void LogJoinDiag(string message)
        {
            if (fusionVerboseJoinLogging)
                Debug.Log($"[Fusion][JoinDiag] {message}");
        }

        public void SetJoinPasswordCritLogging(bool enabled) => joinPasswordCritLogging = enabled;

        public void SetFusionSingletonLifecycleCrit(bool enabled) => fusionSingletonLifecycleCrit = enabled;

        void LogJoinPassCrit(string message)
        {
            if (!joinPasswordCritLogging) return;
            Debug.Log($"[JoinPassCrit] {message}");
        }

        void LogSingletonCrit(string message)
        {
            if (!fusionSingletonLifecycleCrit) return;
            Debug.Log($"[JoinPassCrit][FusionCM] {message}");
        }

        // RecreateRunnerComponents removed: handled by MapChinhBootstrapper when needed.

        public async void StartHost(string roomName, string password)
        {
            await StartHostInternalAsync(roomName, password, null, null);
        }

        /// <summary>Host từ lobby UI: giới hạn slot + ẩn session list khi phòng kín.</summary>
        public async void StartHostWithLobbyOptions(string roomName, string password, int playerCount, bool isPrivateRoom)
        {
            await StartHostInternalAsync(roomName, password, playerCount, isPrivateRoom);
        }

        async Task StartHostInternalAsync(string roomName, string password, int? playerCount, bool? isPrivateRoom)
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.StartNetworkingLoadingUI();

            await PrepareRunnerForNewOperation();

            hostPassword = password ?? "";

            byte[] token = BuildPasswordConnectionToken(hostPassword);

            var sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(gameSceneIndex), LoadSceneMode.Single);

            var args = new StartGameArgs()
            {
                GameMode = GameMode.Host,
                SessionName = roomName,
                ConnectionToken = token,
                Scene = sceneInfo,
                SceneManager = GetSceneManagerForRunner()
            };

            if (playerCount.HasValue && playerCount.Value > 0)
                args.PlayerCount = playerCount.Value;
            if (isPrivateRoom.HasValue)
                args.IsVisible = !isPrivateRoom.Value;

            var result = await Runner.StartGame(args);

            if (!result.Ok)
            {
                Debug.LogError($"[Fusion] Failed to start Host: {result.ShutdownReason}");
                if (SceneTransitionManager.Instance != null)
                    SceneTransitionManager.Instance.FinishLoadingUI();
                OnConnectionError?.Invoke($"Lỗi tạo phòng: {result.ShutdownReason}");
                return;
            }

            OnSessionConnected?.Invoke(new RecentFusionSessionEntry
            {
                roomName = roomName ?? "",
                password = hostPassword ?? "",
                wasHost = true,
                lastUsedUtcTicks = DateTime.UtcNow.Ticks,
                hostPlayerCount = playerCount ?? 4,
                hostIsPrivate = isPrivateRoom ?? false
            });
        }

        /// <summary>Join as client with the same scene setup as host. Does not raise <see cref="OnConnectionError"/> on failure if <paramref name="suppressGlobalConnectionErrors"/> is true.</summary>
        public async Task<StartGameResult> TryJoinRoomAsync(string roomName, string password, bool startNetworkLoadingUi, bool suppressGlobalConnectionErrors)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                if (startNetworkLoadingUi && SceneTransitionManager.Instance != null)
                    SceneTransitionManager.Instance.FinishLoadingUI();
                Debug.LogWarning("[Fusion] TryJoinRoomAsync: empty room name.");
                return default;
            }

            // Only set true after StartNetworkingLoadingUI runs (post-join success) so FinishLoadingUI never runs for "never shown" flows.
            _joinTryUsedNetworkingLoadingUi = false;

            bool passWizardClientJoin = startNetworkLoadingUi && suppressGlobalConnectionErrors;
            if (passWizardClientJoin)
            {
                LogJoinPassCrit($"TryJoin ENTRY session=\"{roomName.Trim()}\" pwdLen={password?.Length ?? 0} " +
                                $"frame={Time.frameCount} realtime={Time.realtimeSinceStartup:F3}");
            }

            await PrepareRunnerForNewOperation(passWizardClientJoin);

            byte[] token = BuildPasswordConnectionToken(password);

            // Client: do not pass Scene — host / NetworkSceneManager drives loaded scene (avoids local load before auth → blue screen / desync).
            LogJoinDiag($"TryJoinRoomAsync start session=\"{roomName.Trim()}\" pwdLen={(password?.Length ?? 0)} " +
                        $"token={(token == null ? "null" : token.Length.ToString())} scene=host-driven " +
                        $"runnerIsRunning={(Runner != null && Runner.IsRunning)} startNetLoad={startNetworkLoadingUi} " +
                        $"suppressGlobalErr={suppressGlobalConnectionErrors}");

            if (passWizardClientJoin)
            {
                LogJoinPassCrit($"TryJoin after Prepare runnerNull={Runner == null} " +
                                $"isRunning={Runner != null && Runner.IsRunning} isCloudReady={Runner != null && Runner.IsCloudReady} " +
                                $"isShutdown={Runner != null && Runner.IsShutdown} " +
                                $"sceneMgr={(GetSceneManagerForRunner() != null)} " +
                                $"tokenNull={token == null} tokenLen={(token?.Length ?? 0)}");
            }

            _joinStartGameInFlight = true;
            StartGameResult result = default;
            try
            {
                if (passWizardClientJoin)
                    LogJoinPassCrit($"TryJoin calling StartGame Client session=\"{roomName.Trim()}\" …");

                var joinArgs = new StartGameArgs()
                {
                    GameMode = GameMode.Client,
                    SessionName = roomName.Trim(),
                    ConnectionToken = token,
                    SceneManager = GetSceneManagerForRunner()
                };

                var joinTask = Runner.StartGame(joinArgs);
                float timeoutSec = Mathf.Clamp(clientJoinStartGameTimeoutSeconds, 8f, 120f);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSec));
                var completed = await Task.WhenAny(joinTask, timeoutTask);

                if (ReferenceEquals(completed, timeoutTask))
                {
                    LogJoinDiag($"TryJoinRoomAsync StartGame TIMEOUT after {timeoutSec:F0}s — forcing Shutdown(OperationTimeout).");
                    if (passWizardClientJoin)
                        LogJoinPassCrit($"TryJoin StartGame TIMEOUT {timeoutSec:F0}s → Shutdown(OperationTimeout)");

                    _joinFailureFrame = Time.frameCount;
                    try
                    {
                        if (Runner != null && Runner.IsRunning)
                        {
                            bool destroyChild = Runner.gameObject != gameObject;
                            await Runner.Shutdown(destroyGameObject: destroyChild, shutdownReason: ShutdownReason.OperationTimeout);
                        }
                    }
                    catch (Exception shutEx)
                    {
                        Debug.LogWarning($"[Fusion] Join timeout Shutdown: {shutEx.Message}");
                    }

                    Runner = null;
                    _callbacksRegisteredForRunnerId = int.MinValue;
                    EnsureRunnerExists();

                    if (_joinTryUsedNetworkingLoadingUi && SceneTransitionManager.Instance != null)
                        SceneTransitionManager.Instance.FinishLoadingUI();
                    if (suppressGlobalConnectionErrors)
                        _joinWizardSuppressGlobalErrorsUntil = Time.unscaledTime + 2f;
                    if (!suppressGlobalConnectionErrors)
                        OnConnectionError?.Invoke("Kết nối quá thời gian — hãy thử lại.");

                    // StartGameResult is Fusion-defined (read-only); use default for failed join (Ok == false).
                    return default;
                }

                result = await joinTask;
            }
            catch (Exception ex)
            {
                if (passWizardClientJoin)
                    LogJoinPassCrit($"TryJoin StartGame EXCEPTION {ex.GetType().Name}: {ex.Message}");
                Debug.LogError($"[Fusion][JoinDiag] StartGame threw: {ex.Message}");
                Debug.LogException(ex);
                _joinFailureFrame = Time.frameCount;
                if (_joinTryUsedNetworkingLoadingUi && SceneTransitionManager.Instance != null)
                    SceneTransitionManager.Instance.FinishLoadingUI();
                if (suppressGlobalConnectionErrors)
                    _joinWizardSuppressGlobalErrorsUntil = Time.unscaledTime + 2f;
                if (!suppressGlobalConnectionErrors)
                    OnConnectionError?.Invoke($"Join error: {ex.Message}");
                return default;
            }
            finally
            {
                _joinStartGameInFlight = false;
            }

            LogJoinDiag($"TryJoinRoomAsync done Ok={result.Ok} ShutdownReason={result.ShutdownReason}");

            if (passWizardClientJoin)
            {
                LogJoinPassCrit($"TryJoin StartGame returned Ok={result.Ok} ShutdownReason={result.ShutdownReason} " +
                                $"_joinStartGameInFlight cleared; if Ok=false check host logs for OnConnectRequest / room name match");
            }

            if (!result.Ok)
            {
                _joinFailureFrame = Time.frameCount;
                if (passWizardClientJoin)
                    LogJoinPassCrit($"TryJoin FAIL reason={result.ShutdownReason} (ConnectionRefused often = wrong/missing password on host)");
                Debug.LogError($"[Fusion] Failed to join Client: {result.ShutdownReason}");
                if (_joinTryUsedNetworkingLoadingUi && SceneTransitionManager.Instance != null)
                    SceneTransitionManager.Instance.FinishLoadingUI();
                if (suppressGlobalConnectionErrors)
                    _joinWizardSuppressGlobalErrorsUntil = Time.unscaledTime + 2f;
                if (!suppressGlobalConnectionErrors)
                {
                    if (result.ShutdownReason == ShutdownReason.ConnectionRefused && !string.IsNullOrEmpty(password))
                        OnConnectionError?.Invoke("Password incorrect!");
                    else
                        OnConnectionError?.Invoke($"Không thể vào phòng: {result.ShutdownReason}");
                }
                return result;
            }

            if (passWizardClientJoin)
                LogJoinPassCrit($"TryJoin SUCCESS session=\"{roomName.Trim()}\" → OnSessionConnected (client)");

            if (startNetworkLoadingUi && SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.StartNetworkingLoadingUI();
                _joinTryUsedNetworkingLoadingUi = true;
            }

            OnSessionConnected?.Invoke(new RecentFusionSessionEntry
            {
                roomName = roomName.Trim(),
                password = password ?? "",
                wasHost = false,
                lastUsedUtcTicks = DateTime.UtcNow.Ticks,
                hostPlayerCount = 4,
                hostIsPrivate = false
            });

            return result;
        }

        /// <summary>After a failed public join, whether to show the password step (vs hard error on room name step).</summary>
        public static bool ShouldOfferPasswordStep(ShutdownReason reason)
        {
            if (reason == ShutdownReason.Ok) return false;
            switch (reason)
            {
                case ShutdownReason.GameNotFound:
                case ShutdownReason.GameClosed:
                case ShutdownReason.GameIsFull:
                case ShutdownReason.GameIdAlreadyExists:
                case ShutdownReason.InvalidArguments:
                case ShutdownReason.InvalidRegion:
                case ShutdownReason.MaxCcuReached:
                case ShutdownReason.OperationTimeout:
                    return false;
                case ShutdownReason.ConnectionRefused:
                case ShutdownReason.ConnectionTimeout:
                case ShutdownReason.PhotonCloudTimeout:
                case ShutdownReason.Error:
                default:
                    return true;
            }
        }

        public async void JoinRoom(string roomName, string password)
        {
            await TryJoinRoomAsync(roomName, password ?? "", true, false);
        }

        public async void StartSinglePlayer(string sceneName = "")
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.StartNetworkingLoadingUI();

            await PrepareRunnerForNewOperation();
            Debug.Log($"[FUSION][StartSinglePlayer] PrepareRunnerForNewOperation done. RunnerNull={(Runner == null)} " +
                      $"runnerGO={(Runner != null ? Runner.gameObject.name : "null")} " +
                      $"alreadyRunning={(Runner != null && Runner.IsRunning)} " +
                      $"activeScene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}({UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex}) " +
                      $"targetGameSceneIndex={gameSceneIndex}");

            // Fusion 2 uses SceneRef which is index-based. We use the gameSceneIndex from the Inspector.
            // You can also use SceneUtility.GetBuildIndexByScenePath if needed.
            var sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(gameSceneIndex), LoadSceneMode.Single);

            var result = await Runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Single,
                Scene = sceneInfo,
                SceneManager = GetSceneManagerForRunner()
            });

            if (!result.Ok)
            {
                Debug.LogError($"[Fusion] Failed to start Single Player: {result.ShutdownReason}");
                OnConnectionError?.Invoke($"Lỗi tạo phòng Singleplayer: {result.ShutdownReason}");
            }
        }

        // --- INetworkRunnerCallbacks ---

        /// <summary>Password for the current hosted session; enforced in <see cref="OnConnectRequest"/>.</summary>
        private string hostPassword = "";

        public async void StartHostWithPass(string roomName, string password)
        {
            await StartHostInternalAsync(roomName, password, null, null);
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (player == runner.LocalPlayer)
            {
                // T?t Loading Screen khi ch?nh m?nh d? join
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.FinishLoadingUI();
                }
            }
            // Player spawning is handled by PlayerSpawner callbacks.
        }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            // Player despawn: handled in <see cref="PlayerSpawner.OnPlayerLeft"/> (server Despawn + SetPlayerObject on join).
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            if (_joinStartGameInFlight || IsJoinWizardSuppressingGlobalErrors())
            {
                LogJoinDiag($"OnConnectFailed suppressed reason={reason} (wizard join recovery window)");
                if (_joinTryUsedNetworkingLoadingUi && SceneTransitionManager.Instance != null)
                    SceneTransitionManager.Instance.FinishLoadingUI();
                return;
            }
            Debug.LogError($"[Fusion] Connect Failed: {reason}");
            ReportFatalConnectionError($"Kết nối thất bại: {reason}");
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.LogWarning($"[Fusion] Runner Shutdown: {shutdownReason}");
            Debug.LogWarning($"[FUSION][OnShutdown] runner={runner?.name} reason={shutdownReason} " +
                             $"InstanceRunnerSame={(Runner == runner)} instanceRunnerRunning={(Runner != null && Runner.IsRunning)}");
            if (shutdownReason != ShutdownReason.Ok)
            {
                if (_joinStartGameInFlight || IsJoinWizardSuppressingGlobalErrors())
                {
                    LogJoinDiag($"OnShutdown suppressed reason={shutdownReason} (wizard join recovery window)");
                    return;
                }
                if (_joinFailureFrame >= 0 && Time.frameCount <= _joinFailureFrame + 1)
                    return;
                ReportFatalConnectionError($"Mất kết nối: {shutdownReason}");
            }
        }

        // C?c callbacks kh?c c?a INetworkRunnerCallbacks (B?t bu??c ph?i implement)
        public void OnInput(NetworkRunner runner, NetworkInput input) 
        { 
            // T?m nh?n v?t Local v? l?y t?i Data nh?t v?o du?ng truy?n m?ng
            if (PlayerNetworkInput.LocalInstance != null)
            {
                input.Set(PlayerNetworkInput.LocalInstance.GetLocalInput());
            }
        }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { Debug.Log("[Fusion] Connected to Server!"); }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            if (_joinStartGameInFlight || IsJoinWizardSuppressingGlobalErrors())
                return;
            ReportFatalConnectionError($"Mất kết nối server: {reason}");
        }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            bool hostHasPassword = !string.IsNullOrEmpty(hostPassword);
            if (fusionVerboseJoinLogging)
                Debug.Log($"[Fusion] OnConnectRequest: hostHasPassword={hostHasPassword}");

            if (!hostHasPassword)
            {
                LogJoinPassCrit("OnConnectRequest HOST → Accept (no host password set)");
                request.Accept();
                return;
            }

            if (token == null || token.Length == 0)
            {
                LogJoinPassCrit("OnConnectRequest HOST → Refuse (host has password, client token null/empty)");
                if (fusionVerboseJoinLogging)
                    Debug.LogWarning("[Fusion] Connection refused: room has password but client sent no token.");
                request.Refuse();
                return;
            }

            string clientPass = Encoding.UTF8.GetString(token);
            bool match = clientPass == hostPassword;
            if (match)
            {
                LogJoinPassCrit($"OnConnectRequest HOST → Accept (password length match hostLen={hostPassword.Length} clientLen={clientPass.Length})");
                request.Accept();
            }
            else
            {
                LogJoinPassCrit($"OnConnectRequest HOST → Refuse (password mismatch hostLen={hostPassword.Length} clientLen={clientPass.Length})");
                if (fusionVerboseJoinLogging)
                    Debug.LogWarning("[Fusion] Connection refused: password mismatch.");
                request.Refuse();
            }
        }
        
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            OnSessionListUpdatedEvent?.Invoke(sessionList ?? new List<SessionInfo>());
        }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        
        // Th?m cho Fusion 2
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    }
}





