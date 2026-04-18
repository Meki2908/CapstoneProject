using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

/// <summary>
/// UI thanh máu boss — dùng PREFAB thay vì tạo bằng code.
/// 
/// === SETUP ===
/// 1. Chạy menu: Tools → Create Boss HP Bar Prefab
/// 2. Import sprite sheet → set Multiple → Sprite Editor → Slice
/// 3. Mở prefab, kéo sprites vào các Image
/// 4. Gán prefab vào field "healthBarPrefab" trên script
/// 
/// === CẤU TRÚC PREFAB ===
/// BossHealthBar (root RectTransform)
/// ├── PanelBackground (Image)
/// ├── BarFrame (Image: sprite frame tối)
/// │   └── FillArea
/// │       ├── BarTrail (Image: filled, đỏ tối)
/// │       ├── BarFill (Image: filled, sprite đỏ)
/// │       └── FlashOverlay (Image: trắng, alpha=0)
/// ├── SkullIcon (Image: đầu lâu)
/// ├── LeftWingDeco (Image: cánh trái)
/// ├── RightWingDeco (Image: cánh phải)
/// ├── BossName (TextMeshProUGUI)
/// └── HPText (TextMeshProUGUI)
/// 
/// API giữ nguyên: ShowBossHealth() / HideBoss() / HideAll()
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    public static BossHealthBarUI Instance { get; private set; }
    
    [Header("=== PREFAB ===")]
    [Tooltip("Kéo prefab BossHealthBar vào đây")]
    public GameObject healthBarPrefab;
    
    private Canvas canvas;
    
    private class BossEntry
    {
        public TakeDamageTest hpScript;
        public EnemyScript enemyScript;
        public GameObject panelGO;
        // UI references
        public Image barFill;
        public Image barTrail;
        public Image flashOverlay;
        public RectTransform fillMaskRT;   // FillArea — co cái này để mask BarFill
        public RectTransform trailMaskRT;  // TrailArea — co cái này để mask BarTrail
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;
        // State
        public float maxHP;
        public float lastHP;
        public float trailTarget;
        public float flashAlpha;
        public float fillMaskMaxX;   // anchorMax.x ban đầu của FillArea
        public float fillMaskMinX;   // anchorMin.x ban đầu của FillArea
        public bool isFadingOut;     // Prevent duplicate FadeOutAndRemove coroutines
    }
    
    private List<BossEntry> activeBosses = new List<BossEntry>();
    
    [Header("=== ANIMATION ===")]
    public float smoothSpeed = 6f;
    public float trailDelay = 0.5f;
    public float trailSpeed = 2.5f;
    public float flashDuration = 0.12f;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureCanvas();
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
        for (int i = activeBosses.Count - 1; i >= 0; i--)
        {
            var entry = activeBosses[i];
            
            if (entry.hpScript == null || entry.panelGO == null)
            {
                RemoveBossEntry(i);
                continue;
            }
            
            float currentHP = entry.hpScript.CurrentHealth;
            float maxHP = entry.maxHP;
            if (maxHP <= 0) maxHP = 1;
            float percent = Mathf.Clamp01(currentHP / maxHP);
            
            // === DETECT DAMAGE → flash + trail delay ===
            if (currentHP < entry.lastHP - 0.5f)
            {
                entry.flashAlpha = 1f;
                StartCoroutine(DelayedTrailUpdate(entry, percent));
            }
            entry.lastHP = currentHP;
            
            // === MAIN BAR (mask clip — co FillArea, sprite giữ nguyên) ===
            if (entry.fillMaskRT != null)
            {
                float currentX = entry.fillMaskRT.anchorMax.x;
                float targetX = Mathf.Lerp(entry.fillMaskMinX, entry.fillMaskMaxX, percent);
                float smoothX = Mathf.Lerp(currentX, targetX, Time.deltaTime * smoothSpeed);
                entry.fillMaskRT.anchorMax = new Vector2(smoothX, entry.fillMaskRT.anchorMax.y);
            }
            
            // === TRAIL BAR (delayed, cũng mask) ===
            if (entry.trailMaskRT != null)
            {
                float currentX = entry.trailMaskRT.anchorMax.x;
                float targetX = Mathf.Lerp(entry.fillMaskMinX, entry.fillMaskMaxX, entry.trailTarget);
                float smoothX = Mathf.Lerp(currentX, targetX, Time.deltaTime * trailSpeed);
                entry.trailMaskRT.anchorMax = new Vector2(smoothX, entry.trailMaskRT.anchorMax.y);
            }
            
            // === FLASH ===
            if (entry.flashOverlay != null)
            {
                entry.flashAlpha = Mathf.Max(0, entry.flashAlpha - Time.deltaTime / flashDuration);
                entry.flashOverlay.color = new Color(1, 1, 1, entry.flashAlpha * 0.5f);
            }
            
            // === TEXT ===
            if (entry.hpText != null)
            {
                entry.hpText.text = $"{Mathf.Max(0, Mathf.CeilToInt(currentHP))} / {Mathf.CeilToInt(maxHP)}";
            }
            
            // === AUTO HIDE khi chết ===
            if (!entry.isFadingOut && (!entry.hpScript.IsAlive() || currentHP <= 0))
            {
                entry.isFadingOut = true;
                StartCoroutine(FadeOutAndRemove(i));
            }
        }
    }
    
    private IEnumerator DelayedTrailUpdate(BossEntry entry, float newPercent)
    {
        yield return new WaitForSeconds(trailDelay);
        if (entry != null)
            entry.trailTarget = newPercent;
    }
    
    private IEnumerator FadeOutAndRemove(int index)
    {
        if (index < 0 || index >= activeBosses.Count) yield break;
        var entry = activeBosses[index];
        if (entry.panelGO == null) { RemoveBossEntry(index); yield break; }
        
        // Fade out 0.5s
        CanvasGroup cg = entry.panelGO.GetComponent<CanvasGroup>();
        if (cg == null) cg = entry.panelGO.AddComponent<CanvasGroup>();
        
        float t = 0;
        while (t < 0.5f)
        {
            // Guard: panelGO or CanvasGroup may have been destroyed externally
            if (entry.panelGO == null || cg == null) yield break;
            t += Time.deltaTime;
            cg.alpha = 1f - (t / 0.5f);
            yield return null;
        }
        
        // Re-find index because list may have shifted during the fade
        int currentIndex = activeBosses.IndexOf(entry);
        if (currentIndex >= 0)
            RemoveBossEntry(currentIndex);
    }
    
    // ==================== PUBLIC API ====================
    
    public void ShowBossHealth(TakeDamageTest hpScript)
    {
        if (hpScript == null) return;
        
        // Tránh duplicate
        foreach (var e in activeBosses)
        {
            if (e.hpScript == hpScript) return;
        }
        
        // ưu tiên 1: BossName từ TakeDamageTest (tên đầy đủ, kể cả biệt danh)
        string bossName = hpScript.BossName;

        // ưu tiên 2 (fallback): lấy tên từ EnemyScript chỉ khi BossName để trống
        if (string.IsNullOrEmpty(bossName))
        {
            var es = hpScript.GetComponent<EnemyScript>();
            if (es == null) es = hpScript.GetComponentInParent<EnemyScript>();
            if (es != null)
            {
                // enemyName ưu tiên hơn specificEnemyType
                if (!string.IsNullOrEmpty(es.enemyName) && es.enemyName != "Enemy")
                    bossName = es.enemyName;
                else
                    bossName = es.specificEnemyType.ToString();
            }
        }

        // ưu tiên 3 (fallback cuối): tên GameObject
        if (string.IsNullOrEmpty(bossName))
            bossName = hpScript.gameObject.name;

        // Lấy EnemyScript cho entry (chỉ dùng để track HP, không dùng lấy tên)
        var enemyScript = hpScript.GetComponent<EnemyScript>();
        if (enemyScript == null) enemyScript = hpScript.GetComponentInParent<EnemyScript>();
        
        float maxHP = hpScript.MaxHealth;
        if (maxHP <= 0) maxHP = hpScript.CurrentHealth;
        if (maxHP <= 0) maxHP = 100;
        
        // Tạo entry từ prefab
        var entry = CreateBossEntry(bossName, activeBosses.Count);
        entry.hpScript = hpScript;
        entry.enemyScript = enemyScript;
        entry.maxHP = maxHP;
        entry.lastHP = hpScript.CurrentHealth;
        entry.trailTarget = 1f;
        
        if (entry.hpText != null)
            entry.hpText.text = $"{Mathf.CeilToInt(maxHP)} / {Mathf.CeilToInt(maxHP)}";
        
        activeBosses.Add(entry);
        Debug.Log($"[BossHealthBar] Showing: {bossName} (HP={maxHP}), total={activeBosses.Count}");
    }
    
    public void ShowBossHealth(EnemyScript bossScript)
    {
        if (bossScript == null) return;
        var hp = bossScript.GetComponent<TakeDamageTest>();
        if (hp == null) hp = bossScript.GetComponentInChildren<TakeDamageTest>();
        if (hp != null) ShowBossHealth(hp);
    }
    
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
    
    public void HideAll()
    {
        for (int i = activeBosses.Count - 1; i >= 0; i--)
            RemoveBossEntry(i);
    }
    
    public void Hide() => HideAll();
    
    public static BossHealthBarUI EnsureInstance()
    {
        if (Instance != null) return Instance;
        
        // Tìm trong scene trước
        Instance = FindAnyObjectByType<BossHealthBarUI>();
        if (Instance != null) return Instance;
        
        // Tạo mới + tự load prefab từ Resources
        var go = new GameObject("[BossHealthBarUI]");
        Instance = go.AddComponent<BossHealthBarUI>();
        
        // Auto-load prefab: đặt prefab trong Assets/Resources/BossHealthBar
        var prefab = Resources.Load<GameObject>("BossHealthBar");
        if (prefab != null)
        {
            Instance.healthBarPrefab = prefab;
            Debug.Log("[BossHealthBarUI] Auto-loaded prefab from Resources/BossHealthBar");
        }
        else
        {
            Debug.LogWarning("[BossHealthBarUI] Prefab not found in Resources/BossHealthBar — using fallback code bar");
        }
        
        return Instance;
    }
    
    // ==================== INTERNAL ====================
    
    private void EnsureCanvas()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }
    
    private BossEntry CreateBossEntry(string bossName, int index)
    {
        var entry = new BossEntry();
        
        GameObject panelGO;
        
        if (healthBarPrefab != null)
        {
            // === DÙNG PREFAB ===
            panelGO = Instantiate(healthBarPrefab, canvas.transform, false);
            panelGO.name = $"BossBar_{bossName}";
            
            // Tìm components từ hierarchy prefab
            entry.barFill = FindImage(panelGO, "BarFill");
            entry.barTrail = FindImage(panelGO, "BarTrail");
            entry.flashOverlay = FindImage(panelGO, "FlashOverlay");
            entry.nameText = FindTMP(panelGO, "BossName");
            entry.hpText = FindTMP(panelGO, "HPText");
        }
        else
        {
            // === FALLBACK: tạo bằng code (basic) ===
            panelGO = CreateFallbackBar(bossName, ref entry);
        }
        
        entry.panelGO = panelGO;
        UpdateBarPosition(panelGO, index);
        
        // Set tên boss
        if (entry.nameText != null)
            entry.nameText.text = bossName.ToUpper();
        
        // Init: setup mask system (V2 — tìm FillMask/TrailMask trực tiếp)
        // Prefab V2 có sẵn FillMask và TrailMask với RectMask2D
        entry.fillMaskRT = FindRT(panelGO, "FillMask");
        entry.trailMaskRT = FindRT(panelGO, "TrailMask");
        
        // Fallback: nếu dùng prefab cũ, tìm parent của BarFill
        if (entry.fillMaskRT == null && entry.barFill != null)
        {
            Transform fillParent = entry.barFill.transform.parent;
            if (fillParent != null)
            {
                entry.fillMaskRT = fillParent.GetComponent<RectTransform>();
                if (fillParent.GetComponent<RectMask2D>() == null)
                    fillParent.gameObject.AddComponent<RectMask2D>();
            }
        }
        
        // Lưu anchor ban đầu
        if (entry.fillMaskRT != null)
        {
            entry.fillMaskMaxX = entry.fillMaskRT.anchorMax.x;
            entry.fillMaskMinX = entry.fillMaskRT.anchorMin.x;
        }
        if (entry.trailMaskRT != null)
        {
            // Trail dùng cùng range anchor như Fill
            // (đã setup giống nhau trong prefab)
        }
        
        // BarFill/BarTrail: chỉ set type, KHÔNG override anchor/offset
        // → Bạn tự chỉnh kích thước trong prefab, script tôn trọng
        if (entry.barFill != null)
        {
            entry.barFill.type = Image.Type.Simple;
            entry.barFill.preserveAspect = false;
        }
        if (entry.barTrail != null)
        {
            entry.barTrail.type = Image.Type.Simple;
            entry.barTrail.preserveAspect = false;
        }
        
        return entry;
    }
    
    private void RemoveBossEntry(int index)
    {
        if (index < 0 || index >= activeBosses.Count) return;
        var entry = activeBosses[index];
        if (entry.panelGO != null) Destroy(entry.panelGO);
        activeBosses.RemoveAt(index);
        
        for (int i = 0; i < activeBosses.Count; i++)
            UpdateBarPosition(activeBosses[i].panelGO, i);
    }
    
    private void UpdateBarPosition(GameObject panel, int index)
    {
        if (panel == null) return;
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt == null) return;
        
        // Giữa trên, stack xuống dưới
        float topY = 0.96f - index * 0.10f;
        float botY = topY - 0.09f;
        rt.anchorMin = new Vector2(0.18f, botY);
        rt.anchorMax = new Vector2(0.82f, topY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
    
    // === HELPERS: tìm component trong hierarchy prefab ===
    
    private Image FindImage(GameObject root, string name)
    {
        var all = root.GetComponentsInChildren<Image>(true);
        foreach (var img in all)
        {
            if (img.gameObject.name == name) return img;
        }
        return null;
    }
    
    private RectTransform FindRT(GameObject root, string name)
    {
        var all = root.GetComponentsInChildren<RectTransform>(true);
        foreach (var rt in all)
        {
            if (rt.gameObject.name == name) return rt;
        }
        return null;
    }
    
    private TextMeshProUGUI FindTMP(GameObject root, string name)
    {
        var all = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in all)
        {
            if (tmp.gameObject.name == name) return tmp;
        }
        // Fallback: tìm legacy Text
        return null;
    }
    
    // === FALLBACK: tạo bar đơn giản nếu không có prefab ===
    private GameObject CreateFallbackBar(string bossName, ref BossEntry entry)
    {
        GameObject panel = new GameObject($"BossBar_{bossName}");
        panel.transform.SetParent(canvas.transform, false);
        panel.AddComponent<RectTransform>();
        
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.03f, 0.08f, 0.8f);
        
        // Fill area
        GameObject fillArea = new GameObject("FillArea");
        fillArea.transform.SetParent(panel.transform, false);
        RectTransform faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0.05f, 0.1f);
        faRT.anchorMax = new Vector2(0.95f, 0.5f);
        faRT.offsetMin = Vector2.zero;
        faRT.offsetMax = Vector2.zero;
        
        Image faBg = fillArea.AddComponent<Image>();
        faBg.color = new Color(0.1f, 0.05f, 0.05f, 0.9f);
        
        // Trail
        GameObject trail = new GameObject("BarTrail");
        trail.transform.SetParent(fillArea.transform, false);
        RectTransform tRT = trail.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = Vector2.zero; tRT.offsetMax = Vector2.zero;
        entry.barTrail = trail.AddComponent<Image>();
        entry.barTrail.color = new Color(0.7f, 0.15f, 0.1f, 0.85f);
        entry.barTrail.type = Image.Type.Filled;
        entry.barTrail.fillMethod = Image.FillMethod.Horizontal;
        
        // Fill
        GameObject fill = new GameObject("BarFill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fRT = fill.AddComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
        fRT.offsetMin = Vector2.zero; fRT.offsetMax = Vector2.zero;
        entry.barFill = fill.AddComponent<Image>();
        entry.barFill.color = new Color(0.85f, 0.15f, 0.1f);
        entry.barFill.type = Image.Type.Filled;
        entry.barFill.fillMethod = Image.FillMethod.Horizontal;
        
        // Flash
        GameObject flash = new GameObject("FlashOverlay");
        flash.transform.SetParent(fillArea.transform, false);
        RectTransform flRT = flash.AddComponent<RectTransform>();
        flRT.anchorMin = Vector2.zero; flRT.anchorMax = Vector2.one;
        flRT.offsetMin = Vector2.zero; flRT.offsetMax = Vector2.zero;
        entry.flashOverlay = flash.AddComponent<Image>();
        entry.flashOverlay.color = new Color(1, 1, 1, 0);
        
        // Name (TMP)
        GameObject nameGO = new GameObject("BossName");
        nameGO.transform.SetParent(panel.transform, false);
        RectTransform nRT = nameGO.AddComponent<RectTransform>();
        nRT.anchorMin = new Vector2(0.05f, 0.5f);
        nRT.anchorMax = new Vector2(0.95f, 1f);
        nRT.offsetMin = Vector2.zero; nRT.offsetMax = Vector2.zero;
        entry.nameText = nameGO.AddComponent<TextMeshProUGUI>();
        entry.nameText.fontSize = 20;
        entry.nameText.fontStyle = FontStyles.Bold;
        entry.nameText.color = new Color(1f, 0.85f, 0.55f);
        entry.nameText.alignment = TextAlignmentOptions.Center;
        
        // HP text
        GameObject hpGO = new GameObject("HPText");
        hpGO.transform.SetParent(fillArea.transform, false);
        RectTransform hRT = hpGO.AddComponent<RectTransform>();
        hRT.anchorMin = Vector2.zero; hRT.anchorMax = Vector2.one;
        hRT.offsetMin = Vector2.zero; hRT.offsetMax = Vector2.zero;
        entry.hpText = hpGO.AddComponent<TextMeshProUGUI>();
        entry.hpText.fontSize = 14;
        entry.hpText.fontStyle = FontStyles.Bold;
        entry.hpText.color = Color.white;
        entry.hpText.alignment = TextAlignmentOptions.Center;
        
        return panel;
    }
}
