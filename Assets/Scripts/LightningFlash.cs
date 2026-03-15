using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VFX tia sét đỏ thuần visual — không đèn, không skybox.
/// Nếu bật followCamera: luôn bám theo camera ở XZ nhưng giữ Y cố định,
/// tạo cảm giác sét nằm trên bầu trời dù player đi đâu.
/// </summary>
public class LightningFlash : MonoBehaviour
{
    [Header("Sky Tracking (bám bầu trời)")]
    [Tooltip("Bật để VFX luôn theo camera ở XZ — chỉ dùng khi hố đen là skybox texture. Nếu hố đen là object cố định trong world thì TẮT cái này.")]
    public bool followCamera = false;
    [Tooltip("Độ cao cố định của tâm hiệu ứng (nên đặt ngang với hố đen)")]
    public float skyHeight = 150f;

    [Header("Timing")]
    [Tooltip("Thời gian tối thiểu giữa mỗi đợt sét (giây)")]
    public float minTime = 0.5f;
    [Tooltip("Thời gian tối đa giữa mỗi đợt sét (giây)")]
    public float maxTime = 2.5f;
    [Tooltip("Thời gian mỗi tia sáng lên (giây)")]
    public float flashDuration = 0.1f;

    [Header("Bolt Count")]
    [Tooltip("Số tia sét xuất hiện mỗi đợt")]
    public int boltCount = 3;

    [Header("Bolt Shape")]
    [Tooltip("Số điểm gấp của mỗi tia (nhiều hơn = ngoằn ngoèo hơn)")]
    public int boltSegments = 16;
    [Tooltip("Độ ngoằn ngoèo")]
    public float boltJitter = 6f;
    [Tooltip("Chiều dài tia sét (hướng xuống)")]
    public float boltLength = 80f;
    [Tooltip("Bán kính vùng sét có thể xuất hiện xung quanh tâm")]
    public float spawnRadius = 40f;

    [Header("Bolt Appearance")]
    [Tooltip("Cường độ sáng tổng thể (0 = tắt, 1 = bình thường, >1 = rực sáng hơn)")]
    [Range(0f, 5f)]
    public float intensity = 1f;
    [Tooltip("Độ rộng lõi tia sáng (Unity units)")]
    public float coreWidth = 0.6f;
    [Tooltip("Độ rộng lớp glow (to hơn lõi)")]
    public float glowWidth = 3f;
    [Tooltip("Màu lõi tia sét (nên gần trắng-đỏ để trông sáng)")]
    public Color coreColor = new Color(1f, 0.85f, 0.85f, 1f);
    [Tooltip("Màu glow phía ngoài")]
    public Color glowColor = new Color(1f, 0.1f, 0.05f, 0.6f);

    // --- Private ---
    private List<LineRenderer> _cores  = new List<LineRenderer>();
    private List<LineRenderer> _glows  = new List<LineRenderer>();
    private Camera _cam;
    private Material _coreMat;
    private Material _glowMat;

    void Awake()
    {
        _coreMat = CreateAdditiveMaterial(coreColor);
        _glowMat = CreateAdditiveMaterial(glowColor);
        _cam = Camera.main;
    }

    void Update()
    {
        // Bám theo camera để sét luôn xuất hiện trên bầu trời cùng hướng
        if (followCamera && _cam != null)
        {
            Vector3 camPos = _cam.transform.position;
            transform.position = new Vector3(camPos.x, skyHeight, camPos.z);
        }
    }

    void Start()
    {
        // Tạo pool LineRenderer
        for (int i = 0; i < boltCount; i++)
        {
            _glows.Add(CreateLR("Glow_" + i, _glowMat, glowWidth, glowWidth * 0.4f));
            _cores.Add(CreateLR("Core_" + i, _coreMat, coreWidth, coreWidth * 0.3f));
        }

        StartCoroutine(LightningLoop());
    }

    // ──────────────────────────── Helpers ────────────────────────────

