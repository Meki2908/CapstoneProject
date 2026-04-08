using UnityEngine;

/// <summary>
/// Giữ singleton cho scene — flow NGO đã gỡ; dungeon portal dùng <see cref="DungeonPortalLobbyCoordinator"/> local countdown.
/// </summary>
public class DungeonPartyLobbySession : MonoBehaviour
{
    public static DungeonPartyLobbySession Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static bool TryEnsureSessionForHost(out DungeonPartyLobbySession session)
    {
        return TryGetSessionForNetwork(out session);
    }

    public static bool TryGetSessionForNetwork(out DungeonPartyLobbySession session)
    {
        session = Instance != null ? Instance : Object.FindFirstObjectByType<DungeonPartyLobbySession>();
        return session != null;
    }
}
