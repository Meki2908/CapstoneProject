using UnityEngine;
using Unity.Netcode;

public class TitleScreenManager : MonoBehaviour
{
    public void StartNetworkAsHost()
    {
        ParrelSyncTransportPort.ApplyClonePortOffsetIfNeeded(NetworkManager.Singleton);
        NetworkManager.Singleton.StartHost();
    }
}
