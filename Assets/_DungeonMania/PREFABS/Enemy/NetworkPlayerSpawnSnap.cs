using UnityEngine;

/// <summary>
/// Gắn root prefab player — snap spawn từ <see cref="PlayerSpawnConfig"/>.
/// </summary>
[DefaultExecutionOrder(-200)]
public class NetworkPlayerSpawnSnap : MonoBehaviour
{
    void Awake()
    {
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
    }

    void Start()
    {
        var pt = PlayerSpawnConfig.SpawnPoint;
        if (pt != null)
            transform.SetPositionAndRotation(pt.position, pt.rotation);
    }
}
