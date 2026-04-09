using UnityEngine;

/// <summary>
/// UI to nhỏ liên tục (pulse effect).
/// Gắn lên bất kỳ UI element nào (Button, Image, Text, Panel...).
/// </summary>
public class UIPulse : MonoBehaviour
{
    [Range(0.8f, 1.0f)] public float minScale = 0.9f;
    [Range(1.0f, 1.5f)] public float maxScale = 1.1f;
    [Range(0.5f, 5f)]   public float speed = 2f;

    Vector3 _baseScale;

    void OnEnable()
    {
        _baseScale = transform.localScale;
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) + 1f) * 0.5f;
        float s = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = _baseScale * s;
    }

    void OnDisable()
    {
        transform.localScale = _baseScale;
    }
}
