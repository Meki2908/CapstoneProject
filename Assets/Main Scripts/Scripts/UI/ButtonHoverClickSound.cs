using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Plays lightweight UI SFX on hover/click.
/// Compatible with legacy prefab data that serialized:
/// clickVolume, hoverVolume, playHoverSound.
/// </summary>
[DisallowMultipleComponent]
public sealed class ButtonHoverClickSound : MonoBehaviour, IPointerEnterHandler
{
    [Range(0f, 1f)] public float clickVolume = 0.8f;
    [Range(0f, 1f)] public float hoverVolume = 0.4f;
    public bool playHoverSound = true;

    static AudioSource s_sharedSource;
    static AudioClip s_hoverClip;
    static AudioClip s_clickClip;

    Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.RemoveListener(PlayClick);
            _button.onClick.AddListener(PlayClick);
        }
    }

    void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(PlayClick);
    }

    public void OnPointerEnter(PointerEventData _)
    {
        if (!playHoverSound)
            return;

        if (s_hoverClip == null)
            return;

        EnsureSharedSource();
        if (s_sharedSource != null)
            s_sharedSource.PlayOneShot(s_hoverClip, Mathf.Clamp01(hoverVolume));
    }

    void PlayClick()
    {
        if (s_clickClip == null)
            return;

        EnsureSharedSource();
        if (s_sharedSource != null)
            s_sharedSource.PlayOneShot(s_clickClip, Mathf.Clamp01(clickVolume));
    }

    static void EnsureSharedSource()
    {
        if (s_sharedSource != null)
            return;

        var go = new GameObject("UI_ButtonSfx");
        DontDestroyOnLoad(go);
        s_sharedSource = go.AddComponent<AudioSource>();
        s_sharedSource.playOnAwake = false;
    }
}
