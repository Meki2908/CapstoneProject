using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Debug overlay hiển thị góc trái màn hình — cho thấy:
///   - Scene hiện tại
///   - Role (Host/Client)
///   - Danh sách tất cả player đã connected + spawned
///   - Ping
///   - Trạng thái NetworkRunner
///
/// BẬT/TẮT bằng phím F3 (hoặc toggle trong Inspector).
/// Tự động gắn vào MultiplayerManager — không cần setup thêm.
/// </summary>
public class MultiplayerDebugOverlay : MonoBehaviour
{
    [Tooltip("Bật/tắt debug overlay.")]
    [SerializeField] private bool showOverlay = true;

    [Tooltip("Phím toggle overlay.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F3;

    private GUIStyle _boxStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _playerStyle;
    private bool _stylesInitialized;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showOverlay = !showOverlay;
        }
    }

    void OnGUI()
    {
        if (!showOverlay) return;

        InitStyles();

        var runner = MultiplayerManager.Runner;
        float x = 10;
        float y = 10;
        float w = 380;
        float lineH = 24;
        float padding = 8;

        // Tính chiều cao
        int playerCount = 0;
        if (runner != null && runner.IsRunning)
        {
            foreach (var _ in runner.ActivePlayers) playerCount++;
        }
        float h = padding * 2 + lineH * (6 + playerCount);

        // Background box
        GUI.Box(new Rect(x, y, w, h), "", _boxStyle);

        float cy = y + padding;

        // ─── Title ───
        GUI.Label(new Rect(x + padding, cy, w - padding * 2, lineH), "🔧 MULTIPLAYER DEBUG (F3 toggle)", _titleStyle);
        cy += lineH + 2;

        // ─── Scene ───
        string sceneName = SceneManager.GetActiveScene().name;
        GUI.Label(new Rect(x + padding, cy, w - padding * 2, lineH), $"📍 Scene: {sceneName}", _labelStyle);
        cy += lineH;

        // ─── Runner status ───
        if (runner == null || !runner.IsRunning)
        {
            GUI.Label(new Rect(x + padding, cy, w - padding * 2, lineH), "❌ Runner: NOT CONNECTED", _labelStyle);
            return;
        }

        string role = runner.IsServer ? "🟢 HOST" : "🔵 CLIENT";
        GUI.Label(new Rect(x + padding, cy, w - padding * 2, lineH), $"Role: {role}", _labelStyle);
        cy += lineH;

        string session = MultiplayerManager.CurrentSessionName ?? "--";
        GUI.Label(new Rect(x + padding, cy, w - padding * 2, lineH), $"🏠 Room: {session}", _labelStyle);
        cy += lineH;

        // ─── Ping ───
        if (!runner.IsServer)
        {
            double rtt = runner.GetPlayerRtt(runner.LocalPlayer);
            int rttMs = (int)(rtt * 1000.0);
            GUI.Label(new Rect(x + padding, cy, w - padding * 2, lineH), $"📶 Ping: {rttMs} ms", _labelStyle);
        }
        else
        {
            GUI.Label(new Rect(x + padding, cy, w - padding * 2, lineH), "📶 Ping: 0 ms (Host)", _labelStyle);
        }
        cy += lineH;

        // ─── Player list ───
        GUI.Label(new Rect(x + padding, cy, w - padding * 2, lineH), $"👥 Players ({playerCount}):", _labelStyle);
        cy += lineH;

        foreach (var p in runner.ActivePlayers)
        {
            bool isLocal = (p == runner.LocalPlayer);
            bool isHost = (p.PlayerId == 1);
            string icon = isHost ? "👑" : "🎮";
            string localTag = isLocal ? " ← YOU" : "";
            string spawned = "❓";

            // Kiểm tra player đã được spawn chưa
            if (runner.TryGetPlayerObject(p, out var playerObj))
            {
                spawned = playerObj != null ? "✅ Spawned" : "⏳ Pending";
            }
            else
            {
                spawned = "⏳ Not spawned";
            }

            string line = $"  {icon} Player {p.PlayerId}{localTag} — {spawned}";
            GUI.Label(new Rect(x + padding, cy, w - padding * 2, lineH), line, _playerStyle);
            cy += lineH;
        }
    }

    void InitStyles()
    {
        if (_stylesInitialized) return;
        _stylesInitialized = true;

        _boxStyle = new GUIStyle(GUI.skin.box);
        _boxStyle.normal.background = MakeTex(2, 2, new Color(0.05f, 0.05f, 0.12f, 0.9f));

        _titleStyle = new GUIStyle(GUI.skin.label);
        _titleStyle.fontSize = 16;
        _titleStyle.fontStyle = FontStyle.Bold;
        _titleStyle.normal.textColor = new Color(0.4f, 1f, 0.5f);

        _labelStyle = new GUIStyle(GUI.skin.label);
        _labelStyle.fontSize = 14;
        _labelStyle.normal.textColor = Color.white;

        _playerStyle = new GUIStyle(GUI.skin.label);
        _playerStyle.fontSize = 13;
        _playerStyle.normal.textColor = new Color(0.85f, 0.85f, 1f);
    }

    static Texture2D MakeTex(int w, int h, Color col)
    {
        var pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        var tex = new Texture2D(w, h);
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }
}
