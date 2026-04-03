using UnityEngine;

/// <summary>
/// Gắn lên Cylinder cột sáng để tạo hiệu ứng nhấp nháy / pulse.
/// </summary>
public class BeamPulse : MonoBehaviour
{
    [Header("Pulse Settings")]
    [Range(0f, 1f)] public float minAlpha    = 0.2f;   // Alpha thấp nhất
    [Range(0f, 1f)] public float maxAlpha    = 0.7f;   // Alpha cao nhất
    public float speed = 2f;                            // Tốc độ nhấp nháy

    [Header("Emission Pulse (nếu material có Emission)")]
    public bool  pulseEmission = true;
    public Color emissionColor = new Color(1f, 0.8f, 0f); // Vàng
    public float emissionMin   = 0.5f;
    public float emissionMax   = 2.0f;

    Renderer   _rend;
    Material   _mat;          // instance material (không ảnh hưởng asset gốc)

    static readonly int _emissionID = Shader.PropertyToID("_EmissionColor");
    static readonly int _colorID    = Shader.PropertyToID("_BaseColor");    // URP
    // Standard shader dùng "_Color" — script tự thử cả hai bên dưới

    void Awake()
    {
        _rend = GetComponent<Renderer>();
        if (_rend) _mat = _rend.material;   // Tạo instance riêng
    }

    void Update()
    {
        if (_mat == null) return;

        float t     = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;   // 0 → 1
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        // ── Alpha (Transparent mode) ──────────────────────────────────────
        // URP: _BaseColor   |  Standard: _Color
        if (_mat.HasProperty("_BaseColor"))
        {
            Color c = _mat.GetColor("_BaseColor");
            c.a = alpha;
            _mat.SetColor("_BaseColor", c);
        }
        else if (_mat.HasProperty("_Color"))
        {
            Color c = _mat.GetColor("_Color");
            c.a = alpha;
            _mat.SetColor("_Color", c);
        }

        // ── Emission brightness ───────────────────────────────────────────
        if (pulseEmission && _mat.HasProperty(_emissionID))
        {
            float intensity = Mathf.Lerp(emissionMin, emissionMax, t);
            _mat.SetColor(_emissionID, emissionColor * intensity);
        }
    }
}
