using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Text;
using UnityEngine.SceneManagement;

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

        public static FusionConnectionManager Instance { get; private set; }
        public NetworkRunner Runner { get; private set; }

        public event Action<string> OnConnectionError;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
            }
        }
        // NOTE: Main-map bootstrap is handled by MapChinhBootstrapper + PlayerSpawner (Option B).
        // This manager is kept for menu flows (Host/Join/Single from UI_Game).

        private void EnsureRunnerExists()
        {
            if (Runner == null)
            {
                Runner = gameObject.GetComponent<NetworkRunner>();
                if (Runner == null)
                    Runner = gameObject.AddComponent<NetworkRunner>();
                
                Runner.ProvideInput = true;
            }

            if (gameObject.GetComponent<NetworkSceneManagerDefault>() == null)
            {
                gameObject.AddComponent<NetworkSceneManagerDefault>();
            }
        }

        // RecreateRunnerComponents removed: handled by MapChinhBootstrapper when needed.

        public async void StartHost(string roomName, string password)
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.StartNetworkingLoadingUI();

            EnsureRunnerExists();

            byte[] token = string.IsNullOrEmpty(password) ? new byte[0] : Encoding.UTF8.GetBytes(password);

            var sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(gameSceneIndex), LoadSceneMode.Single);

            var result = await Runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Host,
                SessionName = roomName,
                ConnectionToken = token,
                Scene = sceneInfo,
                SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>()
            });

            if (!result.Ok)
            {
                Debug.LogError($"[Fusion] Failed to start Host: {result.ShutdownReason}");
                OnConnectionError?.Invoke($"L??i t?o ph?ng: {result.ShutdownReason}");
            }
        }

        public async void JoinRoom(string roomName, string password)
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.StartNetworkingLoadingUI();

            EnsureRunnerExists();

            byte[] token = string.IsNullOrEmpty(password) ? new byte[0] : Encoding.UTF8.GetBytes(password);

            var result = await Runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.Client,
                SessionName = roomName,
                ConnectionToken = token,
                SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>()
            });

            if (!result.Ok)
            {
                Debug.LogError($"[Fusion] Failed to join Client: {result.ShutdownReason}");
                OnConnectionError?.Invoke($"Kh?ng th?? v?o ph?ng: {result.ShutdownReason}");
            }
        }

        public async void StartSinglePlayer(string sceneName = "")
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.StartNetworkingLoadingUI();

            EnsureRunnerExists();
            Debug.Log($"[FUSION][StartSinglePlayer] EnsureRunnerExists done. RunnerNull={(Runner == null)} " +
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
                SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>()
            });

            if (!result.Ok)
            {
                Debug.LogError($"[Fusion] Failed to start Single Player: {result.ShutdownReason}");
                OnConnectionError?.Invoke($"L??i t?o ph?ng Singleplayer: {result.ShutdownReason}");
            }
        }

        // --- INetworkRunnerCallbacks ---

        public void OnConnectionRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {

            // L?y token c?a ph?ng hi??n t?i t? Host
            // (Khi t?o ph?ng Host, token kh?ng ????c t? l?u trong Runner.ConnectionToken, n?n ta c?n c?ch x?c th?c)
            // V? FusionConnectionManager l? Singleton, ta c? th?? l?u password hi??n t?i ?? l?c t?o ph?ng.
            // Nh?ng c?ch chu?n l?: N?u ph?ng c? pass, pass ??? ph?i ????c ki??m tra.
            // ??? ???n gi?n, ta s? l?u Host Password v?o m??t bi?n v? check.
        }

        // L?u password khi t?o ph?ng
        private string hostPassword = "";
        
        public async void StartHostWithPass(string roomName, string password)
        {
            hostPassword = password;
            StartHost(roomName, password);
        }

        // --- C?p nh?t l?i logic k?t n??i c? l?u pass (S?a ????i StartHost ?? tr?n b?ng c?ch g?i SetHostPassword) ---
        // Do h?m tr?n ??? ????c khai b?o, ta s?a l?i OnConnectionRequest ???? d?ng bi?n hostPassword.

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
            // Tu? ch?n: Xo? Player GameObject n?u c?n (th??ng Fusion c? tu? ch?n t? hu? NetworkObject khi client tho?t)
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Debug.LogError($"[Fusion] Connect Failed: {reason}");
            OnConnectionError?.Invoke($"K?t n??i th?t b?i: {reason}");
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.LogWarning($"[Fusion] Runner Shutdown: {shutdownReason}");
            Debug.LogWarning($"[FUSION][OnShutdown] runner={runner?.name} reason={shutdownReason} " +
                             $"InstanceRunnerSame={(Runner == runner)} instanceRunnerRunning={(Runner != null && Runner.IsRunning)}");
            if (shutdownReason != ShutdownReason.Ok)
            {
                OnConnectionError?.Invoke($"M?t k?t n??i: {shutdownReason}");
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
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) 
        { 
            // Phi?n b?n OnConnectRequest n?y l? cho Fusion 2. 
            // N?u d?ng Fusion 1, callback l? OnConnectionRequest. Ch?ng ta s? l?m logic check pass ?? ???y.
            
            if (string.IsNullOrEmpty(hostPassword))
            {
                request.Accept(); // Kh?ng c? pass
                return;
            }

            if (token == null || token.Length == 0)
            {
                request.Refuse(); // C? pass m? kh?ng g?i pass
                return;
            }

            string clientPass = Encoding.UTF8.GetString(token);
            if (clientPass == hostPassword)
            {
                request.Accept();
            }
            else
            {
                request.Refuse();
            }
        }
        
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
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





