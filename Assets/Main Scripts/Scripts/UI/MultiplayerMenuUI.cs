using UnityEngine;
using TMPro;
using Artsystack.ArtsystackGui;
using System.Collections;

public class MultiplayerMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Input field cho tên phòng")]
    [SerializeField] private TMP_InputField roomNameInput;
    [Tooltip("Input field cho mật khẩu phòng (nếu có)")]
    [SerializeField] private TMP_InputField passwordInput;
    [Tooltip("Text dùng để hiển thị lỗi (Sai mật khẩu, Không tìm thấy phòng, v.v.)")]
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Save Data")]
    [Tooltip("Kéo các nút Continue (của Singleplayer và Multiplayer) vào đây để ẩn đi nếu chưa từng chơi")]
    [SerializeField] private GameObject[] continueButtons;

    private GameMenuManager _gameMenuManager;

    private void Start()
    {
        _gameMenuManager = FindAnyObjectByType<GameMenuManager>();
        
        if (errorText != null)
            errorText.text = "";

        // Ẩn nút Continue nếu chưa có file save
        if (_gameMenuManager != null && continueButtons != null)
        {
            bool hasSave = _gameMenuManager.HasSaveData();
            foreach (var btn in continueButtons)
            {
                if (btn != null)
                    btn.SetActive(hasSave);
            }
        }

        // Lắng nghe sự kiện lỗi kết nối từ FusionConnectionManager
        if (FusionConnectionManager.Instance != null)
        {
            FusionConnectionManager.Instance.OnConnectionError += HandleConnectionError;
        }
    }

    private void OnDestroy()
    {
        if (FusionConnectionManager.Instance != null)
        {
            FusionConnectionManager.Instance.OnConnectionError -= HandleConnectionError;
        }
    }

    /// <summary>
    /// Được gọi khi người chơi bấm nút "Tạo Phòng" (Create Host)
    /// </summary>
    public void OnCreateRoomClicked()
    {
        if (string.IsNullOrWhiteSpace(roomNameInput.text))
        {
            ShowError("Tên phòng không được để trống!");
            return;
        }

        ShowLoadingScreen();
        
        string roomName = roomNameInput.text.Trim();
        string pass = passwordInput != null ? passwordInput.text.Trim() : "";
        
        FusionConnectionManager.Instance.StartHostWithPass(roomName, pass);
    }

    /// <summary>
    /// Được gọi khi người chơi bấm nút "Vào Phòng" (Join Host)
    /// </summary>
    public void OnJoinRoomClicked()
    {
        if (string.IsNullOrWhiteSpace(roomNameInput.text))
        {
            ShowError("Tên phòng không được để trống!");
            return;
        }

        ShowLoadingScreen();

        string roomName = roomNameInput.text.Trim();
        string pass = passwordInput != null ? passwordInput.text.Trim() : "";

        FusionConnectionManager.Instance.JoinRoom(roomName, pass);
    }

    /// <summary>
    /// Được gọi khi người chơi bấm "Single Player" (chơi đơn)
    /// </summary>
    public void OnSinglePlayerClicked()
    {
        ShowLoadingScreen();
        FusionConnectionManager.Instance.StartSinglePlayer();
    }

    private void ShowLoadingScreen()
    {
        if (errorText != null)
            errorText.text = "";

        if (_gameMenuManager != null && _gameMenuManager.Panel_Loading != null)
        {
            // Bật Loading Screen hiện có trong Canvas Menu
            _gameMenuManager.Panel_Loading.SetActive(true);
        }
    }

    private void HideLoadingScreen()
    {
        if (_gameMenuManager != null && _gameMenuManager.Panel_Loading != null)
        {
            _gameMenuManager.Panel_Loading.SetActive(false);
        }
    }

    private void HandleConnectionError(string errorMessage)
    {
        HideLoadingScreen();
        ShowError(errorMessage);
    }

    private void ShowError(string msg)
    {
        if (errorText != null)
        {
            errorText.text = msg;
            StopAllCoroutines();
            StartCoroutine(ClearErrorTextAfterDelay(5f));
        }
        else
        {
            Debug.LogWarning($"[MultiplayerMenuUI] Lỗi: {msg} (Chưa gắn Error Text vào UI)");
        }
    }

    private IEnumerator ClearErrorTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (errorText != null)
            errorText.text = "";
    }
}
