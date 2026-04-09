using Fusion;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays real-time network ping (RTT) in milliseconds.
/// Attaches to a TextMeshProUGUI element. Updates every updateInterval seconds.
/// Shows "Ping: --" when not connected, colored green/yellow/red based on latency.
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

        var runner = MultiplayerManager.Runner;
        if (runner == null || !runner.IsRunning)
        {
            pingText.text = "Ping: --";
            pingText.color = new Color(0.5f, 0.5f, 0.5f);
            return;
        }

        double rtt;

        if (runner.IsServer)
        {
            // Host always has 0 ping to itself
            rtt = 0;
        }
        else
        {
            // Client: get RTT to server
            rtt = runner.GetPlayerRtt(runner.LocalPlayer);
        }

        int rttMs = (int)(rtt * 1000.0);
        pingText.text = $"Ping: {rttMs} ms";

        // Color based on latency
        if (rttMs < 80)
            pingText.color = new Color(0.3f, 1f, 0.3f); // Green
        else if (rttMs < 150)
            pingText.color = new Color(1f, 0.9f, 0.2f); // Yellow
        else
            pingText.color = new Color(1f, 0.3f, 0.3f); // Red
    }

    /// <summary>Set the text component at runtime.</summary>
    public void SetPingText(TextMeshProUGUI text)
    {
        pingText = text;
    }
}
