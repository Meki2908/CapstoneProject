using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class HostSessionUI : MonoBehaviour
{
    [Header("=== INPUTS ===")]
    [SerializeField] private TMP_InputField playerDisplayNameInput;
    [SerializeField] private TMP_InputField sessionNameInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Tooltip("When on, the room is private (hidden from public listing).")]
    [SerializeField] private Toggle isPrivateToggle;

    [Header("=== OPTIONS ===")]
    [Tooltip("Max players for this session.")]
    [SerializeField] private Slider maxPlayersSlider;
    [SerializeField] private TextMeshProUGUI maxPlayersText;

    [Tooltip("Optional — unused when the lobby uses a fixed scene from FusionConnectionManager.")]
    [SerializeField] private TMP_Dropdown mapDropdown;

    [Header("=== BUTTONS ===")]
    [SerializeField] private Button createSessionButton;
    [SerializeField] private Button backButton;

    public const string FixedGameplaySceneName = "Map_Chinh";

    public event Action<SessionData> OnCreateSessionRequested;
    public event Action OnBackRequested;

    public struct SessionData
    {
        public string playerDisplayName;
        public string sessionName;
        public string password;
        public bool isPrivate;
        public int maxPlayers;
        public string selectedScene;
    }

    /// <summary>Called from <see cref="LobbyRuntimePanels"/> after the runtime host UI is built.</summary>
    public void WireRuntimeControls(
        TMP_InputField displayNameField,
        TMP_InputField nameField,
        TMP_InputField passField,
        Toggle privateToggle,
        Slider playersSlider,
        TextMeshProUGUI playersLabel,
        Button createBtn,
        Button backBtn)
    {
        playerDisplayNameInput = displayNameField;
        sessionNameInput = nameField;
        passwordInput = passField;
        isPrivateToggle = privateToggle;
        maxPlayersSlider = playersSlider;
        maxPlayersText = playersLabel;
        createSessionButton = createBtn;
        backButton = backBtn;
    }

    private void Start()
    {
        if (createSessionButton != null)
            createSessionButton.onClick.AddListener(HandleCreateSession);
        if (backButton != null)
            backButton.onClick.AddListener(() => OnBackRequested?.Invoke());

        if (maxPlayersSlider != null)
        {
            maxPlayersSlider.onValueChanged.AddListener(UpdateMaxPlayersText);
            UpdateMaxPlayersText(maxPlayersSlider.value);
        }

        if (playerDisplayNameInput != null)
            playerDisplayNameInput.text = PlayerDisplayNamePrefs.GetSavedOrDefault();

        if (sessionNameInput != null && string.IsNullOrEmpty(sessionNameInput.text))
            sessionNameInput.text = "Room_" + UnityEngine.Random.Range(1000, 9999);
    }

    private void UpdateMaxPlayersText(float value)
    {
        if (maxPlayersText == null) return;
        int v = (int)value;
        int max = maxPlayersSlider != null ? (int)maxPlayersSlider.maxValue : 4;
        maxPlayersText.text = $"{v} / {max}";
    }

    private void HandleCreateSession()
    {
        if (sessionNameInput == null || createSessionButton == null) return;

        string roomName = sessionNameInput.text.Trim();
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("[HostSessionUI] Room name cannot be empty.");
            return;
        }

        string displayName = playerDisplayNameInput != null ? playerDisplayNameInput.text : "";
        string sanitizedDisplay = PlayerDisplayNamePrefs.Sanitize(displayName);
        PlayerDisplayNamePrefs.Save(sanitizedDisplay);

        var data = new SessionData
        {
            playerDisplayName = sanitizedDisplay,
            sessionName = roomName,
            password = passwordInput != null ? passwordInput.text : "",
            isPrivate = isPrivateToggle != null && isPrivateToggle.isOn,
            maxPlayers = maxPlayersSlider != null ? (int)maxPlayersSlider.value : 4,
            selectedScene = mapDropdown != null && mapDropdown.options.Count > 0
                ? mapDropdown.options[mapDropdown.value].text
                : FixedGameplaySceneName
        };

        createSessionButton.interactable = false;
        OnCreateSessionRequested?.Invoke(data);
    }

    public void ResetUI()
    {
        if (createSessionButton != null)
            createSessionButton.interactable = true;
    }

    private void OnDestroy()
    {
        if (createSessionButton != null)
            createSessionButton.onClick.RemoveAllListeners();
        if (backButton != null)
            backButton.onClick.RemoveAllListeners();
        if (maxPlayersSlider != null)
            maxPlayersSlider.onValueChanged.RemoveAllListeners();
    }
}
