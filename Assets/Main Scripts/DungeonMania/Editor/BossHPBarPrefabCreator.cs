#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor tool: tạo Boss HP Bar prefab.
/// Menu: Tools → Create Boss HP Bar Prefab
/// 
/// CẤU TRÚC MỚI:
/// - BarFillFrame: sprite thanh đỏ có khung → LUÔN FULL SIZE (trang trí)
/// - BarFill: Image MÀU ĐỎ ĐƠN GIẢN bên trong → bị mask clip khi mất máu
/// - Khi HP giảm: BarFill bị che → lộ nền tối → BarFillFrame vẫn nguyên khung
/// </summary>
public class BossHPBarPrefabCreator
{
    [MenuItem("Tools/Create Boss HP Bar Prefab (V2)")]
    public static void CreatePrefab()
    {
        GameObject root = new GameObject("BossHealthBar");
        RectTransform rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(700, 90);
        
        // ================================================================
        // BAR CONTAINER — chứa mọi thứ liên quan thanh máu
        // ================================================================
        GameObject barContainer = CreateChild(root, "BarContainer");
        RectTransform containerRT = barContainer.GetComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0.06f, 0.02f);
        containerRT.anchorMax = new Vector2(0.94f, 0.55f);
        StretchFill(containerRT);
        
        // --- Nền tối (hiện ra khi HP giảm) ---
        GameObject darkBg = CreateChild(barContainer, "DarkBackground");
        Image darkBgImg = darkBg.AddComponent<Image>();
        darkBgImg.color = new Color(0.08f, 0.04f, 0.04f, 0.95f);
        darkBgImg.raycastTarget = false;
        RectTransform darkBgRT = darkBg.GetComponent<RectTransform>();
        darkBgRT.anchorMin = new Vector2(0.03f, 0.1f);
        darkBgRT.anchorMax = new Vector2(0.97f, 0.9f);
        StretchFill(darkBgRT);
        
        // --- Trail Mask (delayed damage — đỏ tối, bị mask) ---
        GameObject trailMask = CreateChild(barContainer, "TrailMask");
        trailMask.AddComponent<RectMask2D>();
        RectTransform trailMaskRT = trailMask.GetComponent<RectTransform>();
        trailMaskRT.anchorMin = new Vector2(0.03f, 0.1f);
        trailMaskRT.anchorMax = new Vector2(0.97f, 0.9f);
        StretchFill(trailMaskRT);
        
        GameObject trailBar = CreateChild(trailMask, "BarTrail");
        Image trailImg = trailBar.AddComponent<Image>();
        trailImg.color = new Color(0.6f, 0.08f, 0.04f, 0.9f);
        trailImg.raycastTarget = false;
        RectTransform trailRT = trailBar.GetComponent<RectTransform>();
        trailRT.anchorMin = Vector2.zero;
        trailRT.anchorMax = Vector2.one;
        StretchFill(trailRT);
        
        // --- Fill Mask (thanh HP chính — bị mask clip) ---
        // ⭐ Script sẽ co cái FillMask này → clip BarFill bên trong
        GameObject fillMask = CreateChild(barContainer, "FillMask");
        fillMask.AddComponent<RectMask2D>();
        RectTransform fillMaskRT = fillMask.GetComponent<RectTransform>();
        fillMaskRT.anchorMin = new Vector2(0.03f, 0.1f);
        fillMaskRT.anchorMax = new Vector2(0.97f, 0.9f);
        StretchFill(fillMaskRT);
        
        // BarFill — Image MÀU ĐỎ ĐƠN GIẢN (hoặc gradient)
        // Script tìm "BarFill" → parent là FillMask → co FillMask để clip
        GameObject fillBar = CreateChild(fillMask, "BarFill");
        Image fillImg = fillBar.AddComponent<Image>();
        fillImg.color = new Color(0.85f, 0.12f, 0.08f);  // Đỏ đơn giản
        fillImg.raycastTarget = false;
        RectTransform fillRT = fillBar.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        StretchFill(fillRT);
        
        // --- Flash Overlay ---
        GameObject flash = CreateChild(fillMask, "FlashOverlay");
        Image flashImg = flash.AddComponent<Image>();
        flashImg.color = new Color(1, 1, 1, 0);
        flashImg.raycastTarget = false;
        RectTransform flashRT = flash.GetComponent<RectTransform>();
        flashRT.anchorMin = Vector2.zero;
        flashRT.anchorMax = Vector2.one;
        StretchFill(flashRT);
        
