using Fusion;
using UnityEngine;

/// <summary>
/// Gắn trên root prefab player (cùng <see cref="NetworkObject"/>).
/// Đảm bảo tất cả component cần thiết được thêm + set spawn position cho CẢ local VÀ remote.
///
/// Cải tiến so với bản cũ:
/// - Spawn cho TẤT CẢ players (local và remote) dựa trên PlayerId
/// - Tự động gán InputAuthority cho player object
/// - Đợi PlayerSpawnConfig sẵn sàng trước khi teleport
/// - Force teleport nếu spawn lệch
/// </summary>
[DefaultExecutionOrder(-200)]
public class NetworkPlayerSpawnSnap : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Thử teleport lại sau bao lâu nếu spawn lệch (giây). Đặt 0 để tắt.")]
    [SerializeField] private float resnapDelay = 0.5f;
    [Tooltip("Khoảng cách tối đa để coi là 'spawn đúng vị trí'.")]
    [SerializeField] private float snapDistanceThreshold = 0.5f;
    [Tooltip("Số frame tối đa đợi spawn config sẵn sàng trước khi fallback (0 = không đợi).")]
    [SerializeField] private int maxWaitFrames = 60;

    private bool _hasSnapped;
    private float _resnapTimer;
    private Vector3 _targetSpawnPos;
    private Quaternion _targetSpawnRot;
    private bool _spawnConfigChecked;
    private int _waitFrames;

    /// <summary>Thời gian (giây) cho phép resnap sau spawn. Hết thời gian → tắt resnap vĩnh viễn.</summary>
    [Tooltip("Thời gian (giây) cho phép resnap sau khi spawn. Sau đó tắt resnap.")]
    [SerializeField] private float resnapGracePeriod = 2f;
    private float _resnapGraceTimer;
    private bool _resnapDisabled;

    void Awake()
    {
        // ❌ TẤT CẢ các component mạng (NetworkPlayerRootFollowBody, NetworkAnimatorSync...)
        // BẮT BUỘC phải được gắn sẵn vào Prefab từ Editor! KHÔNG ĐƯỢC DÙNG AddComponent TẠI ĐÂY!
        // Vui lòng dùng Tools -> Setup Player Prefab (Multiplayer) để tự động thêm vào Prefab.

        _resnapTimer = 0f;
        _spawnConfigChecked = false;
    }

    public override void Spawned()
    {
        _hasSnapped = false;
        _resnapTimer = 0f;
        _spawnConfigChecked = false;
        _waitFrames = 0;
        _resnapDisabled = false;
        _resnapGraceTimer = resnapGracePeriod;

        // Tính spawn position dựa trên PlayerId
        CalculateSpawnPosition();

        // Snap NGAY tại Spawned — trước khi FixedUpdateNetwork chạy lần đầu.
        // CC đang disabled (LocalOwnership chưa chạy), teleport trực tiếp là an toàn.
        ApplySnapPosition();
    }

    private void CalculateSpawnPosition()
    {
        if (Runner == null)
        {
            Debug.LogWarning("[SpawnSnap] Runner is null! Cannot calculate spawn.");
            return;
        }

        int playerIndex = GetPlayerIndex();
        Vector3 pos;
        Quaternion rot;

        if (PlayerSpawnConfig.SpawnPointCount > 0)
        {
            // Dùng spawn point riêng cho mỗi player dựa trên index
            var spawnPoint = PlayerSpawnConfig.GetSpawnPoint(playerIndex);
            if (spawnPoint != null)
            {
                pos = spawnPoint.position;
                rot = spawnPoint.rotation;
                Debug.Log($"[SpawnSnap] Player {playerIndex} spawn at spawn point: {spawnPoint.name} → {pos}");
            }
            else
            {
                // Spawn point null → dùng fallback theo index
                pos = PlayerSpawnConfig.GetSpawnPosition(playerIndex);
                rot = Quaternion.identity;
                Debug.LogWarning($"[SpawnSnap] Spawn point [{playerIndex}] is null! Using fallback: {pos}");
            }
        }
        else
        {
            // Không có spawn config → đợi thêm
            Debug.Log($"[SpawnSnap] No spawn config yet for player {playerIndex} — will wait.");
            _spawnConfigChecked = false;
            return;
        }

        _targetSpawnPos = pos;
        _targetSpawnRot = rot;
        _spawnConfigChecked = true;
    }

    private int GetPlayerIndex()
    {
        // Phải khớp MultiplayerManager: index = PlayerId - 1 (không dùng thứ tự foreach ActivePlayers — thứ tự đó không đảm bảo).
        if (Runner == null || Object == null || !Object.IsValid)
            return 0;

        var auth = Object.InputAuthority;
        if (auth != PlayerRef.None)
            return Mathf.Max(0, auth.PlayerId - 1);

        // Spawned() đôi khi chạy trước khi InputAuthority gán xong — dùng object map của LocalPlayer.
        var localObj = Runner.GetPlayerObject(Runner.LocalPlayer);
        if (localObj != null && localObj == Object)
            return Mathf.Max(0, Runner.LocalPlayer.PlayerId - 1);

        return 0;
    }

    private Vector3 GetFallbackSpawnPosition(int playerIndex)
    {
        Debug.LogError($"[SpawnSnap] SpawnPoint[{playerIndex}] fallback called — BUG! Spawn points must be set in scene.");
        return Vector3.zero;
    }

    public override void FixedUpdateNetwork()
    {
        if (Runner == null) return;

        if (!_spawnConfigChecked)
        {
            // Đợi spawn config sẵn sàng với limit frame
            if (PlayerSpawnConfig.SpawnPointCount > 0)
            {
                CalculateSpawnPosition();
            }
            else
            {
                _waitFrames++;
                if (_waitFrames > maxWaitFrames)
                {
                    Debug.LogError($"[SpawnSnap] ⏰ Waited {maxWaitFrames} frames but SpawnPointCount=0! Add _SpawnPoints + FusionSpawnPointSetter to scene! Spawn position will be WRONG.");
                    int idx = GetPlayerIndex();
                    _targetSpawnPos = GetFallbackSpawnPosition(idx);
                    _targetSpawnRot = Quaternion.identity;
                    _spawnConfigChecked = true;
                }
                return;
            }
        }

        // Tất cả players đều cần snap spawn (không chỉ local)
        if (!_hasSnapped)
        {
            TrySnapToSpawn();
        }
        else if (!_resnapDisabled)
        {
            // BUG-10 fix: Chỉ resnap trong grace period, sau đó tắt vĩnh viễn
            _resnapGraceTimer -= Runner.DeltaTime;
            if (_resnapGraceTimer <= 0f)
            {
                _resnapDisabled = true;
                Debug.Log($"[SpawnSnap] Resnap grace period ended — movement unlocked.");
            }
            else
            {
                // Resnap nếu player bị lệch khỏi target (chỉ trong grace period)
                Vector3 currentPos = transform.position;
                float dist = Vector3.Distance(currentPos, _targetSpawnPos);
                if (dist > snapDistanceThreshold)
                {
                    _hasSnapped = false;
                    TrySnapToSpawn();
                    Debug.Log($"[SpawnSnap] 🔄 Resnapped player to {_targetSpawnPos} (was {dist:F2}m away)");
                }
            }
        }
    }

    /// <summary>Teleport không điều kiện tới spawn point. Dùng trong Spawned() và ForceSnapToSpawn().</summary>
    private void ApplySnapPosition()
    {
        var cc = GetComponentInChildren<CharacterController>(true);
        bool ccWasEnabled = cc != null && cc.enabled;
        if (cc != null) cc.enabled = false;

        transform.SetPositionAndRotation(_targetSpawnPos, _targetSpawnRot);

        if (TryGetComponent<NetworkPlayerRootFollowBody>(out var rootFollow))
            rootFollow.ForceTeleport(_targetSpawnPos, _targetSpawnRot);

        if (cc != null) cc.enabled = ccWasEnabled;

        // Không dùng Rigidbody (tránh MissingComponentException break code trên Unity mới)

        Debug.Log($"[SpawnSnap] Teleported '{gameObject.name}' to {_targetSpawnPos}");
    }

    private void TrySnapToSpawn()
    {
        // Kiểm tra spawn position hợp lệ
        if (_targetSpawnPos == Vector3.zero && PlayerSpawnConfig.SpawnPointCount == 0)
        {
            // Chưa có spawn config → đợi thêm
            return;
        }

        Vector3 currentPos = transform.position;
        float distance = Vector3.Distance(currentPos, _targetSpawnPos);

        if (distance > snapDistanceThreshold)
        {
            ApplySnapPosition();
        }

        _hasSnapped = true;

        // Không dùng Rigidbody

        // Reset CharacterController velocity (chỉ khi đang bật — tránh warning khi remote / lobby)
        var ccMove = GetComponentInChildren<CharacterController>(true);
        if (ccMove != null && ccMove.enabled)
        {
            ccMove.Move(Vector3.zero);
        }
    }

    // ───────────────────── PUBLIC API ─────────────────────

    /// <summary>
    /// Force snap player tới spawn position (dùng cho respawn).
    /// </summary>
    public void ForceSnapToSpawn()
    {
        if (!_spawnConfigChecked)
            CalculateSpawnPosition();

        _hasSnapped = false;
        TrySnapToSpawn();
    }

    /// <summary>
    /// Teleport player tới vị trí tùy chỉnh.
    /// </summary>
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        _targetSpawnPos = position;
        _targetSpawnRot = rotation;

        // Tạm tắt CC để tránh capsule nội bộ kéo player về chỗ cũ
        var cc = GetComponentInChildren<CharacterController>(true);
        bool ccWasEnabled = cc != null && cc.enabled;
        if (cc != null) cc.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        var rootFollow = GetComponent<NetworkPlayerRootFollowBody>();
        if (rootFollow != null)
            rootFollow.ForceTeleport(position, rotation);

        if (cc != null) cc.enabled = ccWasEnabled;

        _hasSnapped = true;
        Debug.Log($"[SpawnSnap] Teleported to: {position}");
    }

    /// <summary>
    /// Lấy spawn position hiện tại của player này.
    /// </summary>
    public Vector3 CurrentSpawnPos => _targetSpawnPos;
    public Quaternion CurrentSpawnRot => _targetSpawnRot;
}
