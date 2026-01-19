using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BigMapController : MonoBehaviour
{
    [Header("UI")]
    public GameObject bigMapPanel;        // Panel chứa map (ẩn/hiện)
    public RectTransform mapRect;         // RectTransform của MapImage (hoặc MapViewport)
    public RectTransform playerMarkerRect; // Marker người chơi (con của mapRect)

    [Header("Player")]
    public Transform player;

    [Header("World Bounds (from capture camera)")]
    // Bạn chụp cam ở (0,800,0), ortho size 400, ảnh vuông => bounds mặc định như này
    public float minX = -400, maxX = 400;
    public float minZ = -400, maxZ = 400;

    [Header("Fix if flipped")]
    public bool invertX = false;
    public bool invertZ = false;

    [Header("Toggle Key")]
    public KeyCode toggleKey = KeyCode.M;

    [Header("Portals (Teleport Markers)")]
    public Transform[] portalPoints;           // Các điểm dịch chuyển trong world
    public MapPortalMarker portalMarkerPrefab; // Prefab UI marker (nên là Button + MapPortalMarker)
    public RectTransform portalsParent;        // Parent chứa portal markers (thường = mapRect)
    public bool closeMapAfterTeleport = true;

    [Header("Teleport Support (optional)")]
    public CharacterController characterController;
    public Rigidbody playerRigidbody;
    public UnityEngine.AI.NavMeshAgent navAgent;
    public Vector3 teleportOffset = Vector3.up * 0.1f;

    [Header("Debug")]
    public bool debugLogs = false;

    private MapPortalMarker[] _portalMarkers;

    void Awake()
    {
        if (bigMapPanel != null)
            bigMapPanel.SetActive(false);

        if (portalsParent == null)
            portalsParent = mapRect;

        // Auto grab components if not assigned
        if (player != null)
        {
            if (characterController == null) characterController = player.GetComponent<CharacterController>();
            if (playerRigidbody == null) playerRigidbody = player.GetComponent<Rigidbody>();
            if (navAgent == null) navAgent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
        }
    }

    void Start()
    {
        SpawnPortalMarkers();
    }

    void Update()
    {
        if (IsTogglePressedThisFrame())
        {
            if (bigMapPanel == null)
            {
                Debug.LogError("[BigMap] bigMapPanel is NULL (kéo BigMapPanel vào Inspector).");
                return;
            }

            bool next = !bigMapPanel.activeSelf;
            bigMapPanel.SetActive(next);

            if (debugLogs) Debug.Log("[BigMap] bigMapPanel active = " + next);
        }

        if (bigMapPanel != null && bigMapPanel.activeSelf)
        {
            UpdatePlayerMarker();
            UpdatePortalMarkers();
        }
    }

    bool IsTogglePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        // New Input System: hỗ trợ phím M
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            return true;
#endif
        // Old Input
        return Input.GetKeyDown(toggleKey);
    }

    // =========================
    // World -> Map UI conversion
    // =========================
    Vector2 WorldToMapUI(Vector3 worldPos)
    {
        if (mapRect == null) return Vector2.zero;

        float nx = Mathf.InverseLerp(minX, maxX, worldPos.x);
        float nz = Mathf.InverseLerp(minZ, maxZ, worldPos.z);

        if (invertX) nx = 1f - nx;
        if (invertZ) nz = 1f - nz;

        nx = Mathf.Clamp01(nx);
        nz = Mathf.Clamp01(nz);

        float uiX = (nx - 0.5f) * mapRect.rect.width;
        float uiY = (nz - 0.5f) * mapRect.rect.height;

        return new Vector2(uiX, uiY);
    }

    // =========================
    // Player marker
    // =========================
    void UpdatePlayerMarker()
    {
        if (player == null || playerMarkerRect == null || mapRect == null) return;

        Vector2 ui = WorldToMapUI(player.position);
        playerMarkerRect.anchoredPosition = ui;

        if (debugLogs)
            Debug.Log($"[BigMap] Player world=({player.position.x:F1},{player.position.z:F1}) ui={ui}");
    }

    // =========================
    // Portal markers
    // =========================
    void SpawnPortalMarkers()
    {
        if (portalMarkerPrefab == null)
        {
            if (debugLogs) Debug.LogWarning("[BigMap] portalMarkerPrefab is NULL (chưa gán prefab marker).");
            return;
        }

        if (portalPoints == null || portalPoints.Length == 0)
        {
            if (debugLogs) Debug.LogWarning("[BigMap] portalPoints is empty.");
            return;
        }

        if (portalsParent == null) portalsParent = mapRect;
        if (portalsParent == null)
        {
            Debug.LogError("[BigMap] portalsParent/mapRect is NULL.");
            return;
        }

        // clear old
        if (_portalMarkers != null)
        {
            for (int i = 0; i < _portalMarkers.Length; i++)
                if (_portalMarkers[i] != null)
                    Destroy(_portalMarkers[i].gameObject);
        }

        _portalMarkers = new MapPortalMarker[portalPoints.Length];

        for (int i = 0; i < portalPoints.Length; i++)
        {
            var t = portalPoints[i];
            if (t == null) continue;

            var marker = Instantiate(portalMarkerPrefab, portalsParent);
            marker.name = $"PortalMarker_{i}";

            // IMPORTANT: Init(map, target) chỉ 2 args
            marker.Init(this, t);

            _portalMarkers[i] = marker;
        }
    }

    void UpdatePortalMarkers()
    {
        if (_portalMarkers == null || _portalMarkers.Length == 0) return;

        for (int i = 0; i < _portalMarkers.Length; i++)
        {
            var m = _portalMarkers[i];
            if (m == null || m.Target == null) continue;

            Vector2 ui = WorldToMapUI(m.Target.position);
            m.Rect.anchoredPosition = ui;
        }
    }

    // =========================
    // Teleport
    // =========================
    public void TeleportTo(Vector3 targetPos)
    {
        if (player == null) return;

        Vector3 finalPos = targetPos + teleportOffset;

        // Nếu dùng NavMeshAgent: dùng Warp
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.Warp(finalPos);
        }
        else
        {
            // Nếu dùng CharacterController: tắt/bật để khỏi bị đẩy ngược
            if (characterController != null) characterController.enabled = false;

            // Nếu dùng Rigidbody: reset velocity
            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }

            player.position = finalPos;

            if (characterController != null) characterController.enabled = true;
        }

        if (closeMapAfterTeleport && bigMapPanel != null)
            bigMapPanel.SetActive(false);

        if (debugLogs) Debug.Log("[BigMap] Teleported to: " + finalPos);
    }
}
