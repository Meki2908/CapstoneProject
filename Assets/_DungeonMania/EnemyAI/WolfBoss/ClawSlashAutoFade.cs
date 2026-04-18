using UnityEngine;
using System.Collections;

public class ClawSlashAutoFade : MonoBehaviour
{
    [Header("Behavior")]
    [Tooltip("Thời gian tồn tại của nhát chém.")]
    public float duration = 1.0f;

    [Tooltip("Có tự động mở rộng theo trục X hoặc Y không?")]
    public bool expandOverTime = true;
    public Vector3 expandScale = new Vector3(1.2f, 1f, 1.2f);
    
    [Tooltip("Có fade out Renderer không (nếu material có hỗ trợ Transparent/Color alpha)")]
    public bool fadeOut = true;

    private Vector3 initialScale;
    private Renderer[] renderers;

    void Start()
    {
        initialScale = transform.localScale;
        renderers = GetComponentsInChildren<Renderer>();
        StartCoroutine(AnimateSlash());
    }

    IEnumerator AnimateSlash()
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / duration;

            // 1. Phóng to nhẹ tạo cảm giác chém ra (tuỳ chọn)
            if (expandOverTime)
            {
                transform.localScale = Vector3.Lerp(initialScale, Vector3.Scale(initialScale, expandScale), normalizedTime);
            }

            // 2. Fade alpha material (yêu cầu Material phải là dạng Transparent, ví dụ URP/Particles/Unlit)
            if (fadeOut && renderers != null)
            {
                // Dùng Color của shader (với Shader Graph thường là "_BaseColor" hoặc Particle Standard là "_Color")
                foreach (var r in renderers)
                {
                    if (r.material.HasProperty("_BaseColor"))
                    {
                        Color c = r.material.GetColor("_BaseColor");
                        c.a = Mathf.Lerp(1f, 0f, normalizedTime);
                        r.material.SetColor("_BaseColor", c);
                    }
                    else if (r.material.HasProperty("_Color"))
                    {
                        Color c = r.material.GetColor("_Color");
                        c.a = Mathf.Lerp(1f, 0f, normalizedTime);
                        r.material.SetColor("_Color", c);
                    }
                }
            }

            yield return null;
        }

        // Đảm bảo object biến mất
        Destroy(gameObject);
    }
}
