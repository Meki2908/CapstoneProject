using UnityEngine;
using Fusion;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class PlayerNetworkSetup : NetworkBehaviour
{
    public static CinemachineCamera LocalCinemachineCamera { get; private set; }
    public static PlayerCameraDistanceManager LocalCameraDistanceManager { get; private set; }

    [Header("Camera Settings")]
    [Tooltip("Prefab Camera (chứa CinemachineCamera). Khi LocalPlayer xuất hiện, nó sẽ tự spawn cái này ra ngoài Scene và gắn vào đầu.")]
    [SerializeField] private GameObject playerCameraPrefab;
    [Tooltip("Điểm Camera sẽ Follow và LookAt (Ví dụ: Transform của Head hoặc Spine). Nếu để trống sẽ lấy gốc của Player.")]
    [SerializeField] private Transform cameraLookPoint;

    [Header("Nameplate (multiplayer)")]
    [Tooltip("World-space name tag prefab (e.g. Player's name). Parent under socket on head so it follows hit-react motion; script billboards to camera.")]
    [SerializeField] private GameObject nameplatePrefab;
    [Tooltip("Empty/socket on head. Falls back to Camera LookPoint when unset.")]
    [SerializeField] private Transform nameplateMountPoint;

    private GameObject _spawnedCamera;
    private GameObject _spawnedNameplate;

    public override void Spawned()
    {
        base.Spawned();

        var cc = GetComponentInChildren<CharacterController>(true);
        if (cc != null)
            cc.enabled = HasStateAuthority || HasInputAuthority;

        var pi = GetComponentInChildren<PlayerInput>(true);

        if (HasInputAuthority)
        {
            Debug.Log($"[PlayerNetworkSetup] Spawned local player. Will setup camera. scene={gameObject.scene.name} player={name}");

            if (pi != null)
            {
                pi.enabled = true;
                try
                {
                    if (pi.currentActionMap != null && pi.currentActionMap.name != "Player")
                        pi.SwitchCurrentActionMap("Player");
                }
                catch { }
            }

            SetupCamera();
        }
        else
        {
            if (pi != null)
                pi.enabled = false;
        }

        TrySpawnNameplate();
    }

    void TrySpawnNameplate()
    {
        if (Runner == null || nameplatePrefab == null)
            return;
        if (Runner.GameMode == GameMode.Single)
            return;

        Transform mount = nameplateMountPoint != null ? nameplateMountPoint : cameraLookPoint;
        if (mount == null)
            mount = transform;

        _spawnedNameplate = Instantiate(nameplatePrefab, mount);
        _spawnedNameplate.transform.localPosition = Vector3.zero;
        _spawnedNameplate.transform.localRotation = Quaternion.identity;

        if (_spawnedNameplate.TryGetComponent(out PlayerNameplate plate))
        {
            var ch = GetComponent<Character>();
            if (ch != null)
                plate.Bind(ch);
        }

        if (HasInputAuthority)
            _spawnedNameplate.SetActive(false);
    }

    public void SetupCamera()
    {
        if (playerCameraPrefab == null)
        {
            Debug.LogWarning("[PlayerNetworkSetup] Chưa gán playerCameraPrefab!");
            return;
        }

        // Tạo ra Camera từ Prefab ở ngoài Scene, hoàn toàn tách biệt khỏi Player
        _spawnedCamera = Instantiate(playerCameraPrefab);
        
        // Cần đảm bảo Camera được sinh ra cùng Scene với NetworkRunner (rất quan trọng khi test Multi-peer trong Editor)
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_spawnedCamera, gameObject.scene);

        CinemachineCamera cvc = _spawnedCamera.GetComponentInChildren<CinemachineCamera>();
        if (cvc != null)
        {
            Transform target = cameraLookPoint != null ? cameraLookPoint : this.transform;
            cvc.Follow = target;
            cvc.LookAt = target;
            
            _spawnedCamera.transform.position = target.position;
            _spawnedCamera.transform.rotation = target.rotation;

            cvc.PreviousStateIsValid = false;

            // Expose for other local-player systems (EnemyDetection, Zoom, etc.)
            LocalCinemachineCamera = cvc;
            Debug.Log($"[PlayerNetworkSetup] LocalCinemachineCamera READY: {cvc.name} (scene={_spawnedCamera.scene.name})");

            // Ensure distance manager exists on the spawned camera rig
            var mgr = _spawnedCamera.GetComponentInChildren<PlayerCameraDistanceManager>(true);
            if (mgr == null)
                mgr = _spawnedCamera.AddComponent<PlayerCameraDistanceManager>();
            LocalCameraDistanceManager = mgr;

            // KIỂM SOÁT TỐI CAO: Ép Main Camera nhảy thẳng tới vị trí nhân vật ngay lập tức
            // Tránh việc CinemachineBrain "blend" từ một góc nhìn xa (như gốc toạ độ) tới nhân vật
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = target.position;
                mainCam.transform.rotation = target.rotation;
                
                var brain = mainCam.GetComponent<CinemachineBrain>();
                if (brain != null)
                {
                    // Mẹo Unity: Tắt/Bật CinemachineBrain sẽ buộc nó reset toàn bộ các hiệu ứng blend đang tính toán
                    brain.enabled = false;
                    brain.enabled = true;
                }
            }
        }
        else
        {
            Debug.LogWarning("[PlayerNetworkSetup] Không tìm thấy CinemachineCamera trong prefab Camera!");
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_spawnedNameplate != null)
        {
            Destroy(_spawnedNameplate);
            _spawnedNameplate = null;
        }

        // Khi người chơi thoát, xóa luôn camera của họ
        if (_spawnedCamera != null)
        {
            Destroy(_spawnedCamera);
        }

        if (HasInputAuthority)
        {
            if (LocalCinemachineCamera != null && _spawnedCamera != null && LocalCinemachineCamera.transform.IsChildOf(_spawnedCamera.transform))
                LocalCinemachineCamera = null;
            if (LocalCameraDistanceManager != null && _spawnedCamera != null && LocalCameraDistanceManager.transform.IsChildOf(_spawnedCamera.transform))
                LocalCameraDistanceManager = null;
        }
        base.Despawned(runner, hasState);
    }
}




