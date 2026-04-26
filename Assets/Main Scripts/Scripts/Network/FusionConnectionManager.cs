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
                OnConnectionError?.Invoke($"Lỗi tạo phòng: {result.ShutdownReason}");
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
                OnConnectionError?.Invoke($"Không thể vào phòng: {result.ShutdownReason}");
            }
        }

        public async void StartSinglePlayer(string sceneName = "")
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.StartNetworkingLoadingUI();

            EnsureRunnerExists();

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
                OnConnectionError?.Invoke($"Lỗi tạo phòng Singleplayer: {result.ShutdownReason}");
            }
        }

        // --- INetworkRunnerCallbacks ---

        public void OnConnectionRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {

            // Lấy token của phòng hiện tại từ Host
            // (Khi tạo phòng Host, token không được tự lưu trong Runner.ConnectionToken, nên ta cần cách xác thực)
            // Vì FusionConnectionManager là Singleton, ta có thể lưu password hiện tại ở lúc tạo phòng.
            // Nhưng cách chuẩn là: Nếu phòng có pass, pass đó phải được kiểm tra.
            // Để đơn giản, ta sẽ lưu Host Password vào một biến và check.
        }

        // Lưu password khi tạo phòng
        private string hostPassword = "";
        
        public async void StartHostWithPass(string roomName, string password)
        {
            hostPassword = password;
            StartHost(roomName, password);
        }

        // --- Cập nhật lại logic kết nối có lưu pass (Sửa đổi StartHost ở trên bằng cách gọi SetHostPassword) ---
        // Do hàm trên đã được khai báo, ta sửa lại OnConnectionRequest để dùng biến hostPassword.

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (player == runner.LocalPlayer)
            {
                // T?t Loading Screen khi ch�nh m�nh d� join
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.FinishLoadingUI();
                }
            }

            if (runner.IsServer)
            {
                // 1. T�m t?t c? c�c object c� tag "SpawnPoint" trong scene
                GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
                Vector3 spawnPosition = Vector3.up * 5; // M?c d?nh n?u kh�ng t�m th?y g� th� spawn tr�n cao

                if (spawnPoints.Length > 0)
                {
                    // L?y ng?u nhi�n m?t di?m v� nh�ch l�n m?t ch�t cho an to�n
                    int index = player.PlayerId % spawnPoints.Length;
                    spawnPosition = spawnPoints[index].transform.position + Vector3.up * 1.5f;
                    Debug.Log($"[FLOW] �� t�m th?y {spawnPoints.Length} SpawnPoints. Spawn Player {player.PlayerId} t?i: {spawnPosition}");
                }
                else
                {
                    Debug.LogWarning("[FLOW] Kh�ng t�m th?y object n�o c� tag 'SpawnPoint'. Spawn m?c d?nh tr�n cao!");
                }

                // 2. Spawn nh�n v?t (Server n?m quy?n)
                NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
                Debug.Log($"[FLOW] Player d� du?c Spawn th�nh c�ng! PlayerID: {player.PlayerId}");
            }
        }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            // Tuỳ chọn: Xoá Player GameObject nếu cần (thường Fusion có tuỳ chọn tự huỷ NetworkObject khi client thoát)
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Debug.LogError($"[Fusion] Connect Failed: {reason}");
            OnConnectionError?.Invoke($"Kết nối thất bại: {reason}");
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.LogWarning($"[Fusion] Runner Shutdown: {shutdownReason}");
            if (shutdownReason != ShutdownReason.Ok)
            {
                OnConnectionError?.Invoke($"Mất kết nối: {shutdownReason}");
            }
        }

        // Các callbacks khác của INetworkRunnerCallbacks (Bắt buộc phải implement)
        public void OnInput(NetworkRunner runner, NetworkInput input) 
        { 
            // T�m nh�n v?t Local v� l?y t�i Data nh�t v�o du?ng truy?n m?ng
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
            // Phiên bản OnConnectRequest này là cho Fusion 2. 
            // Nếu dùng Fusion 1, callback là OnConnectionRequest. Chúng ta sẽ làm logic check pass ở đây.
            
            if (string.IsNullOrEmpty(hostPassword))
            {
                request.Accept(); // Không có pass
                return;
            }

            if (token == null || token.Length == 0)
            {
                request.Refuse(); // Có pass mà không gửi pass
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
        
        // Thêm cho Fusion 2
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    }
}





