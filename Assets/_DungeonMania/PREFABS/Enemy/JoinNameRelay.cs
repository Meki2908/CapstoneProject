using UnityEngine;
using TMPro;

/// <summary>
/// Gắn trên nút JOIN trong Panel_JoinRoom.
/// Trước khi MultiplayerManager.StartClientAndLoadGame() chạy,
/// component này copy tên từ Join panel vào MultiplayerManager
/// và swap cả playerNameInput + statusText sang Join panel.
///
/// FIX: Không dùng runtime AddListener (chạy SAU persistent listeners).
/// MultiplayerManager gọi ApplyJoinPanelOverrides() trực tiếp
/// TRƯỚC khi đọc input, đảm bảo name + statusText luôn đúng panel.
/// </summary>
public class JoinNameRelay : MonoBehaviour
{
    [SerializeField] internal TMP_InputField joinNameInput;
    [SerializeField] internal TextMeshProUGUI joinStatusText;
    [SerializeField] internal MultiplayerManager multiplayerManager;

    /// <summary>
    /// Tìm JoinNameRelay đang active trong scene và apply overrides.
    /// Gọi từ MultiplayerManager.StartClientAndLoadGame() TRƯỚC SavePlayerName().
    /// </summary>
    public static void ApplyJoinPanelOverrides(MultiplayerManager mgr)
    {
        if (mgr == null) return;

        // Tìm tất cả JoinNameRelay (kể cả inactive parent)
        var relays = Object.FindObjectsByType<JoinNameRelay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var relay in relays)
        {
            if (relay == null) continue;
            relay.DoApply(mgr);
        }
    }

    void DoApply(MultiplayerManager mgr)
    {
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        // Swap playerNameInput → Join panel's input
        // Điều này đảm bảo SavePlayerName() đọc từ đúng panel
        if (joinNameInput != null)
        {
            string name = joinNameInput.text.Trim();
            if (!string.IsNullOrEmpty(name))
            {
                NetworkPlayerName.LocalPlayerName = name;
                PlayerPrefs.SetString("PlayerName", name);
                PlayerPrefs.Save();
                Debug.Log($"[JoinNameRelay] Set player name from Join panel: {name}");
            }

            // Swap field để SavePlayerName() cũng đọc từ Join panel
            var nameField = typeof(MultiplayerManager).GetField("playerNameInput", flags);
            if (nameField != null)
            {
                nameField.SetValue(mgr, joinNameInput);
                Debug.Log("[JoinNameRelay] Swapped playerNameInput to Join panel.");
            }
        }

        // Swap statusText → Join panel's status text
        if (joinStatusText != null)
        {
            var statusField = typeof(MultiplayerManager).GetField("statusText", flags);
            if (statusField != null)
            {
                statusField.SetValue(mgr, joinStatusText);
                Debug.Log("[JoinNameRelay] Swapped statusText to Join panel.");
            }
        }
    }
}
