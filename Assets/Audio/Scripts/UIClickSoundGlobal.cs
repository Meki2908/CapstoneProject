using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Tự động phát âm thanh click/hover cho TẤT CẢ buttons trong game.
/// Bao gồm cả buttons ẩn (Pause menu, Settings, Win/Lose panel...).
/// Gắn vào cùng GameObject với SoundManager.
/// 
/// KHÔNG cần gán sound cho từng button riêng lẻ.
/// </summary>
public class UIClickSoundGlobal : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Phát sound khi hover? (để false nếu không muốn)")]
    public bool playHoverSound = true;
    
    [Tooltip("Volume cho UI click")]
    [Range(0f, 1f)]
    public float clickVolume = 0.8f;
    
    [Tooltip("Volume cho UI hover")]
    [Range(0f, 1f)]
    public float hoverVolume = 0.4f;

    [Header("Auto Refresh")]
    [Tooltip("Tự refresh mỗi X giây để bắt buttons mới spawn")]
    public float refreshInterval = 2f;

    private static UIClickSoundGlobal instance;
    private float nextRefreshTime;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }
        instance = this;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Refresh sau 0.5s khi scene mới load (chờ UI instantiate xong)
        Invoke(nameof(RegisterAllUI), 0.5f);
    }

    void Start()
    {
        RegisterAllUI();
        nextRefreshTime = Time.unscaledTime + refreshInterval;
    }

    void Update()
    {
        // Tự refresh định kỳ để bắt buttons mới (win/lose panels, popups...)
        if (Time.unscaledTime >= nextRefreshTime)
        {
            nextRefreshTime = Time.unscaledTime + refreshInterval;
            RegisterAllUI();
        }
    }

    /// <summary>
    /// Gọi từ bên ngoài khi cần refresh ngay (spawn UI mới, load scene...)
    /// </summary>
    public static void RefreshButtons()
    {
        if (instance != null) instance.RegisterAllUI();
    }

    void RegisterAllUI()
    {
        // === TÌM CẢ BUTTONS ẨN (inactive) ===
        // FindObjectsByType chỉ tìm active objects
        // → Dùng Resources.FindObjectsOfTypeAll để tìm CẢ ẩn
        
        // Buttons — click + hover
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (var btn in allButtons)
        {
            // Bỏ qua prefab trong Assets (chỉ xử lý objects trong scene)
            if (!IsSceneObject(btn.gameObject)) continue;
            if (btn.GetComponent<UIClickSoundListener>() != null) continue;
            
            var listener = btn.gameObject.AddComponent<UIClickSoundListener>();
            listener.clickVolume = clickVolume;
            listener.hoverVolume = hoverVolume;
            listener.playHoverSound = playHoverSound;
        }
        
        // Selectables (Slider, Toggle, Dropdown...) — hover only
        if (playHoverSound)
        {
            Selectable[] allSelectables = Resources.FindObjectsOfTypeAll<Selectable>();
            foreach (var sel in allSelectables)
            {
                if (sel is Button) continue;
                if (!IsSceneObject(sel.gameObject)) continue;
                if (sel.GetComponent<UIHoverSoundListener>() != null) continue;
                
                var hover = sel.gameObject.AddComponent<UIHoverSoundListener>();
                hover.hoverVolume = hoverVolume;
            }
        }
    }

    /// <summary>
    /// Kiểm tra object thuộc scene (không phải prefab trong Assets)
    /// </summary>
    bool IsSceneObject(GameObject go)
    {
        return go.scene.IsValid() && go.scene.isLoaded;
    }

    /// <summary>
    /// Gọi trực tiếp để phát click sound (dùng từ UnityEvent trên Button)
    /// </summary>
    public void PlayClick()
    {
        SoundManager.PlayUIClick(null, clickVolume);
    }
}

/// <summary>
/// Component nhỏ gắn vào từng Button để phát sound khi click/hover.
/// Được UIClickSoundGlobal tự động thêm vào.
/// </summary>
public class UIClickSoundListener : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    [HideInInspector] public float clickVolume = 0.8f;
    [HideInInspector] public float hoverVolume = 0.4f;
    [HideInInspector] public bool playHoverSound = false;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        SoundManager.PlayUIClick(null, clickVolume);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (playHoverSound)
            SoundManager.PlayUIHover(null, hoverVolume);
    }
}

/// <summary>
/// Hover-only sound cho Selectables (Slider, Toggle, Dropdown, v.v.)
/// Được UIClickSoundGlobal tự động thêm vào khi playHoverSound = true.
/// </summary>
public class UIHoverSoundListener : MonoBehaviour, IPointerEnterHandler
{
    [HideInInspector] public float hoverVolume = 0.4f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        SoundManager.PlayUIHover(null, hoverVolume);
    }
}
