using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

/// <summary>
/// World-space name tag for a <see cref="Character"/>. Parent should sit on a head/socket transform;
/// this script only billboards toward the local camera, scales by distance, and renders on top of geometry when enabled.
/// </summary>
public class PlayerNameplate : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] float baseWorldScale = 0.012f;

    [Header("Distance Scaling")]
    [SerializeField] bool enableDistanceScaling = true;
    [SerializeField] float minScaleMultiplier = 0.8f;
    [SerializeField] float maxScaleMultiplier = 4f;
    [SerializeField] float distanceScaleFactor = 0.08f;

    [Header("Always On Top")]
    [Tooltip("Draw through walls/obstacles (TMP material ZTest Always).")]
    [SerializeField] bool alwaysOnTop = true;

    /// <summary>Matches TMP SDF shaders: <c>ZTest [unity_GUIZTestMode]</c> (see <c>TMP_SDF.shader</c> in this project).</summary>
    static readonly int UnityGuiZTestModeId = Shader.PropertyToID("unity_GUIZTestMode");

    Character _owner;

    public static int PackArgb(Color32 c) => (c.a << 24) | (c.r << 16) | (c.g << 8) | c.b;

    public static int StableColorArgbFromString(string s)
    {
        int h = string.IsNullOrEmpty(s) ? 0 : s.GetHashCode();
        float hue = (Mathf.Abs(h) % 360) / 360f;
        Color rgb = Color.HSVToRGB(hue, 0.5f, 0.98f);
        var c = (Color32)rgb;
        c.a = 255;
        return PackArgb(c);
    }

    public static Color32 Color32FromArgb(int argb)
    {
        if (argb == 0)
            return Color.white;
        return new Color32(
            (byte)(argb >> 16),
            (byte)(argb >> 8),
            (byte)argb,
            (byte)(argb >> 24));
    }

    void Awake()
    {
        if (nameText == null)
            nameText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (nameText != null)
        {
            nameText.raycastTarget = false;

            if (alwaysOnTop)
            {
                Material materialInstance = nameText.fontMaterial;
                materialInstance.SetInt(UnityGuiZTestModeId, (int)CompareFunction.Always);
            }
        }

        var canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = null;
        }

        var ray = GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (ray != null)
            ray.enabled = false;
    }

    public void Bind(Character owner)
    {
        if (_owner != null)
            _owner.DisplayInfoChanged -= ApplyFromOwner;

        _owner = owner;
        if (_owner != null)
        {
            _owner.DisplayInfoChanged += ApplyFromOwner;
            ApplyFromOwner();
        }
    }

    void OnDestroy()
    {
        if (_owner != null)
            _owner.DisplayInfoChanged -= ApplyFromOwner;
    }

    void ApplyFromOwner()
    {
        if (nameText == null || _owner == null)
            return;

        nameText.text = _owner.DisplayName.ToString();
        int argb = _owner.DisplayNameColorArgb;
        if (argb == 0)
            nameText.color = Color32FromArgb(StableColorArgbFromString(nameText.text));
        else
            nameText.color = Color32FromArgb(argb);
    }

    void LateUpdate()
    {
        if (_owner == null)
            return;

        var cam = Camera.main;
        if (cam == null)
            return;

        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);

        if (enableDistanceScaling)
        {
            float distance = Vector3.Distance(cam.transform.position, transform.position);
            float scaleMultiplier = Mathf.Clamp(distance * distanceScaleFactor, minScaleMultiplier, maxScaleMultiplier);
            transform.localScale = Vector3.one * (baseWorldScale * scaleMultiplier);
        }
        else
        {
            transform.localScale = Vector3.one * baseWorldScale;
        }
    }
}
