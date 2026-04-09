using UnityEngine;

/// <summary>
/// Debug tool: test Boss HP Bar nhanh mà không cần chơi tới wave 5.
/// Gắn vào bất kỳ GameObject nào trong scene (hoặc Player).
/// 
/// PHÍM TẮT:
///   F7 = Hiện HP bar cho boss đầu tiên tìm thấy trong scene
///   F8 = Hiện HP bar FAKE (không cần boss thật) — test UI thuần
///   F6 = Ẩn tất cả HP bar
/// 
/// XÓA SCRIPT NÀY SAU KHI TEST XONG.
/// </summary>
public class BossHPBarDebug : MonoBehaviour
{
    [Header("Fake Boss Settings (F8)")]
    public string fakeBossName = "TEST BOSS";
    public float fakeMaxHP = 10000f;
    
    private TakeDamageTest foundBoss;
    private bool fakeActive = false;
    private float fakeCurrentHP;
    
    // Fake UI references
    private GameObject fakeBarGO;
    
    void Update()
    {
        // F7: Tìm boss thật trong scene → hiện HP bar
        if (Input.GetKeyDown(KeyCode.F7))
        {
            FindAndShowRealBoss();
        }
        
        // F8: Hiện HP bar fake (test UI không cần boss)
        if (Input.GetKeyDown(KeyCode.F8))
        {
            if (!fakeActive)
                ShowFakeBossBar();
            else
                SimulateDamage(); // Bấm F8 lần nữa = giảm HP
        }
        
        // F6: Ẩn tất cả
        if (Input.GetKeyDown(KeyCode.F6))
        {
            HideAll();
        }
    }
    
    void FindAndShowRealBoss()
    {
        foreach (var es in FindObjectsByType<EnemyScript>(FindObjectsSortMode.None))
        {
            if (!es.isBoss) continue;
            
            var hp = es.GetComponent<TakeDamageTest>();
            if (hp == null) hp = es.GetComponentInChildren<TakeDamageTest>();
            if (hp == null) continue;
            
            BossHealthBarUI.EnsureInstance();
            BossHealthBarUI.Instance.ShowBossHealth(hp);
            foundBoss = hp;
            Debug.Log($"[BossHPBarDebug] F7: Showing HP bar for: {es.enemyName} (HP={hp.MaxHealth})");
            return;
        }
        Debug.LogWarning("[BossHPBarDebug] F7: Không tìm thấy boss trong scene!");
    }
    
    void ShowFakeBossBar()
    {
        fakeActive = true;
        fakeCurrentHP = fakeMaxHP;
        
        // Tạo fake TakeDamageTest để BossHealthBarUI đọc HP
        fakeBarGO = new GameObject($"[FakeBoss_{fakeBossName}]");
        var fakeHP = fakeBarGO.AddComponent<TakeDamageTest>();
        
        // Set HP values
        fakeHP.MaxHealth = fakeMaxHP;
        fakeHP.CurrentHealth = fakeMaxHP;
        
        var fakeES = fakeBarGO.AddComponent<EnemyScript>();
        fakeES.isBoss = true;
        fakeES.enemyName = fakeBossName;
        
        BossHealthBarUI.EnsureInstance();
        BossHealthBarUI.Instance.ShowBossHealth(fakeHP);
        
        Debug.Log($"[BossHPBarDebug] F8: Fake boss bar shown! Bấm F8 lần nữa để giảm HP. F6 để ẩn.");
    }
    
    void SimulateDamage()
    {
        if (fakeBarGO == null) return;
        var fakeHP = fakeBarGO.GetComponent<TakeDamageTest>();
        if (fakeHP == null) return;
        
        // Giảm 15% HP mỗi lần bấm
        float damage = fakeMaxHP * 0.15f;
        fakeCurrentHP = Mathf.Max(0, fakeCurrentHP - damage);
        fakeHP.CurrentHealth = fakeCurrentHP;
        
        Debug.Log($"[BossHPBarDebug] F8: Damage! HP = {fakeCurrentHP}/{fakeMaxHP} ({Mathf.RoundToInt(fakeCurrentHP/fakeMaxHP*100)}%)");
        
        if (fakeCurrentHP <= 0)
        {
            Debug.Log("[BossHPBarDebug] Fake boss died! Bấm F8 để tạo mới.");
            fakeActive = false;
        }
    }
    
    void HideAll()
    {
        if (BossHealthBarUI.Instance != null)
            BossHealthBarUI.Instance.HideAll();
        
        if (fakeBarGO != null)
        {
            Destroy(fakeBarGO);
            fakeBarGO = null;
        }
        
        fakeActive = false;
        Debug.Log("[BossHPBarDebug] F6: All boss HP bars hidden.");
    }
    
    void OnDestroy()
    {
        if (fakeBarGO != null)
            Destroy(fakeBarGO);
    }
}