        // --- BarFillFrame: sprite thanh đỏ CÓ KHUNG → LUÔN FULL, trang trí ---
        // Kéo sprite "thanh đỏ có khung" vào đây
        // Nó nằm TRÊN fill nên khung luôn hiện, phần đỏ bên dưới bị che/hiện bởi BarFill
        GameObject fillFrame = CreateChild(barContainer, "BarFillFrame");
        Image fillFrameImg = fillFrame.AddComponent<Image>();
        fillFrameImg.color = Color.white; // ⭐ KÉO SPRITE VÀO ĐÂY
        fillFrameImg.raycastTarget = false;
        RectTransform fillFrameRT = fillFrame.GetComponent<RectTransform>();
        fillFrameRT.anchorMin = Vector2.zero;
        fillFrameRT.anchorMax = Vector2.one;
        StretchFill(fillFrameRT);
        
        // ================================================================
        // DECORATIONS
        // ================================================================
        
        // Skull Icon (góc trái)
        GameObject skull = CreateChild(root, "SkullIcon");
        Image skullImg = skull.AddComponent<Image>();
        skullImg.color = Color.white;
        skullImg.raycastTarget = false;
        skullImg.preserveAspect = true;
        RectTransform skullRT = skull.GetComponent<RectTransform>();
        skullRT.anchorMin = new Vector2(0f, 0.1f);
        skullRT.anchorMax = new Vector2(0.18f, 1.3f);
        StretchFill(skullRT);
        
        // Left Wing
        GameObject leftWing = CreateChild(root, "LeftWingDeco");
        Image leftImg = leftWing.AddComponent<Image>();
        leftImg.color = Color.white;
        leftImg.raycastTarget = false;
        leftImg.preserveAspect = true;
        RectTransform leftRT = leftWing.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0.02f, -0.15f);
        leftRT.anchorMax = new Vector2(0.15f, 0.35f);
        StretchFill(leftRT);
        
        // Right Wing
        GameObject rightWing = CreateChild(root, "RightWingDeco");
        Image rightImg = rightWing.AddComponent<Image>();
        rightImg.color = Color.white;
        rightImg.raycastTarget = false;
        rightImg.preserveAspect = true;
        RectTransform rightRT = rightWing.GetComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(0.85f, -0.15f);
        rightRT.anchorMax = new Vector2(0.98f, 0.35f);
        StretchFill(rightRT);
        
        // ================================================================
        // TEXT
        // ================================================================
        
        // Boss Name
        GameObject nameObj = CreateChild(root, "BossName");
        TextMeshProUGUI nameTMP = nameObj.AddComponent<TextMeshProUGUI>();
        nameTMP.text = "BOSS NAME";
        nameTMP.fontSize = 22;
        nameTMP.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        nameTMP.color = new Color(1f, 0.85f, 0.55f);
        nameTMP.alignment = TextAlignmentOptions.Center;
        nameTMP.outlineWidth = 0.2f;
        nameTMP.outlineColor = new Color32(0, 0, 0, 200);
        nameTMP.raycastTarget = false;
        RectTransform nameRT = nameObj.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0.1f, 0.55f);
        nameRT.anchorMax = new Vector2(0.9f, 1f);
        StretchFill(nameRT);
        
        // HP Text
        GameObject hpTextObj = CreateChild(root, "HPText");
        TextMeshProUGUI hpTMP = hpTextObj.AddComponent<TextMeshProUGUI>();
        hpTMP.text = "1000 / 1000";
        hpTMP.fontSize = 14;
        hpTMP.fontStyle = FontStyles.Bold;
        hpTMP.color = Color.white;
        hpTMP.alignment = TextAlignmentOptions.Center;
        hpTMP.outlineWidth = 0.15f;
        hpTMP.outlineColor = new Color32(0, 0, 0, 180);
        hpTMP.raycastTarget = false;
        RectTransform hpRT = hpTextObj.GetComponent<RectTransform>();
        hpRT.anchorMin = new Vector2(0.1f, 0.05f);
        hpRT.anchorMax = new Vector2(0.9f, 0.52f);
        StretchFill(hpRT);
        
        // ================================================================
        // SAVE
        // ================================================================
        string prefabPath = "Assets/ASSETS/UI/BossHPBar/BossHealthBar.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        
        Debug.Log($"[BossHPBar] Prefab V2 created at: {prefabPath}");
        Debug.Log("[BossHPBar] === HƯỚNG DẪN GÁN SPRITES ===");
        Debug.Log("  BarFillFrame → Sprite thanh đỏ CÓ KHUNG (trang trí, luôn full)");
        Debug.Log("  BarFill → KHÔNG cần sprite (đã có màu đỏ sẵn)");
        Debug.Log("  SkullIcon → Sprite đầu lâu lớn");
        Debug.Log("  LeftWingDeco → Sprite cánh trái");
        Debug.Log("  RightWingDeco → Sprite cánh phải");
    }
    
    private static GameObject CreateChild(GameObject parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        return go;
    }
    
    private static void StretchFill(RectTransform rt)
    {
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