    Material CreateAdditiveMaterial(Color color)
    {
        Shader sh = Shader.Find("Legacy Shaders/Particles/Additive")
                 ?? Shader.Find("Particles/Additive")
                 ?? Shader.Find("Particles/Standard Unlit")
                 ?? Shader.Find("Sprites/Default")
                 ?? Shader.Find("Standard");
        Material mat = new Material(sh);
        mat.color = color;
        mat.renderQueue = 4000;
        return mat;
    }

    LineRenderer CreateLR(string goName, Material mat, float startW, float endW)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(this.transform, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material        = mat;
        lr.startColor      = mat.color;
        lr.endColor        = new Color(mat.color.r, mat.color.g, mat.color.b, 0f);
        lr.startWidth      = startW;
        lr.endWidth        = endW;
        lr.positionCount   = 0;
        lr.useWorldSpace   = true;
        lr.numCapVertices  = 6;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows  = false;
        lr.enabled         = false;
        return lr;
    }

    // ──────────────────────────── Coroutine ────────────────────────────

    IEnumerator LightningLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minTime, maxTime));
            ShowBolts(true);
            yield return new WaitForSeconds(flashDuration);
            ShowBolts(false);

            // Đôi khi có tia nhanh thứ 2 liền sau
            if (Random.value < 0.4f)
            {
                yield return new WaitForSeconds(Random.Range(0.05f, 0.12f));
                ShowBolts(true);
                yield return new WaitForSeconds(flashDuration * 0.7f);
                ShowBolts(false);
            }
        }
    }

    // Tính màu đã nhân intensity (clamp alpha về 1 tối đa, RGB có thể > 1 cho HDR)
    Color ApplyIntensity(Color baseColor)
    {
        return new Color(
            baseColor.r * intensity,
            baseColor.g * intensity,
            baseColor.b * intensity,
            Mathf.Clamp01(baseColor.a * intensity));
    }

    void ShowBolts(bool show)
    {
        for (int i = 0; i < boltCount; i++)
        {
            if (show)
            {
                // Áp dụng intensity vào màu trước khi vẽ
                Color c = ApplyIntensity(coreColor);
                _cores[i].startColor = c;
                _cores[i].endColor   = new Color(c.r, c.g, c.b, 0f);

                Color g = ApplyIntensity(glowColor);
                _glows[i].startColor = g;
                _glows[i].endColor   = new Color(g.r, g.g, g.b, 0f);

                Vector3[] pts = GenerateBoltPoints();
                ApplyPoints(_cores[i], pts);
                ApplyPoints(_glows[i], pts);
            }
            else
            {
                _cores[i].enabled = false;
                _glows[i].enabled = false;
            }
        }
    }

    // ──────────────────────────── Point generation ────────────────────────────

    Vector3[] GenerateBoltPoints()
    {
        // Bắt đầu từ tâm (vị trí GameObject) + lệch ngẫu nhiên trong bán kính
        Vector3 offset = new Vector3(
            Random.Range(-spawnRadius, spawnRadius),
            0f,
            Random.Range(-spawnRadius, spawnRadius));

        Vector3 start = transform.position + offset;
        Vector3 end   = start + Vector3.down * boltLength
                      + new Vector3(Random.Range(-spawnRadius * 0.3f, spawnRadius * 0.3f),
                                    0f,
                                    Random.Range(-spawnRadius * 0.3f, spawnRadius * 0.3f));

        Vector3[] pts = new Vector3[boltSegments];
        Vector3 dir   = (end - start) / (boltSegments - 1);
        pts[0]        = start;

        for (int i = 1; i < boltSegments - 1; i++)
        {
            pts[i] = start + dir * i + Random.insideUnitSphere * boltJitter;
        }
        pts[boltSegments - 1] = end;
        return pts;
    }

    void ApplyPoints(LineRenderer lr, Vector3[] pts)
    {
        lr.positionCount = pts.Length;
        lr.SetPositions(pts);
        lr.enabled = true;
    }
}
