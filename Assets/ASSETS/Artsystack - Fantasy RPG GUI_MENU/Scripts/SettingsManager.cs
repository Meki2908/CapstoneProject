using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace Artsystack.ArtsystackGui
{
    /// <summary>
    /// Quản lý cài đặt game - Audio, Graphics, Controls, Gameplay
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        [Header("Panel Settings")]
        [SerializeField] private GameObject panel_GUISettings;
        [SerializeField] private GameObject panel_GUIGame; // Panel menu chính

        [Header("Right Side - Info Panel")]
        [SerializeField] private GameObject panel_InfoRight;
        [SerializeField] private TextMeshProUGUI text_InfoTitle;
        [SerializeField] private TextMeshProUGUI text_InfoDescription;
        
        [Header("Sub-Panels (Settings Tabs)")]
        [SerializeField] private GameObject panel_Gameplay;
        [SerializeField] private GameObject panel_Controller;
        [SerializeField] private GameObject panel_Graphics;
        [SerializeField] private GameObject panel_Audio;

        [Header("Panel Switch Buttons")]
        [SerializeField] private Button button_TabGameplay;
        [SerializeField] private Button button_TabController;
        [SerializeField] private Button button_TabGraphics;
        [SerializeField] private Button button_TabAudio;

        [Header("Bottom Buttons - Shared (TextMeshPro)")]
        [SerializeField] private TextMeshProUGUI text_Restore;
        [SerializeField] private TextMeshProUGUI text_Confirm;
        [SerializeField] private TextMeshProUGUI text_Back;

        [Header("Audio Settings")]
        [SerializeField] private Slider tab_MasterVolume;
        [SerializeField] private Slider tab_MusicVolume;
        [SerializeField] private Slider tab_SFXVolume;
        [SerializeField] private Button btn_VoiceLanguage_Prev;
        [SerializeField] private Button btn_VoiceLanguage_Next;
        [SerializeField] private TextMeshProUGUI text_VoiceLanguage;
        [SerializeField] private Button btn_BackgroundSound_Prev;
        [SerializeField] private Button btn_BackgroundSound_Next;
        [SerializeField] private TextMeshProUGUI text_BackgroundSound;
        
        [Header("Graphics Settings")]
        [SerializeField] private Slider tab_Brightness;
        [SerializeField] private Button btn_Saturation_Prev;
        [SerializeField] private Button btn_Saturation_Next;
        [SerializeField] private TextMeshProUGUI text_Saturation;
        [SerializeField] private Slider tab_Contrast;

        [SerializeField] private Button btn_ScreenResolution_Prev;
        [SerializeField] private Button btn_ScreenResolution_Next;
        [SerializeField] private TextMeshProUGUI text_ScreenResolution;

        [SerializeField] private Button btn_FrameRate_Prev;
        [SerializeField] private Button btn_FrameRate_Next;
        [SerializeField] private TextMeshProUGUI text_FrameRate;

        [SerializeField] private Button btn_DisplayMode_Prev;
        [SerializeField] private Button btn_DisplayMode_Next;
        [SerializeField] private TextMeshProUGUI text_DisplayMode;


        [SerializeField] private Button btn_ChromaticAberration_Prev;
        [SerializeField] private Button btn_ChromaticAberration_Next;
        [SerializeField] private TextMeshProUGUI text_ChromaticAberration;

        [SerializeField] private Button btn_Sharpening_Prev;
        [SerializeField] private Button btn_Sharpening_Next;
        [SerializeField] private TextMeshProUGUI text_Sharpening;

        [Header("Controls Settings - Key Bindings")]
        [SerializeField] private Button btn_KeyBind_Dodge;
        [SerializeField] private TMP_InputField input_KeyBind_Dodge;
        [SerializeField] private Button btn_KeyBind_Sprint;
        [SerializeField] private TMP_InputField input_KeyBind_Sprint;
        [SerializeField] private Button btn_KeyBind_SneakCrouch;
        [SerializeField] private TMP_InputField input_KeyBind_SneakCrouch;
        [SerializeField] private Button btn_KeyBind_Jump;
        [SerializeField] private TMP_InputField input_KeyBind_Jump;
        [SerializeField] private Button btn_KeyBind_MoveForward;
        [SerializeField] private TMP_InputField input_KeyBind_MoveForward;
        [SerializeField] private Button btn_KeyBind_MoveBackward;
        [SerializeField] private TMP_InputField input_KeyBind_MoveBackward;
        [SerializeField] private Button btn_KeyBind_MoveRight;
        [SerializeField] private TMP_InputField input_KeyBind_MoveRight;
        [SerializeField] private Button btn_KeyBind_MoveLeft;
        [SerializeField] private TMP_InputField input_KeyBind_MoveLeft;
        [SerializeField] private Button btn_KeyBind_Heal;
        [SerializeField] private TMP_InputField input_KeyBind_Heal;
        [SerializeField] private Button btn_KeyBind_Menu;
        [SerializeField] private TMP_InputField input_KeyBind_Menu;
        [SerializeField] private Button btn_KeyBind_Attack;
        [SerializeField] private TMP_InputField input_KeyBind_Attack;
        [SerializeField] private Button btn_KeyBind_Interact;
        [SerializeField] private TMP_InputField input_KeyBind_Interact;
        [SerializeField] private Button btn_KeyBind_WeaponWheel;
        [SerializeField] private TMP_InputField input_KeyBind_WeaponWheel;

        [Header("Gameplay Settings")]
        // Camera Settings
        [SerializeField] private Slider tab_CameraMouseSpeed;
        [SerializeField] private Slider tab_CameraRotateSpeed;
        [SerializeField] private Slider tab_CameraZoomSpeedGameplay;
        
        
        // UI Settings
        [SerializeField] private Button btn_MiniMap_Prev;
        [SerializeField] private Button btn_MiniMap_Next;
        [SerializeField] private TextMeshProUGUI text_MiniMap;

        // PlayerPrefs Keys
        private const string KEY_MASTER_VOLUME = "Settings_MasterVolume";
        private const string KEY_MUSIC_VOLUME = "Settings_MusicVolume";
        private const string KEY_SFX_VOLUME = "Settings_SFXVolume";
        private const string KEY_VOICE_LANGUAGE = "Settings_VoiceLanguage";
        private const string KEY_BACKGROUND_SOUND = "Settings_BackgroundSound";
        
        private const string KEY_BRIGHTNESS = "Settings_Brightness";
        private const string KEY_SATURATION = "Settings_Saturation";
        private const string KEY_CONTRAST = "Settings_Contrast";

        // New Graphics Settings Keys
        private const string KEY_SCREEN_RESOLUTION = "Settings_ScreenResolution";
        private const string KEY_DISPLAY_MODE = "Settings_DisplayMode";
        private const string KEY_FRAME_RATE = "Settings_FrameRate";

        private const string KEY_CHROMATIC_ABERRATION = "Settings_ChromaticAberration";
        private const string KEY_SHARPENING = "Settings_Sharpening";




        private const string KEY_MINI_MAP = "Settings_MiniMap";
        
        private const string KEY_CAMERA_MOUSE_SPEED = "Settings_CameraMouseSpeed";
        private const string KEY_CAMERA_ROTATE_SPEED = "Settings_CameraRotateSpeed";
        private const string KEY_CAMERA_ZOOM_SPEED_GAMEPLAY = "Settings_CameraZoomSpeedGameplay";

        // Default values
        private float defaultMasterVolume = 0.75f;
        private float defaultMusicVolume = 0.7f;
        private float defaultSfxVolume = 0.8f;
        
        private float defaultBrightness = 1.0f;
        private bool defaultSaturation = true;
        private int defaultContrast = 50;
        private bool defaultMiniMap = true;

        // New Graphics Defaults
        private int defaultScreenResolution = 0;
        private int defaultDisplayMode = 0; // 0 = Fullscreen, 1 = Windowed, 2 = Borderless
        private int defaultFrameRate = 60;

        private bool defaultChromaticAberration = false;
        private bool defaultSharpening = false;

        // Camera default values
        private float defaultCameraMouseSpeed = 0.8f;
        private float defaultCameraRotateSpeed = 0.7f;
        private float defaultCameraZoomSpeedGameplay = 0.6f;

        // Key Binding system
        private const string KEY_BIND_PREFIX = "KeyBind_";
        private Dictionary<string, KeyCode> keyBindings = new Dictionary<string, KeyCode>();
        private Dictionary<string, KeyCode> defaultKeyBindings = new Dictionary<string, KeyCode>()
        {
            { "Dodge", KeyCode.LeftShift },
            { "Sprint", KeyCode.Mouse1 },
            { "SneakCrouch", KeyCode.C },
            { "Jump", KeyCode.Space },
            { "MoveForward", KeyCode.W },
            { "MoveBackward", KeyCode.S },
            { "MoveRight", KeyCode.D },
            { "MoveLeft", KeyCode.A },
            { "Heal", KeyCode.Z },
            { "Menu", KeyCode.Escape },
            { "Attack", KeyCode.Mouse0 },
            { "Interact", KeyCode.F },
            { "WeaponWheel", KeyCode.Tab }
        };

        // Rebinding state
        private bool isWaitingForKey = false;
        private string currentRebindAction = "";
        private TMP_InputField currentRebindInput = null;

        // Track if settings have been modified
        private bool hasUnsavedChanges = false;
        
        // Current active tab tracking
        private GameObject currentPanel;

        private void Start()
        {
            // Đảm bảo GameSettings singleton tồn tại (đồng bộ mọi scene)
            GameSettings.EnsureInstance();

            // Initialize hover info panel references
            SettingHoverInfo.SetInfoPanel(panel_InfoRight, text_InfoTitle, text_InfoDescription);
            if (panel_InfoRight != null) panel_InfoRight.SetActive(false);

            // Đảm bảo Settings panel tắt khi bắt đầu
            if (panel_GUISettings != null)
                panel_GUISettings.SetActive(false);

            RestoreDefaultKeyBindings(); // Initialize key bindings with defaults
            LoadSettings();
            SetupEventListeners();
            SetupPanelSwitchButtons();
            SetupBottomButtons();
            PopulateScreenResolutions();
            SetupGraphicsButtons();
            SetupGameplayButtons();
            SetupAudioButtons();
            SetupKeyBindButtons();

            // Tự động gắn hover info cho tất cả setting items có child "Bg"
            SettingHoverInfo.AutoSetupAllPanels(panel_Gameplay, panel_Controller, panel_Graphics, panel_Audio);

            // Initially disable Restore and Confirm buttons
            UpdateBottomButtonsState();
        }

        private void Update()
        {
            // Key rebinding detection
            if (isWaitingForKey)
            {
                // FIX: Escape hủy rebind thay vì gán
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    CancelRebind();
                    return;
                }

                // Check for mouse buttons first
                for (int i = 0; i < 3; i++)
                {
                    if (Input.GetMouseButtonDown(i))
                    {
                        KeyCode mouseKey = (KeyCode)((int)KeyCode.Mouse0 + i);
                        AssignKeyBinding(currentRebindAction, mouseKey);
                        return;
                    }
                }

                // Check for any key press
                if (Input.anyKeyDown)
                {
                    foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
                    {
                        if (Input.GetKeyDown(key) && key != KeyCode.Mouse0 && key != KeyCode.Mouse1 && key != KeyCode.Mouse2)
                        {
                            AssignKeyBinding(currentRebindAction, key);
                            return;
                        }
                    }
                }
                return; // Don't process shortcuts while rebinding
            }

            // Bottom button keyboard shortcuts (only when settings panel is active)
            if (panel_GUISettings != null && panel_GUISettings.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Y))
                {
                    OnRestoreClicked();
                }
                else if (Input.GetKeyDown(KeyCode.X) && hasUnsavedChanges)
                {
                    OnConfirmClicked();
                }
                else if (Input.GetKeyDown(KeyCode.B))
                {
                    OnBackClicked();
                }
            }
        }

        private void AssignKeyBinding(string action, KeyCode key)
        {
            keyBindings[action] = key;
            isWaitingForKey = false;
            currentRebindAction = "";
            currentRebindInput = null;
            UpdateKeyBindTexts();
            MarkAsChanged();
        }

        /// <summary>
        /// Hủy thao tác rebind (nhấn Escape)
        /// </summary>
        private void CancelRebind()
        {
            isWaitingForKey = false;
            if (currentRebindInput != null && keyBindings.ContainsKey(currentRebindAction))
                currentRebindInput.text = GetKeyDisplayName(keyBindings[currentRebindAction]);
            currentRebindAction = "";
            currentRebindInput = null;
            Debug.Log("[Settings] Key rebind cancelled");
        }

        /// <summary>
        /// Sets up key bind button click listeners
        /// </summary>
        private void SetupKeyBindButtons()
        {
            SetupSingleKeyBind(btn_KeyBind_Dodge, input_KeyBind_Dodge, "Dodge");
            SetupSingleKeyBind(btn_KeyBind_Sprint, input_KeyBind_Sprint, "Sprint");
            SetupSingleKeyBind(btn_KeyBind_SneakCrouch, input_KeyBind_SneakCrouch, "SneakCrouch");
            SetupSingleKeyBind(btn_KeyBind_Jump, input_KeyBind_Jump, "Jump");
            SetupSingleKeyBind(btn_KeyBind_MoveForward, input_KeyBind_MoveForward, "MoveForward");
            SetupSingleKeyBind(btn_KeyBind_MoveBackward, input_KeyBind_MoveBackward, "MoveBackward");
            SetupSingleKeyBind(btn_KeyBind_MoveRight, input_KeyBind_MoveRight, "MoveRight");
            SetupSingleKeyBind(btn_KeyBind_MoveLeft, input_KeyBind_MoveLeft, "MoveLeft");
            SetupSingleKeyBind(btn_KeyBind_Heal, input_KeyBind_Heal, "Heal");
            SetupSingleKeyBind(btn_KeyBind_Menu, input_KeyBind_Menu, "Menu");
            SetupSingleKeyBind(btn_KeyBind_Attack, input_KeyBind_Attack, "Attack");
            SetupSingleKeyBind(btn_KeyBind_Interact, input_KeyBind_Interact, "Interact");
            SetupSingleKeyBind(btn_KeyBind_WeaponWheel, input_KeyBind_WeaponWheel, "WeaponWheel");

            UpdateKeyBindTexts();
        }

        private void SetupSingleKeyBind(Button btn, TMP_InputField inputField, string action)
        {
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnKeyBindClicked(action, inputField));
            }
            // Make input field read-only so user can't type directly
            if (inputField != null)
            {
                inputField.readOnly = true;
                inputField.interactable = false;
            }
        }

        private void OnKeyBindClicked(string action, TMP_InputField inputField)
        {
            // If already waiting, cancel previous
            if (isWaitingForKey && currentRebindInput != null)
            {
                currentRebindInput.text = GetKeyDisplayName(keyBindings[currentRebindAction]);
            }

            isWaitingForKey = true;
            currentRebindAction = action;
            currentRebindInput = inputField;

            if (inputField != null)
                inputField.text = "...";
        }

        /// <summary>
        /// Updates all key bind button texts to show current key assignments
        /// </summary>
        private void UpdateKeyBindTexts()
        {
            UpdateSingleKeyBindText(input_KeyBind_Dodge, "Dodge");
            UpdateSingleKeyBindText(input_KeyBind_Sprint, "Sprint");
            UpdateSingleKeyBindText(input_KeyBind_SneakCrouch, "SneakCrouch");
            UpdateSingleKeyBindText(input_KeyBind_Jump, "Jump");
            UpdateSingleKeyBindText(input_KeyBind_MoveForward, "MoveForward");
            UpdateSingleKeyBindText(input_KeyBind_MoveBackward, "MoveBackward");
            UpdateSingleKeyBindText(input_KeyBind_MoveRight, "MoveRight");
            UpdateSingleKeyBindText(input_KeyBind_MoveLeft, "MoveLeft");
            UpdateSingleKeyBindText(input_KeyBind_Heal, "Heal");
            UpdateSingleKeyBindText(input_KeyBind_Menu, "Menu");
            UpdateSingleKeyBindText(input_KeyBind_Attack, "Attack");
            UpdateSingleKeyBindText(input_KeyBind_Interact, "Interact");
            UpdateSingleKeyBindText(input_KeyBind_WeaponWheel, "WeaponWheel");
        }

        private void UpdateSingleKeyBindText(TMP_InputField inputField, string action)
        {
            if (inputField != null && keyBindings.ContainsKey(action))
                inputField.text = GetKeyDisplayName(keyBindings[action]);
        }

        private void RestoreDefaultKeyBindings()
        {
            keyBindings.Clear();
            foreach (var kvp in defaultKeyBindings)
            {
                keyBindings[kvp.Key] = kvp.Value;
            }
        }

        private void SaveKeyBindings()
        {
            foreach (var kvp in keyBindings)
            {
                PlayerPrefs.SetString(KEY_BIND_PREFIX + kvp.Key, kvp.Value.ToString());
            }
        }

        private void LoadKeyBindings()
        {
            foreach (var kvp in defaultKeyBindings)
            {
                string saved = PlayerPrefs.GetString(KEY_BIND_PREFIX + kvp.Key, kvp.Value.ToString());
                if (System.Enum.TryParse<KeyCode>(saved, out KeyCode key))
                {
                    keyBindings[kvp.Key] = key;
                }
                else
                {
                    keyBindings[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Converts KeyCode to a user-friendly display name
        /// </summary>
        private string GetKeyDisplayName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Mouse0: return "LMB";
                case KeyCode.Mouse1: return "RMB";
                case KeyCode.Mouse2: return "MMB";
                case KeyCode.LeftShift: return "SHIFT";
                case KeyCode.RightShift: return "R-SHIFT";
                case KeyCode.LeftControl: return "CTRL";
                case KeyCode.RightControl: return "R-CTRL";
                case KeyCode.LeftAlt: return "ALT";
                case KeyCode.RightAlt: return "R-ALT";
                case KeyCode.Space: return "SPACE";
                case KeyCode.Escape: return "ESC";
                case KeyCode.Return: return "ENTER";
                case KeyCode.Tab: return "TAB";
                case KeyCode.BackQuote: return "`";
                case KeyCode.CapsLock: return "CAPS";
                default: return key.ToString().ToUpper();
            }
        }

        /// <summary>
        /// Sets up audio button listeners
        /// </summary>
        private void SetupAudioButtons()
        {
            // Voice Language - Prev/Next
            if (btn_VoiceLanguage_Prev != null)
                btn_VoiceLanguage_Prev.onClick.AddListener(OnVoiceLanguagePrevClicked);
            if (btn_VoiceLanguage_Next != null)
                btn_VoiceLanguage_Next.onClick.AddListener(OnVoiceLanguageNextClicked);

            // Background Sound - Toggle ON/OFF
            if (btn_BackgroundSound_Prev != null)
                btn_BackgroundSound_Prev.onClick.AddListener(OnBackgroundSoundClicked);
            if (btn_BackgroundSound_Next != null)
                btn_BackgroundSound_Next.onClick.AddListener(OnBackgroundSoundClicked);

            UpdateAudioButtonTexts();
        }

        /// <summary>
        /// Updates audio button texts
        /// </summary>
        private void UpdateAudioButtonTexts()
        {
            if (text_VoiceLanguage != null && voiceLanguageIndex < voiceLanguages.Count)
                text_VoiceLanguage.text = voiceLanguages[voiceLanguageIndex];

            if (text_BackgroundSound != null)
                text_BackgroundSound.text = backgroundSoundEnabled ? "ON" : "OFF";
        }

        /// <summary>
        /// Populates the screen resolution list from system resolutions
        /// </summary>
        private void PopulateScreenResolutions()
        {
            screenResolutions.Clear();

            Resolution[] resolutions = Screen.resolutions;

            for (int i = 0; i < resolutions.Length; i++)
            {
                Resolution res = resolutions[i];
                string resString = $"{res.width} x {res.height}";
                if (!screenResolutions.Contains(resString))
                {
                    screenResolutions.Add(resString);
                }
            }

            // Add common resolutions as fallback if list is empty
            if (screenResolutions.Count == 0)
            {
                screenResolutions.Add("1920 x 1080");
                screenResolutions.Add("1280 x 720");
                screenResolutions.Add("2560 x 1440");
                screenResolutions.Add("3840 x 2160");
            }
        }

        /// <summary>
        /// Sets up graphics button listeners (Left/Right buttons)
        /// </summary>
        private void SetupGraphicsButtons()
        {
            // Screen Resolution - Prev/Next
            if (btn_ScreenResolution_Prev != null)
                btn_ScreenResolution_Prev.onClick.AddListener(OnScreenResolutionPrevClicked);
            if (btn_ScreenResolution_Next != null)
                btn_ScreenResolution_Next.onClick.AddListener(OnScreenResolutionNextClicked);

            // Frame Rate - Prev/Next
            if (btn_FrameRate_Prev != null)
                btn_FrameRate_Prev.onClick.AddListener(OnFrameRatePrevClicked);
            if (btn_FrameRate_Next != null)
                btn_FrameRate_Next.onClick.AddListener(OnFrameRateNextClicked);

            // Display Mode - Prev/Next
            if (btn_DisplayMode_Prev != null)
                btn_DisplayMode_Prev.onClick.AddListener(OnDisplayModePrevClicked);
            if (btn_DisplayMode_Next != null)
                btn_DisplayMode_Next.onClick.AddListener(OnDisplayModeNextClicked);


            // Saturation - Toggle ON/OFF
            if (btn_Saturation_Prev != null)
                btn_Saturation_Prev.onClick.AddListener(OnSaturationClicked);
            if (btn_Saturation_Next != null)
                btn_Saturation_Next.onClick.AddListener(OnSaturationClicked);

            // Chromatic Aberration - Toggle ON/OFF
            if (btn_ChromaticAberration_Prev != null)
                btn_ChromaticAberration_Prev.onClick.AddListener(OnChromaticAberrationClicked);
            if (btn_ChromaticAberration_Next != null)
                btn_ChromaticAberration_Next.onClick.AddListener(OnChromaticAberrationClicked);

            // Sharpening - Toggle ON/OFF
            if (btn_Sharpening_Prev != null)
                btn_Sharpening_Prev.onClick.AddListener(OnSharpeningClicked);
            if (btn_Sharpening_Next != null)
                btn_Sharpening_Next.onClick.AddListener(OnSharpeningClicked);

            // Initialize button texts
            UpdateGraphicsButtonTexts();
        }

        /// <summary>
        /// Sets up gameplay button listeners
        /// </summary>
        private void SetupGameplayButtons()
        {
            // Mini Map - Toggle ON/OFF
            if (btn_MiniMap_Prev != null)
                btn_MiniMap_Prev.onClick.AddListener(OnMiniMapClicked);
            if (btn_MiniMap_Next != null)
                btn_MiniMap_Next.onClick.AddListener(OnMiniMapClicked);

            UpdateGameplayButtonTexts();
        }

        /// <summary>
        /// Updates gameplay button texts
        /// </summary>
        private void UpdateGameplayButtonTexts()
        {
            if (text_MiniMap != null)
                text_MiniMap.text = miniMapEnabled ? "ON" : "OFF";
        }

        /// <summary>
        /// Updates all graphics button texts to show current values
        /// </summary>
        private void UpdateGraphicsButtonTexts()
        {
            // Screen Resolution
            if (text_ScreenResolution != null && screenResolutionIndex < screenResolutions.Count)
            {
                text_ScreenResolution.text = screenResolutions[screenResolutionIndex];
            }

            // Frame Rate
            if (text_FrameRate != null)
            {
                text_FrameRate.text = currentFrameRate + " FPS";
            }

            // Display Mode
            if (text_DisplayMode != null)
            {
                text_DisplayMode.text = displayModes[displayModeIndex];
            }

            // Saturation
            if (text_Saturation != null)
                text_Saturation.text = saturationEnabled ? "ON" : "OFF";

            // Chromatic Aberration
            if (text_ChromaticAberration != null)
                text_ChromaticAberration.text = chromaticAberrationEnabled ? "ON" : "OFF";

            // Sharpening
            if (text_Sharpening != null)
                text_Sharpening.text = sharpeningEnabled ? "ON" : "OFF";
        }

        // ==================== Button Click Handlers ====================

        // Screen Resolution - Prev/Next
        private void OnScreenResolutionPrevClicked()
        {
            screenResolutionIndex--;
            if (screenResolutionIndex < 0) screenResolutionIndex = screenResolutions.Count - 1;
            UpdateGraphicsButtonTexts();
            MarkAsChanged();
        }

        private void OnScreenResolutionNextClicked()
        {
            screenResolutionIndex = (screenResolutionIndex + 1) % screenResolutions.Count;
            UpdateGraphicsButtonTexts();
            MarkAsChanged();
        }

        // Frame Rate - Prev/Next
        private void OnFrameRatePrevClicked()
        {
            frameRateIndex--;
            if (frameRateIndex < 0) frameRateIndex = frameRates.Length - 1;
            currentFrameRate = frameRates[frameRateIndex];
            UpdateGraphicsButtonTexts();
            MarkAsChanged();
        }

        private void OnFrameRateNextClicked()
        {
            frameRateIndex = (frameRateIndex + 1) % frameRates.Length;
            currentFrameRate = frameRates[frameRateIndex];
            UpdateGraphicsButtonTexts();
            MarkAsChanged();
        }

        // Display Mode - Prev/Next
        private void OnDisplayModePrevClicked()
        {
            displayModeIndex--;
            if (displayModeIndex < 0) displayModeIndex = displayModes.Count - 1;
            UpdateGraphicsButtonTexts();
            MarkAsChanged();
        }

        private void OnDisplayModeNextClicked()
        {
            displayModeIndex = (displayModeIndex + 1) % displayModes.Count;
            UpdateGraphicsButtonTexts();
            MarkAsChanged();
        }


        // Chromatic Aberration - Toggle ON/OFF
        private void OnChromaticAberrationClicked()
        {
            chromaticAberrationEnabled = !chromaticAberrationEnabled;
            UpdateGraphicsButtonTexts();
            MarkAsChanged();
        }

        // Sharpening - Toggle ON/OFF
        private void OnSharpeningClicked()
        {
            sharpeningEnabled = !sharpeningEnabled;
            UpdateGraphicsButtonTexts();
            MarkAsChanged();
        }

        // Saturation - Toggle ON/OFF
        private void OnSaturationClicked()
        {
            saturationEnabled = !saturationEnabled;
            UpdateGraphicsButtonTexts();
            MarkAsChanged();
        }

        // Mini Map - Toggle ON/OFF
        private void OnMiniMapClicked()
        {
            miniMapEnabled = !miniMapEnabled;
            UpdateGameplayButtonTexts();
            MarkAsChanged();
        }

        // Voice Language - Prev/Next
        private void OnVoiceLanguagePrevClicked()
        {
            voiceLanguageIndex--;
            if (voiceLanguageIndex < 0) voiceLanguageIndex = voiceLanguages.Count - 1;
            UpdateAudioButtonTexts();
            MarkAsChanged();
        }

        private void OnVoiceLanguageNextClicked()
        {
            voiceLanguageIndex = (voiceLanguageIndex + 1) % voiceLanguages.Count;
            UpdateAudioButtonTexts();
            MarkAsChanged();
        }

        // Background Sound - Toggle ON/OFF
        private void OnBackgroundSoundClicked()
        {
            backgroundSoundEnabled = !backgroundSoundEnabled;
            UpdateAudioButtonTexts();
            MarkAsChanged();
        }

        // Runtime values (not serialized, used during runtime)
        private int screenResolutionIndex = 0;
        private int frameRateIndex = 1; // Default to 60 FPS
        private int displayModeIndex = 0;

        private bool chromaticAberrationEnabled = false;
        private bool saturationEnabled = true;
        private bool miniMapEnabled = true;
        private bool sharpeningEnabled = false;
        private int currentFrameRate = 60;
        private int voiceLanguageIndex = 0;
        private bool backgroundSoundEnabled = true;

        // Available options
        private List<string> screenResolutions = new List<string> { "1920 x 1080", "1280 x 720", "2560 x 1440", "3840 x 2160" };
        private int[] frameRates = { 30, 60, 120, 144, 240 };
        private List<string> displayModes = new List<string> { "Fullscreen", "Windowed", "Borderless" };
        private List<string> voiceLanguages = new List<string> { "English", "Vietnamese", "Japanese", "Korean", "Chinese" };

        private void SetupPanelSwitchButtons()
        {
            if (button_TabGameplay != null)
                button_TabGameplay.onClick.AddListener(() => SwitchToPanel(panel_Gameplay));
            
            if (button_TabController != null)
                button_TabController.onClick.AddListener(() => SwitchToPanel(panel_Controller));
            
            if (button_TabGraphics != null)
                button_TabGraphics.onClick.AddListener(() => SwitchToPanel(panel_Graphics));
            
            if (button_TabAudio != null)
                button_TabAudio.onClick.AddListener(() => SwitchToPanel(panel_Audio));
        }

        private void SetupBottomButtons()
        {
            // Bottom buttons use keyboard shortcuts (Y, X, B) instead of click
            // Set initial dim color
            UpdateBottomButtonsState();
        }

        // Tab visual feedback colors
        private Color tabSelectedColor = new Color(1f, 0.82f, 0.2f, 1f); // Vàng
        private Color tabDefaultColor = Color.white;

        private void SwitchToPanel(GameObject targetPanel)
        {
            // Hide all panels
            if (panel_Gameplay != null) panel_Gameplay.SetActive(false);
            if (panel_Controller != null) panel_Controller.SetActive(false);
            if (panel_Graphics != null) panel_Graphics.SetActive(false);
            if (panel_Audio != null) panel_Audio.SetActive(false);

            // Show target panel
            if (targetPanel != null)
            {
                targetPanel.SetActive(true);
                currentPanel = targetPanel;
            }

            // Update tab visuals
            UpdateTabVisual(button_TabGameplay, targetPanel == panel_Gameplay);
            UpdateTabVisual(button_TabController, targetPanel == panel_Controller);
            UpdateTabVisual(button_TabGraphics, targetPanel == panel_Graphics);
            UpdateTabVisual(button_TabAudio, targetPanel == panel_Audio);
        }

        private void UpdateTabVisual(Button tabButton, bool isSelected)
        {
            if (tabButton == null) return;

            // Change text color
            TextMeshProUGUI tabText = tabButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tabText != null)
                tabText.color = isSelected ? tabSelectedColor : tabDefaultColor;

            // Show/hide Selected_line
            Transform selectedLine = tabButton.transform.Find("Selected_line");
            if (selectedLine != null)
                selectedLine.gameObject.SetActive(isSelected);
        }

        private void SetupEventListeners()
        {
            // Audio sliders
            if (tab_MasterVolume != null)
                tab_MasterVolume.onValueChanged.AddListener(OnSettingChanged);
            if (tab_MusicVolume != null)
                tab_MusicVolume.onValueChanged.AddListener(OnSettingChanged);
            if (tab_SFXVolume != null)
                tab_SFXVolume.onValueChanged.AddListener(OnSettingChanged);


            // Graphics sliders
            if (tab_Brightness != null)
                tab_Brightness.onValueChanged.AddListener(OnSettingChanged);
            

            if (tab_Contrast != null)
                tab_Contrast.onValueChanged.AddListener(OnSettingChanged);

            // New Graphics Controls - handled in SetupGraphicsButtons()
            // Button click handlers are set up in SetupGraphicsButtons() method



            // Gameplay sliders (Camera Settings)
            if (tab_CameraMouseSpeed != null)
                tab_CameraMouseSpeed.onValueChanged.AddListener(OnSettingChanged);
            if (tab_CameraRotateSpeed != null)
                tab_CameraRotateSpeed.onValueChanged.AddListener(OnSettingChanged);
            if (tab_CameraZoomSpeedGameplay != null)
                tab_CameraZoomSpeedGameplay.onValueChanged.AddListener(OnSettingChanged);



        }

        /// <summary>
        /// <summary>
        /// Called when any setting is changed - enables Restore and Confirm buttons
        /// </summary>
        private void OnSettingChanged(float value)
        {
            MarkAsChanged();
        }

        private void OnSettingChanged(bool value)
        {
            MarkAsChanged();
        }

        private void OnSettingChanged(int value)
        {
            MarkAsChanged();
        }

        private void MarkAsChanged()
        {
            hasUnsavedChanges = true;
            UpdateBottomButtonsState();
        }

        /// <summary>
        /// Updates the visual state of Restore and Confirm text based on hasUnsavedChanges
        /// Grey text when no changes, white/bright when settings modified
        /// </summary>
        private Color bottomBtnDimColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Xám mờ
        private Color bottomBtnActiveColor = Color.white; // Sáng

        private void UpdateBottomButtonsState()
        {
            // Restore luôn sáng
            if (text_Restore != null)
                text_Restore.color = bottomBtnActiveColor;
            // Confirm chỉ sáng khi có thay đổi
            if (text_Confirm != null)
                text_Confirm.color = hasUnsavedChanges ? bottomBtnActiveColor : bottomBtnDimColor;
        }

        #region Bottom Button Events

        private void OnRestoreClicked()
        {
            if (currentPanel == panel_Gameplay)
                RestoreGameplayDefaults();
            else if (currentPanel == panel_Controller)
                RestoreControllerDefaults();
            else if (currentPanel == panel_Graphics)
                RestoreGraphicsDefaults();
            else if (currentPanel == panel_Audio)
                RestoreAudioDefaults();
            
            ApplySettings();
            hasUnsavedChanges = false;
            UpdateBottomButtonsState();
        }

        private void OnConfirmClicked()
        {
            ApplySettings();
            hasUnsavedChanges = false;
            UpdateBottomButtonsState();
        }

        // Back button - closes settings
        private void OnBackClicked()
        {
            if (hasUnsavedChanges)
            {
                // Option 1: Auto-save before closing
                ApplySettings();
            }
            
            // Ẩn Settings
            if (panel_GUISettings != null)
                panel_GUISettings.SetActive(false);
            
            // Nếu đang trong gameplay (có PauseMenuManager) → quay về Pause panel
            var pauseManager = PauseMenuManager.Instance;
            if (pauseManager != null && pauseManager.IsPaused)
            {
                pauseManager.CloseSettings();
            }
            else if (panel_GUIGame != null)
            {
                // Quay về lobby menu
                panel_GUIGame.SetActive(true);
            }
            
            hasUnsavedChanges = false;
            UpdateBottomButtonsState();
        }

        #endregion

        #region Restore Defaults

        private void RestoreAudioDefaults()
        {
            if (tab_MasterVolume != null) tab_MasterVolume.value = defaultMasterVolume;
            if (tab_MusicVolume != null) tab_MusicVolume.value = defaultMusicVolume;
            if (tab_SFXVolume != null) tab_SFXVolume.value = defaultSfxVolume;
            voiceLanguageIndex = 0;
            backgroundSoundEnabled = true;
            UpdateAudioButtonTexts();
        }

        private void RestoreGraphicsDefaults()
        {
            if (tab_Brightness != null) tab_Brightness.value = defaultBrightness;
            saturationEnabled = defaultSaturation;
            if (tab_Contrast != null) tab_Contrast.value = defaultContrast;

            // New Graphics Settings - reset to defaults
            screenResolutionIndex = defaultScreenResolution;
            frameRateIndex = 1; // Default to 60 FPS
            displayModeIndex = defaultDisplayMode;

            chromaticAberrationEnabled = defaultChromaticAberration;
            sharpeningEnabled = defaultSharpening;

            UpdateGraphicsButtonTexts();
        }

        private void RestoreControllerDefaults()
        {
            RestoreDefaultKeyBindings();
            UpdateKeyBindTexts();
        }

        private void RestoreGameplayDefaults()
        {
            // Camera Settings
            if (tab_CameraMouseSpeed != null) tab_CameraMouseSpeed.value = defaultCameraMouseSpeed;
            if (tab_CameraRotateSpeed != null) tab_CameraRotateSpeed.value = defaultCameraRotateSpeed;
            if (tab_CameraZoomSpeedGameplay != null) tab_CameraZoomSpeedGameplay.value = defaultCameraZoomSpeedGameplay;
            

            
            // UI Settings
            miniMapEnabled = defaultMiniMap;
        }

        #endregion

        #region Apply Settings

        private void ApplySettings()
        {
            // Đồng bộ tất cả values vào GameSettings singleton
            var gs = GameSettings.Instance;
            if (gs == null)
            {
                Debug.LogWarning("[SettingsManager] GameSettings.Instance is null!");
                return;
            }

            // Audio
            if (tab_MasterVolume != null) gs.masterVolume = tab_MasterVolume.value;
            if (tab_MusicVolume != null) gs.musicVolume = tab_MusicVolume.value;
            if (tab_SFXVolume != null) gs.sfxVolume = tab_SFXVolume.value;
            gs.voiceLanguageIndex = voiceLanguageIndex;
            gs.backgroundSoundEnabled = backgroundSoundEnabled;

            // Graphics
            if (tab_Brightness != null) gs.brightness = tab_Brightness.value;
            gs.saturationEnabled = saturationEnabled;
            if (tab_Contrast != null) gs.contrast = tab_Contrast.value;
            gs.screenResolutionIndex = screenResolutionIndex;
            gs.displayModeIndex = displayModeIndex;
            gs.frameRate = currentFrameRate;
            gs.chromaticAberrationEnabled = chromaticAberrationEnabled;
            gs.sharpeningEnabled = sharpeningEnabled;

            // Gameplay (Camera Settings)
            if (tab_CameraMouseSpeed != null) gs.cameraMouseSpeed = tab_CameraMouseSpeed.value;
            if (tab_CameraRotateSpeed != null) gs.cameraRotateSpeed = tab_CameraRotateSpeed.value;
            if (tab_CameraZoomSpeedGameplay != null) gs.cameraZoomSpeed = tab_CameraZoomSpeedGameplay.value;
            gs.miniMapEnabled = miniMapEnabled;

            // Controls - Key Bindings
            SaveKeyBindings();

            // Apply key bindings vào InputSystem (override InputActions runtime)
            ApplyKeyBindingsToInputSystem();

            // Apply + Save qua GameSettings
            gs.ApplyAndSave();
            Debug.Log("[SettingsManager] Settings applied via GameSettings");
        }

        /// <summary>
        /// Applies graphics settings to the game (Screen, VSync, Post-processing)
        /// </summary>
        private void ApplyGraphicsQualitySettings()
        {
            // Apply Screen Resolution + Display Mode cùng lúc
            if (screenResolutions.Count > 0 && screenResolutionIndex >= 0 && screenResolutionIndex < screenResolutions.Count)
            {
                string selectedRes = screenResolutions[screenResolutionIndex];
                string[] parts = selectedRes.Split('x');
                if (parts.Length == 2)
                {
                    if (int.TryParse(parts[0].Trim(), out int width) && int.TryParse(parts[1].Trim(), out int height))
                    {
                        // FIX: Dùng displayModeIndex thay vì Screen.fullScreen
                        FullScreenMode mode = displayModeIndex switch
                        {
                            0 => FullScreenMode.FullScreenWindow,
                            1 => FullScreenMode.Windowed,
                            2 => FullScreenMode.MaximizedWindow,
                            _ => FullScreenMode.FullScreenWindow
                        };
                        Screen.SetResolution(width, height, mode);
                    }
                }
            }

            // Display Mode đã được apply cùng SetResolution ở trên

            // Apply Frame Rate
            Application.targetFrameRate = currentFrameRate;



            // Apply Post-Processing settings (placeholder - requires Post-Processing Stack or URP)
            Debug.Log($"Chromatic Aberration: {(chromaticAberrationEnabled ? "Enabled" : "Disabled")}");
            Debug.Log($"Sharpening: {(sharpeningEnabled ? "Enabled" : "Disabled")}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Load all saved settings
        /// </summary>
        public void LoadSettings()
        {
            var gs = GameSettings.Instance;
            if (gs == null) return;

            // Audio — đọc từ GameSettings
            if (tab_MasterVolume != null) tab_MasterVolume.value = gs.masterVolume;
            if (tab_MusicVolume != null) tab_MusicVolume.value = gs.musicVolume;
            if (tab_SFXVolume != null) tab_SFXVolume.value = gs.sfxVolume;
            voiceLanguageIndex = gs.voiceLanguageIndex;
            backgroundSoundEnabled = gs.backgroundSoundEnabled;

            // Graphics — đọc từ GameSettings
            if (tab_Brightness != null) tab_Brightness.value = gs.brightness;
            saturationEnabled = gs.saturationEnabled;
            if (tab_Contrast != null) tab_Contrast.value = gs.contrast;

            screenResolutionIndex = gs.screenResolutionIndex;
            if (screenResolutions.Count > 0)
                screenResolutionIndex = Mathf.Clamp(screenResolutionIndex, 0, screenResolutions.Count - 1);

            displayModeIndex = Mathf.Clamp(gs.displayModeIndex, 0, displayModes.Count - 1);
            currentFrameRate = gs.frameRate;

            chromaticAberrationEnabled = gs.chromaticAberrationEnabled;
            sharpeningEnabled = gs.sharpeningEnabled;
            miniMapEnabled = gs.miniMapEnabled;

            // Update frame rate index
            for (int i = 0; i < frameRates.Length; i++)
            {
                if (frameRates[i] == currentFrameRate)
                {
                    frameRateIndex = i;
                    break;
                }
            }

            // Gameplay — đọc từ GameSettings
            if (tab_CameraMouseSpeed != null) tab_CameraMouseSpeed.value = gs.cameraMouseSpeed;
            if (tab_CameraRotateSpeed != null) tab_CameraRotateSpeed.value = gs.cameraRotateSpeed;
            if (tab_CameraZoomSpeedGameplay != null) tab_CameraZoomSpeedGameplay.value = gs.cameraZoomSpeed;

            UpdateGraphicsButtonTexts();
            UpdateAudioButtonTexts();
            UpdateGameplayButtonTexts();

            // Controls - Key Bindings (vẫn load riêng)
            LoadKeyBindings();
            UpdateKeyBindTexts();
        }

        /// <summary>
        /// Save current settings
        /// </summary>
        public void SaveSettings()
        {
            ApplySettings();
            Debug.Log("Settings saved!");
        }

        /// <summary>
        /// Open settings panel
        /// </summary>
        public void OpenSettings()
        {
            if (panel_GUISettings != null)
            {
                // Đảm bảo tất cả parent từ panel_GUISettings lên Canvas đều active
                EnsureHierarchyActive(panel_GUISettings.transform);

                panel_GUISettings.SetActive(true);

                // Bật các children layout quan trọng (Middle, Right Side, Controller_Button...)
                // Chúng có thể bị tắt do GameMenuManager.HideAllPanels()
                foreach (Transform child in panel_GUISettings.transform)
                {
                    // Bật tất cả children trực tiếp trừ các tab panel (sẽ do SwitchToPanel quản lý)
                    bool isTabPanel = (child.gameObject == panel_Gameplay ||
                                       child.gameObject == panel_Controller ||
                                       child.gameObject == panel_Graphics ||
                                       child.gameObject == panel_Audio);
                    if (!isTabPanel)
                    {
                        child.gameObject.SetActive(true);
                    }
                }

                // Default to Gameplay panel
                SwitchToPanel(panel_Gameplay);
                // Load current values
                LoadSettings();

                Debug.Log("[SettingsManager] OpenSettings complete — all layout children activated");
            }
        }

        /// <summary>
        /// Đảm bảo tất cả parent trong hierarchy đều active (lên tới Canvas root)
        /// </summary>
        private void EnsureHierarchyActive(Transform child)
        {
            Transform current = child.parent;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    Debug.Log($"[SettingsManager] Activating inactive parent: {current.gameObject.name}");
                    current.gameObject.SetActive(true);
                }
                if (current.GetComponent<Canvas>() != null)
                    break;
                current = current.parent;
            }
        }

        /// <summary>
        /// Close settings panel
        /// </summary>
        public void CloseSettings()
        {
            if (panel_GUISettings != null)
                panel_GUISettings.SetActive(false);
            
            if (panel_GUIGame != null)
                panel_GUIGame.SetActive(true);
        }

        #endregion

        #region Public Properties

        public float MasterVolume => PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, defaultMasterVolume);
        public float MusicVolume => PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, defaultMusicVolume);
        public float SFXVolume => PlayerPrefs.GetFloat(KEY_SFX_VOLUME, defaultSfxVolume);
        
        public float Brightness => PlayerPrefs.GetFloat(KEY_BRIGHTNESS, defaultBrightness);
        public bool Saturation => PlayerPrefs.GetInt(KEY_SATURATION, defaultSaturation ? 1 : 0) == 1;
        public float Contrast => PlayerPrefs.GetFloat(KEY_CONTRAST, defaultContrast);

        // New Graphics Properties
        public int ScreenResolution => PlayerPrefs.GetInt(KEY_SCREEN_RESOLUTION, defaultScreenResolution);
        public int DisplayMode => PlayerPrefs.GetInt(KEY_DISPLAY_MODE, defaultDisplayMode);
        public int FrameRate => PlayerPrefs.GetInt(KEY_FRAME_RATE, defaultFrameRate);

        public bool ChromaticAberration => PlayerPrefs.GetInt(KEY_CHROMATIC_ABERRATION, defaultChromaticAberration ? 1 : 0) == 1;
        public bool Sharpening => PlayerPrefs.GetInt(KEY_SHARPENING, defaultSharpening ? 1 : 0) == 1;



        
        public float CameraMouseSpeed => PlayerPrefs.GetFloat(KEY_CAMERA_MOUSE_SPEED, defaultCameraMouseSpeed);
        public float CameraRotateSpeed => PlayerPrefs.GetFloat(KEY_CAMERA_ROTATE_SPEED, defaultCameraRotateSpeed);
        
        public bool HasUnsavedChanges => hasUnsavedChanges;

        /// <summary>
        /// Get the current key binding for a specific action
        /// </summary>
        public KeyCode GetKeyBinding(string action)
        {
            return keyBindings.ContainsKey(action) ? keyBindings[action] : KeyCode.None;
        }

        /// <summary>
        /// Apply key bindings vào InputSystem (override InputActions runtime)
        /// Tìm PlayerInput trên player GameObject
        /// </summary>
        private void ApplyKeyBindingsToInputSystem()
        {
            // Tìm PlayerInput trên Player
            PlayerInput playerInput = null;
            
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerInput = playerObj.GetComponent<PlayerInput>();
            }

            if (playerInput == null)
            {
                // Không tìm thấy player (có thể đang trong UI_Game lobby) — bỏ qua
                Debug.Log("[SettingsManager] PlayerInput not found — key bindings saved but not applied to InputSystem");
                return;
            }

            InputRebindHelper.ApplyAllBindings(playerInput, GetKeyBinding);
            InputRebindHelper.SaveBindingOverrides(playerInput);
        }

        #endregion
    }
}
