using System.Collections;
using UnityEngine;

/// <summary>
/// Đặt script này lên ROOT object của prefab "Claw slash".
/// Khi được Instantiate, nó sẽ tự động chạy animation:
///   1. Reveal: mesh xuất hiện từ trên xuống (0 → 2.0s)
///   2. Fade:   mesh mờ dần từ đỉnh xuống sau khi reveal xong
///   3. Destroy sau khi hết hiệu ứng
/// </summary>
public class ClawSlashVFX : MonoBehaviour
{
    // ── Tham số tuỳ chỉnh ───────────────────────────────────────────────────
    [Header("Timing")]
    [Tooltip("Tổng thời gian reveal (mesh xuất hiện từ trên xuống).")]
    public float revealDuration = 0.35f;

    [Tooltip("Thời gian chờ sau khi reveal xong trước khi bắt đầu fade.")]
    public float holdDuration = 0.25f;

    [Tooltip("Tổng thời gian fade out (mờ dần từ đỉnh xuống).")]
    public float fadeDuration = 1.4f;

    [Header("Appearance")]
    [Tooltip("Màu sắc nhát chém (RGB) + cường độ Alpha (A, thường để 1).")]
    public Color slashColor = new Color(0.3f, 0.8f, 1f, 1f);

    [Tooltip("Cường độ glow (emission) — 2-5 cho URP bloom đẹp).")]
    public float glowIntensity = 3.5f;

    // ── Shader Property IDs (cached để tránh string lookup) ─────────────────
    static readonly int ID_Reveal        = Shader.PropertyToID("_RevealProgress");
    static readonly int ID_Fade          = Shader.PropertyToID("_FadeProgress");
    static readonly int ID_Color         = Shader.PropertyToID("_SlashColor");
    static readonly int ID_GlowIntensity = Shader.PropertyToID("_GlowIntensity");

    // ── Runtime ─────────────────────────────────────────────────────────────
    private MaterialPropertyBlock _mpb;
    private Renderer[] _renderers;

    // ────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        // Lấy toàn bộ Renderer từ mình và các con (7 Curve meshes)
        _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

        // Đặt màu + glow ngay lập tức
        ApplyAll(0f, 0f);
    }

    private void Start()
    {
        StartCoroutine(PlayEffect());
    }

    // ── Coroutine chính ──────────────────────────────────────────────────────

    IEnumerator PlayEffect()
    {
        // — Phase 1: Reveal (0 → 1 theo trục Y UV) ——————————————————————────
        float t = 0f;
        while (t < revealDuration)
        {
            t += Time.deltaTime;
            float progress = Easing.EaseOutCubic(Mathf.Clamp01(t / revealDuration));
            // _RevealProgress chạy từ 1 (ẩn hoàn toàn ở đầu) → 0 (hiện hoàn toàn)
            // Shader: hiện khi UV.y < revealProgress  ⇒ ta cần flip: progress = 1-norm
            ApplyAll(revealProgress: 1f - progress, fadeProgress: 0f);
            yield return null;
        }

        // Đảm bảo fully revealed
        ApplyAll(0f, 0f);

        // — Giữ nguyên một chút ————————————————————————————————————————————
        yield return new WaitForSeconds(holdDuration);

        // — Phase 2: Fade từ đỉnh xuống ————————————————————————————————————
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float progress = Easing.EaseInQuad(Mathf.Clamp01(t / fadeDuration));
            // _FadeProgress: 0 (không fade gì) → 1.2 (fade xong hoàn toàn, vượt 1 để chắc ăn)
            ApplyAll(0f, progress * 1.2f);
            yield return null;
        }

        Destroy(gameObject);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    void ApplyAll(float revealProgress, float fadeProgress)
    {
        _mpb.SetFloat(ID_Reveal, revealProgress);
        _mpb.SetFloat(ID_Fade, fadeProgress);
        _mpb.SetColor(ID_Color, slashColor);
        _mpb.SetFloat(ID_GlowIntensity, glowIntensity);

        foreach (var r in _renderers)
            r.SetPropertyBlock(_mpb);
    }

    // ── API công khai ─────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ BossPawDamage để override màu trước khi biến động (VD Phase 2 màu đỏ).
    /// </summary>
    public void SetColor(Color color, float intensity = -1f)
    {
        slashColor = color;
        if (intensity >= 0f) glowIntensity = intensity;
        ApplyAll(0f, 0f); // refresh ngay nếu chưa chạy
    }
}

// ── Easing utilities nhỏ gọn ─────────────────────────────────────────────────
internal static class Easing
{
    public static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    public static float EaseInQuad(float t)   => t * t;
}
