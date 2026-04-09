using UnityEngine;

/// <summary>
/// Floating 3D text cho combat feedback (BLOCKED, IMMUNE, MISS, etc.)
/// Spawn tại runtime bằng TextMesh, float lên và fade out. Không cần prefab.
/// 
/// TỰ ĐỘNG load font từ FloatingTextSettings (ScriptableObject trong Resources).
/// Hoạt động ở MỌI scene — không cần gắn config vào scene.
/// </summary>
public class FloatingCombatText : MonoBehaviour
{
    // === STATIC CONFIG — auto-load từ Resources ===
    private static FloatingTextSettings settings;
    private static bool hasLoadedSettings = false;
    
    private TextMesh textMesh;
    private float duration = 1.5f;
    private float elapsed = 0f;
    private Vector3 startPos;
    private Color startColor;
    
    /// <summary>
    /// Load settings từ Resources (1 lần duy nhất)
    /// </summary>
    static void EnsureSettings()
    {
        if (hasLoadedSettings) return;
        hasLoadedSettings = true;
        
        settings = Resources.Load<FloatingTextSettings>("FloatingTextSettings");
        if (settings != null)
        {
            Debug.Log($"[FloatingCombatText] Loaded settings: font={settings.customFont?.name ?? "Default"}");
        }
    }
    
    /// <summary>
    /// Spawn floating text tại vị trí world-space
    /// </summary>
    public static void Spawn(Vector3 position, string text, Color color, float size = -1f, float dur = -1f)
    {
        EnsureSettings();
        
        float charSize = (size > 0) ? size : (settings != null ? settings.characterSize : 0.4f);
        int fontSz = settings != null ? settings.fontSize : 48;
        float displayDur = (dur > 0) ? dur : (settings != null ? settings.displayDuration : 1.5f);
        
        GameObject go = new GameObject("FloatingText_" + text);
        go.transform.position = position + Vector3.up * 2.2f;
        
        FloatingCombatText fct = go.AddComponent<FloatingCombatText>();
        fct.duration = displayDur;
        fct.startPos = go.transform.position;
        fct.startColor = color;
        
        fct.textMesh = go.AddComponent<TextMesh>();
        fct.textMesh.text = text;
        fct.textMesh.color = color;
        fct.textMesh.characterSize = charSize;
        fct.textMesh.fontSize = fontSz;
        fct.textMesh.anchor = TextAnchor.MiddleCenter;
        fct.textMesh.alignment = TextAlignment.Center;
        fct.textMesh.fontStyle = FontStyle.Bold;
        
        // Áp dụng custom font nếu có
        if (settings != null && settings.customFont != null)
        {
            fct.textMesh.font = settings.customFont;
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null && settings.customFont.material != null)
            {
                mr.material = settings.customFont.material;
            }
        }
    }
    
    /// <summary>
    /// Spawn "BLOCKED" text với màu từ settings
    /// </summary>
    public static void SpawnBlocked(Vector3 position)
    {
        EnsureSettings();
        Color blockedColor = (settings != null) ? settings.blockedColor : new Color(0.3f, 0.7f, 1f);
        Spawn(position, "BLOCKED", blockedColor);
    }
    
    void Update()
    {
        elapsed += Time.deltaTime;
        
        if (elapsed >= duration)
        {
            Destroy(gameObject);
            return;
        }
        
        float t = elapsed / duration;
        
        // Float lên
        transform.position = startPos + Vector3.up * t * 1.5f;
        
        // Luôn hướng về camera
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0);
        }
        
        // Fade out ở 40% cuối
        if (t > 0.6f)
        {
            float fadeT = (t - 0.6f) / 0.4f;
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 1f - fadeT);
        }
        
        // Scale pop effect khi xuất hiện
        if (t < 0.12f)
        {
            float s = Mathf.Lerp(0.5f, 1.25f, t / 0.12f);
            transform.localScale = Vector3.one * s;
        }
        else if (t < 0.22f)
        {
            float s = Mathf.Lerp(1.25f, 1f, (t - 0.12f) / 0.1f);
            transform.localScale = Vector3.one * s;
        }
    }
}
