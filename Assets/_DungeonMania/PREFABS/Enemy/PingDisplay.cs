using TMPro;
using UnityEngine;

/// <summary>
/// Hiển thị ping — NGO đã gỡ; có thể nối Fusion RTT sau.
/// </summary>
public class PingDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI pingText;
    [SerializeField] private float updateInterval = 1f;

    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < updateInterval) return;
        _timer = 0f;

        if (pingText == null) return;
        pingText.text = "Ping: --";
        pingText.color = new Color(0.5f, 0.5f, 0.5f);
    }

    public void SetPingText(TextMeshProUGUI text)
    {
        pingText = text;
    }
}
