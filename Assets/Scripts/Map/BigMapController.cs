using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BigMapController : MonoBehaviour
{
    [Header("UI")]
    public GameObject bigMapPanel;            // Panel chứa Big Map
    public RectTransform mapRect;             // RectTransform của map
    public RectTransform playerMarkerRect;    // Marker người chơi (con của mapRect)

    [Header("UI Hide When BigMap Open")]
    public GameObject[] hideWhenBigMapOpen;   // Các UI cần ẩn khi mở Big Map

    [Header("Player")]
    public Transform player;

    [Header("World Bounds (from capture camera)")]
    public float minX = -400, maxX = 400;
    public float minZ = -400, maxZ = 400;

    [Header("Fix if flipped")]
    public bool invertX = false;
    public bool invertZ = false;

    [Header("Toggle Key")]
    public KeyCode toggleKey = KeyCode.M;

    [Header("Portals (Teleport Markers)")]
    public Transform[] portalPoints;
    public MapPortalMarker portalMarkerPrefab;
    public RectTransform portalsParent;
    public bool closeMapAfterTeleport = true;

    [Header("Teleport Support (optional)")]
    public CharacterController characterController;
    public Rigidbody playerRigidbody;
    public UnityEngine.AI.NavMeshAgent navAgent;
    public Vector3 teleportOffset = Vector3.up * 0.1f;

    [Header("Debug")]
    public bool debugLogs = false;

    private MapPortalMarker[] _portalMarkers;

    // =========================
    // Unity
    // =========================
    void Awake()
    {
        if (bigMapPanel != null)
            bigMapPanel.SetActive(false);

        if (portalsParent == null)
            portalsParent = mapRect;

        if (player != null)
        {
            if (characterController == null)
                characterController = player.GetComponent<CharacterController>();

            if (playerRigidbody == null)
                playerRigidbody = player.GetComponent<Rigidbody>();

            if (navAgent == null)
                navAgent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
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
            ToggleBigMap();
        }

        if (bigMapPanel != null && bigMapPanel.activeSelf)
        {
            UpdatePlayerMarker();
            UpdatePortalMarkers();
        }
    }

    // =========================
    // Toggle BigMap
    // =========================
    void ToggleBigMap()
    {
        if (bigMapPanel == null)
        {
            Debug.LogError("[BigMap] bigMapPanel is NULL.");
            return;
        }

        bool next = !bigMapPanel.activeSelf;
        bigMapPanel.SetActive(next);

        // Ẩn / hiện UI khác
        SetOtherUIActive(!next);

        if (debugLogs)
            Debug.Log("[BigMap] BigMap active = " + next);
    }

    void SetOtherUIActive(bool active)
    {
        if (hideWhenBigMapOpen == null) return;

        foreach (var ui in hideWhenBigMapOpen)
        {
            if (ui != null)
                ui.SetActive(active);
        }
    }

    bool IsTogglePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            return true;
#endif
        return Input.GetKeyDown(toggleKey);
    }

    // =========================
    // World -> Map UI
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
    // Player Marker
    // =========================
    void UpdatePlayerMarker()
    {
        if (player == null || playerMarkerRect == null || mapRect == null)
            return;

        Vector2 ui = WorldToMapUI(player.position);
        playerMarkerRect.anchoredPosition = ui;

        if (debugLogs)
            Debug.Log($"[BigMap] Player UI Pos = {ui}");
    }

    // =========================
    // Portal Markers
    // =========================
    void SpawnPortalMarkers()
    {
        if (portalMarkerPrefab == null || portalPoints == null || portalPoints.Length == 0)
            return;

        if (portalsParent == null)
            portalsParent = mapRect;

        // Clear old
        if (_portalMarkers != null)
        {
            foreach (var m in _portalMarkers)
                if (m != null)
                    Destroy(m.gameObject);
        }

        _portalMarkers = new MapPortalMarker[portalPoints.Length];

        for (int i = 0; i < portalPoints.Length; i++)
        {
            if (portalPoints[i] == null) continue;

            var marker = Instantiate(portalMarkerPrefab, portalsParent);
            marker.name = $"PortalMarker_{i}";
            marker.Init(this, portalPoints[i]);

            _portalMarkers[i] = marker;
        }
    }

    void UpdatePortalMarkers()
    {
        if (_portalMarkers == null) return;

        foreach (var m in _portalMarkers)
        {
            if (m == null || m.Target == null) continue;

            m.Rect.anchoredPosition = WorldToMapUI(m.Target.position);
        }
    }

    // =========================
    // Teleport
    // =========================
    public void TeleportTo(Vector3 targetPos)
    {
        if (player == null) return;

        Vector3 finalPos = targetPos + teleportOffset;

        if (navAgent != null && navAgent.enabled)
        {
            navAgent.Warp(finalPos);
        }
        else
        {
            if (characterController != null)
                characterController.enabled = false;

            if (playerRigidbody != null)
            {
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }

            player.position = finalPos;

            if (characterController != null)
                characterController.enabled = true;
        }

        if (closeMapAfterTeleport && bigMapPanel != null)
        {
            bigMapPanel.SetActive(false);
            SetOtherUIActive(true);
        }

        if (debugLogs)
            Debug.Log("[BigMap] Teleported to " + finalPos);
    }
}
