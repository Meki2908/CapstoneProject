using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI thanh máu boss — hiện ở góc trên màn hình khi boss spawn.
/// Hỗ trợ nhiều boss cùng lúc (Demon phase 2 triệu hồi boss phụ).
/// HP đọc từ TakeDamageTest (hệ thống chính).
/// Tên boss đọc từ specificEnemyType.
/// 
/// === CHO ĐỒNG ĐỘI SỬA UI ===
/// Sửa trực tiếp method CreateBossEntry() — đổi vị trí, màu, font, size...
/// API giữ nguyên: ShowBossHealth() / HideBoss() / HideAll()
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    public static BossHealthBarUI Instance { get; private set; }
    
    private Canvas canvas;
    
    // Theo dõi nhiều boss cùng lúc
    private class BossEntry
    {
        public TakeDamageTest hpScript;
        public EnemyScript enemyScript;
        public GameObject panelGO;
        public Image healthBarFill;
        public Text nameText;
        public Text hpText;
        public float maxHP;
    }
    private List<BossEntry> activeBosses = new List<BossEntry>();
    
    private float smoothSpeed = 5f;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateCanvas();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    
    private void Update()
    {
        // Cập nhật từng boss health bar
        for (int i = activeBosses.Count - 1; i >= 0; i--)
        {
            var entry = activeBosses[i];
            
            // Boss bị destroy hoặc null
            if (entry.hpScript == null || entry.panelGO == null)
            {
                RemoveBossEntry(i);
                continue;
            }
            
            float currentHP = entry.hpScript.CurrentHealth;
            float maxHP = entry.maxHP;
            if (maxHP <= 0) maxHP = 1;
            
            float percent = Mathf.Clamp01(currentHP / maxHP);
            
            // Smooth fill — dùng anchorMax.x để co thanh từ phải sang trái
            if (entry.healthBarFill != null)
            {
                RectTransform fillRT = entry.healthBarFill.rectTransform;
                float currentFill = fillRT.anchorMax.x;
                float targetFill = Mathf.Lerp(currentFill, percent, Time.deltaTime * smoothSpeed);
                fillRT.anchorMax = new Vector2(targetFill, 1f);
                
                // Đổi màu: xanh → vàng → đỏ
                if (percent > 0.5f)
                    entry.healthBarFill.color = Color.Lerp(Color.yellow, new Color(0.2f, 0.8f, 0.2f), (percent - 0.5f) * 2f);
                else
                    entry.healthBarFill.color = Color.Lerp(Color.red, Color.yellow, percent * 2f);
            }
            
            // Text HP
            if (entry.hpText != null)
            {
                entry.hpText.text = $"{Mathf.Max(0, Mathf.CeilToInt(currentHP))} / {Mathf.CeilToInt(maxHP)}";
            }
            
            // Tự ẩn khi boss chết
            if (!entry.hpScript.IsAlive() || currentHP <= 0)
            {
                RemoveBossEntry(i);
            }
        }
    }
    
    // ==================== PUBLIC API ====================
    
    /// <summary>
    /// Hiện thanh máu cho 1 boss. Hỗ trợ nhiều boss cùng lúc.
    /// </summary>
    public void ShowBossHealth(TakeDamageTest hpScript)
    {
        if (hpScript == null) return;
        
        // Tránh duplicate
        foreach (var e in activeBosses)
        {
            if (e.hpScript == hpScript) return;
        }
        
        // Tìm EnemyScript để lấy tên
        var es = hpScript.GetComponent<EnemyScript>();
        if (es == null) es = hpScript.GetComponentInParent<EnemyScript>();
        
        // Ưu tiên: specificEnemyType → enemyName → BossName → gameObject.name
        string bossName = "";
        if (es != null)
        {
            // specificEnemyType cho ra tên đúng: "Ifrit", "Lich", "Demon", "Stoneogre"...
            bossName = es.specificEnemyType.ToString();
            // Nếu enemyName đã được set khác default "Enemy" → dùng nó
            if (!string.IsNullOrEmpty(es.enemyName) && es.enemyName != "Enemy")
                bossName = es.enemyName;
        }
        if (string.IsNullOrEmpty(bossName))
            bossName = hpScript.BossName;
        if (string.IsNullOrEmpty(bossName))
            bossName = hpScript.gameObject.name;
        
        float maxHP = hpScript.MaxHealth;
        if (maxHP <= 0) maxHP = hpScript.CurrentHealth;
        if (maxHP <= 0) maxHP = 100;
        
        // Tạo UI entry
        var entry = CreateBossEntry(bossName, activeBosses.Count);
        entry.hpScript = hpScript;
        entry.enemyScript = es;
        entry.maxHP = maxHP;
        
        if (entry.hpText != null)
            entry.hpText.text = $"{Mathf.CeilToInt(maxHP)} / {Mathf.CeilToInt(maxHP)}";
        
        activeBosses.Add(entry);
        
        Debug.Log($"[BossHealthBar] Showing: {bossName} (HP={maxHP}), total bars={activeBosses.Count}");
    }
    
    /// <summary>
    /// Overload — truyền EnemyScript, tự tìm TakeDamageTest
    /// </summary>
    public void ShowBossHealth(EnemyScript bossScript)
    {
        if (bossScript == null) return;
        var hp = bossScript.GetComponent<TakeDamageTest>();
        if (hp == null) hp = bossScript.GetComponentInChildren<TakeDamageTest>();
        if (hp != null) ShowBossHealth(hp);
    }
    
    /// <summary>
    /// Ẩn 1 boss cụ thể
    /// </summary>
    public void HideBoss(TakeDamageTest hpScript)
    {
        for (int i = activeBosses.Count - 1; i >= 0; i--)
        {
            if (activeBosses[i].hpScript == hpScript)
            {
                RemoveBossEntry(i);
                break;
            }
        }
    }
    
    /// <summary>
    /// Ẩn tất cả
    /// </summary>
    public void HideAll()
    {
        for (int i = activeBosses.Count - 1; i >= 0; i--)
        {
            RemoveBossEntry(i);
        }
    }
    
    /// <summary>
    /// Ẩn tất cả (backward compatible)
    /// </summary>
    public void Hide() => HideAll();
    
    public static BossHealthBarUI EnsureInstance()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("[BossHealthBarUI]");
        Instance = go.AddComponent<BossHealthBarUI>();
        return Instance;
    }
    
    // ==================== INTERNAL ====================
    
    private void RemoveBossEntry(int index)
    {
        if (index < 0 || index >= activeBosses.Count) return;
        var entry = activeBosses[index];
        if (entry.panelGO != null) Destroy(entry.panelGO);
        activeBosses.RemoveAt(index);
        
        // Cập nhật vị trí các bar còn lại
        for (int i = 0; i < activeBosses.Count; i++)
        {
            UpdateBarPosition(activeBosses[i].panelGO, i);
        }
    }
    
    private void UpdateBarPosition(GameObject panel, int index)
    {
        if (panel == null) return;
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt == null) return;
        
        // Stack từ trên xuống, mỗi bar cách nhau
        float topY = 0.97f - index * 0.08f;
        float botY = topY - 0.07f;
        rt.anchorMin = new Vector2(0.25f, botY);
        rt.anchorMax = new Vector2(0.75f, topY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
    
    // ==================== TẠO UI BẰNG CODE ====================
    
    private void CreateCanvas()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        gameObject.AddComponent<GraphicRaycaster>();
    }
    
    /// <summary>
    /// Tạo 1 health bar entry cho 1 boss.
    /// Đồng đội sửa method này để thay đổi giao diện.
    /// </summary>
    private BossEntry CreateBossEntry(string bossName, int index)
    {
        var entry = new BossEntry();
        
        // Panel
        GameObject panelGO = new GameObject($"BossBar_{bossName}");
        panelGO.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        entry.panelGO = panelGO;
        UpdateBarPosition(panelGO, index);
        
        Image panelBg = panelGO.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.65f);
        
        // Tên boss
        GameObject nameGO = new GameObject("BossName");
        nameGO.transform.SetParent(panelGO.transform, false);
        
        RectTransform nameRect = nameGO.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.55f);
        nameRect.anchorMax = new Vector2(1, 1f);
        nameRect.offsetMin = new Vector2(10, 0);
        nameRect.offsetMax = new Vector2(-10, -2);
        
        entry.nameText = nameGO.AddComponent<Text>();
        entry.nameText.text = bossName;
        entry.nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        entry.nameText.fontSize = 18;
        entry.nameText.color = Color.white;
        entry.nameText.alignment = TextAnchor.MiddleCenter;
        entry.nameText.fontStyle = FontStyle.Bold;
        
        // Health bar bg
        GameObject hpBgGO = new GameObject("HealthBarBg");
        hpBgGO.transform.SetParent(panelGO.transform, false);
        
        RectTransform hpBgRect = hpBgGO.AddComponent<RectTransform>();
        hpBgRect.anchorMin = new Vector2(0.02f, 0.08f);
        hpBgRect.anchorMax = new Vector2(0.98f, 0.52f);
        hpBgRect.offsetMin = Vector2.zero;
        hpBgRect.offsetMax = Vector2.zero;
        
        Image hpBg = hpBgGO.AddComponent<Image>();
        hpBg.color = new Color(0.15f, 0.05f, 0.05f, 0.9f);
        
        // Health bar fill
        GameObject hpFillGO = new GameObject("HealthBarFill");
        hpFillGO.transform.SetParent(hpBgGO.transform, false);
        
        RectTransform hpFillRect = hpFillGO.AddComponent<RectTransform>();
        hpFillRect.anchorMin = Vector2.zero;
        hpFillRect.anchorMax = Vector2.one;
        hpFillRect.offsetMin = Vector2.zero;
        hpFillRect.offsetMax = Vector2.zero;
        
        entry.healthBarFill = hpFillGO.AddComponent<Image>();
        entry.healthBarFill.color = new Color(0.2f, 0.8f, 0.2f);
        entry.healthBarFill.type = Image.Type.Simple;
        
        // HP text
        GameObject hpTextGO = new GameObject("HealthText");
        hpTextGO.transform.SetParent(hpBgGO.transform, false);
        
        RectTransform hpTextRect = hpTextGO.AddComponent<RectTransform>();
        hpTextRect.anchorMin = Vector2.zero;
        hpTextRect.anchorMax = Vector2.one;
        hpTextRect.offsetMin = Vector2.zero;
        hpTextRect.offsetMax = Vector2.zero;
        
        entry.hpText = hpTextGO.AddComponent<Text>();
        entry.hpText.text = "";
        entry.hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        entry.hpText.fontSize = 14;
        entry.hpText.color = Color.white;
        entry.hpText.alignment = TextAnchor.MiddleCenter;
        
        return entry;
    }
}
