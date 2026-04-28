using UnityEngine;
using Fusion;
using Unity.Cinemachine;

public class PlayerNetworkSetup : NetworkBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("Prefab Camera (chứa CinemachineCamera). Khi LocalPlayer xuất hiện, nó sẽ tự spawn cái này ra ngoài Scene và gắn vào đầu.")]
    [SerializeField] private GameObject playerCameraPrefab;
    [Tooltip("Điểm Camera sẽ Follow và LookAt (Ví dụ: Transform của Head hoặc Spine). Nếu để trống sẽ lấy gốc của Player.")]
    [SerializeField] private Transform cameraLookPoint;

    private GameObject _spawnedCamera;

    public override void Spawned()
    {
        base.Spawned();

        if (HasInputAuthority)
        {
            SetupCamera();
        }
        else
        {
            // Proxy: Không làm gì cả vì Camera đã bị xóa khỏi Prefab Player theo kiến trúc mới.
        }
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
        }
        else
        {
            Debug.LogWarning("[PlayerNetworkSetup] Không tìm thấy CinemachineCamera trong prefab Camera!");
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Khi người chơi thoát, xóa luôn camera của họ
        if (_spawnedCamera != null)
        {
            Destroy(_spawnedCamera);
        }
        base.Despawned(runner, hasState);
    }
}




