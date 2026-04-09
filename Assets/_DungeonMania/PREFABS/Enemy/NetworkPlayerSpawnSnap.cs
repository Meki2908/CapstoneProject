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

    void Awake()
    {
        // Đảm bảo tất cả component được thêm (nếu chưa có)
        if (!TryGetComponent<NetworkPlayerRootFollowBody>(out _))
            gameObject.AddComponent<NetworkPlayerRootFollowBody>();
        if (!TryGetComponent<NetworkPlayerLocalOwnership>(out _))
            gameObject.AddComponent<NetworkPlayerLocalOwnership>();
        if (!TryGetComponent<NetworkAnimatorSync>(out _))
            gameObject.AddComponent<NetworkAnimatorSync>();
        if (!TryGetComponent<NetworkPlayerStats>(out _))
            gameObject.AddComponent<NetworkPlayerStats>();
        if (!TryGetComponent<NetworkPlayerName>(out _))
            gameObject.AddComponent<NetworkPlayerName>();
        if (!TryGetComponent<NetworkPlayerDeathManager>(out _))
            gameObject.AddComponent<NetworkPlayerDeathManager>();
    }

    public override void Spawned()
    {
        _hasSnapped = false;
        _resnapTimer = 0f;
        _spawnConfigChecked = false;
        _waitFrames = 0;

        // Tính spawn position dựa trên PlayerId
        CalculateSpawnPosition();
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
        if (Runner == null) return 0;

        // Dùng PlayerId để đánh index ổn định
        if (Object != null && Object.IsValid)
        {
            // Ưu tiên: duyệt ActivePlayers để tìm thứ tự
            int idx = 0;
            foreach (var p in Runner.ActivePlayers)
            {
                if (p == Object.InputAuthority)
                    return idx;
                idx++;
            }

            // Fallback: dùng PlayerId trừ 1 để index từ 0
            return Object.InputAuthority.PlayerId - 1;
        }

        return 0;
    }

    private Vector3 GetFallbackSpawnPosition(int playerIndex)
    {
        // Xếp vòng tròn quanh origin
        float angle = playerIndex * (360f / 4f);
        float radius = 3f;
        float rad = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad) * radius, 1f, Mathf.Sin(rad) * radius);
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
                    Debug.LogError($"[SpawnSnap] ⏰ Waited {maxWaitFrames} frames but spawn config still empty! Using fallback.");
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
        else
        {
            // Resnap nếu player bị lệch khỏi target
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
            // Chưa đúng vị trí → teleport
            transform.SetPositionAndRotation(_targetSpawnPos, _targetSpawnRot);
            Debug.Log($"[SpawnSnap] Snapped player '{gameObject.name}' to spawn: {_targetSpawnPos} (was at {currentPos}, dist={distance:F2})");
        }

        _hasSnapped = true;

        // Reset velocity nếu có
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Reset CharacterController velocity
        var cc = GetComponentInChildren<CharacterController>();
        if (cc != null)
        {
            cc.Move(Vector3.zero);
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
        transform.SetPositionAndRotation(position, rotation);

        // Reset interpolation state
        var rootFollow = GetComponent<NetworkPlayerRootFollowBody>();
        if (rootFollow != null)
            rootFollow.ForceTeleport(position, rotation);

        Debug.Log($"[SpawnSnap] Teleported to: {position}");
    }

    /// <summary>
    /// Lấy spawn position hiện tại của player này.
    /// </summary>
    public Vector3 CurrentSpawnPos => _targetSpawnPos;
    public Quaternion CurrentSpawnRot => _targetSpawnRot;
}
