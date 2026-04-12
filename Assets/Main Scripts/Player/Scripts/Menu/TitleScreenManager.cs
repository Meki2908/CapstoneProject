using UnityEngine;
// using Unity.Netcode; // Commented out to fix missing namespace error

public class TitleScreenManager : MonoBehaviour
{
    public void StartNetworkAsHost()
    {
        Debug.LogWarning("StartNetworkAsHost called, but Unity.Netcode is not installed. Function disabled.");
        // NetworkManager.Singleton.StartHost();
    }
}
