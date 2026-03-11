using UnityEngine;
using System; // thêm để dùng Action

[RequireComponent(typeof(Animator))]
public class WeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handHolder;
    [SerializeField] private Transform sheathHolder;
    [SerializeField] private EquipmentSystem equipmentSystem; // chỉ dùng cho damage hooks nếu cần

    [Header("Animator Layers (indices)")]
    [Tooltip("Base=0, Sword=1, Axe=2, Mage=3, Arms=5 (adjust to your Animator)")]
    [SerializeField] private int baseLayer = 0;
    [SerializeField] private int swordLayer = 1;
    [SerializeField] private int axeLayer = 2;
    [SerializeField] private int mageLayer = 3;
    [SerializeField] private int armsLayer = 5;

    [Header("Animator Parameters")]
    [SerializeField] private string weaponTypeParam = "weaponType"; // int
    [SerializeField] private string speedParam = "speed";           // float
    [SerializeField] private string drawTrigger = "drawWeapon";     // trigger
    [SerializeField] private string sheathTrigger = "sheathWeapon"; // trigger

    [Header("Runtime")]
    [SerializeField] private WeaponSO currentWeapon; // assigned at runtime or in inspector

    [Header("Ability Management")]
    [SerializeField] private WeaponAbilityManager abilityManager;

    // THÊM: Sự kiện bắn ra khi đổi vũ khí
    public event Action<WeaponSO> OnWeaponChanged;

    private Animator animator;
    private GameObject currentHeldInstance;
    private GameObject currentSheathInstance;
    private Coroutine wandScaleRoutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        // Auto-find WeaponAbilityManager if not assigned
        if (abilityManager == null)
        {
            abilityManager = GetComponentInChildren<WeaponAbilityManager>();
            if (abilityManager == null)
            {
                Debug.LogWarning("[WeaponController] No WeaponAbilityManager found! Ability icons will not work.");
            }
            else
            {
                Debug.Log("[WeaponController] Auto-found WeaponAbilityManager");
            }
        }
    }

    private void Start()
    {
        ApplyWeaponLayersAndParams();
        // Áp đúng trạng thái ban đầu: Wand không dùng sheathHolder
        if (IsCurrentWand())
        {
            EnsureWandInstance();
            SetWandActive(false); // sheath = ẩn
        }
        else
        {
            ShowWeaponInSheath(); // Áp đúng sheathSocket của SO tại thời điểm khởi động
        }

        // Bật/tắt script theo weapon type hiện tại
        //RefreshWeaponScripts();

        // BẮN sự kiện 1 lần khi start nếu currentWeapon có sẵn
        OnWeaponChanged?.Invoke(currentWeapon);
        SyncWithEquipmentSystem();
        // WeaponHitRunner removed - effects handled by separate scripts
    }

    // THÊM: API set weapon từ pickup (có thể dùng chung)
    public void SetCurrentWeapon(WeaponSO weapon)
    {
        EquipWeapon(weapon);
    }

    public void EquipWeapon(WeaponSO weapon)
    {
        currentWeapon = weapon;
        ApplyWeaponLayersAndParams();

        // Reset visuals theo sheath
        ClearHeld();
        ClearSheath();
        if (IsCurrentWand())
        {
            EnsureWandInstance();
            SetWandActive(false); // sheath = ẩn
        }
        else
        {
            ShowWeaponInSheath();
        }

        // Auto-bind và sync
        // WeaponHitRunner removed - effects handled by separate scripts
        SyncWithEquipmentSystem();

        // Bật/tắt script theo weapon type mới
        RefreshWeaponScripts();

        // BẮN sự kiện cho tất cả consumer (Skills/HitRunner/UIs...)
        OnWeaponChanged?.Invoke(currentWeapon);
    }

    public WeaponSO GetCurrentWeapon() => currentWeapon;

    public void DrawWeaponVisual() // gọi từ Animation/State
    {
        animator.ResetTrigger(sheathTrigger);
        animator.SetTrigger(drawTrigger);
        if (IsCurrentWand())
        {
            // Wand always under handHolder; reuse instance and toggle active
            EnsureWandInstance();
            SetWandActive(true);
            StartWandDrawTween();
        }
        else
        {
            ClearHeld();
            if (currentSheathInstance) Destroy(currentSheathInstance);

            if (currentWeapon != null && currentWeapon.weaponPrefab && handHolder)
            {
                currentHeldInstance = Instantiate(currentWeapon.weaponPrefab, handHolder, false);
                ApplySocket(currentHeldInstance.transform, currentWeapon.handSocket);

                // Bind Aura (nếu có)
                var auraCtrl = GetComponent<WeaponAuraController>();
                if (auraCtrl != null) auraCtrl.BindAuraFrom(currentHeldInstance.transform, "Aura");

                // Auto-bind WeaponHitRunner nếu có
                // WeaponHitRunner removed - effects handled by separate scripts

                // BẮN sự kiện vì instance thay đổi (nếu consumer cần rebind theo instance)
                OnWeaponChanged?.Invoke(currentWeapon);
            }
        }
    }

    public void SheathWeaponVisual() // gọi từ Animation/State
    {
        animator.ResetTrigger(drawTrigger);
        animator.SetTrigger(sheathTrigger);
        // Tắt Aura và unbind
        var auraCtrl = GetComponent<WeaponAuraController>();
        if (auraCtrl != null) { auraCtrl.AE_AuraOff(); auraCtrl.UnbindAura(); }

        if (IsCurrentWand())
        {
            EnsureWandInstance();
            StartWandSheathTween();
            // Không tạo sheath instance cho Wand; chỉ ẩn
        }
        else
        {
            ClearSheath();
            if (currentHeldInstance) { Destroy(currentHeldInstance); currentHeldInstance = null; }
            ShowWeaponInSheath();
        }

        // BẮN sự kiện nếu consumer quan tâm trạng thái sheath
        OnWeaponChanged?.Invoke(currentWeapon);
    }

    private void ShowWeaponInSheath()
    {
        // Wand KHÔNG dùng sheathHolder, chỉ ẩn/hiện dưới handHolder
        if (IsCurrentWand()) return;
        if (currentWeapon != null && currentWeapon.weaponPrefab && sheathHolder)
        {
            currentSheathInstance = Instantiate(currentWeapon.weaponPrefab, sheathHolder, false);
            ApplySocket(currentSheathInstance.transform, currentWeapon.sheathSocket);
        }
    }

    // Bật đúng script skill theo loại vũ khí, tắt các script còn lại
    private void RefreshWeaponScripts()
    {
        bool isSword = currentWeapon != null && currentWeapon.weaponType == WeaponType.Sword;
        bool isAxe = currentWeapon != null && currentWeapon.weaponType == WeaponType.Axe;
        bool isMage = currentWeapon != null && currentWeapon.weaponType == WeaponType.Mage;

        // Enable/disable across entire character hierarchy (not just this GameObject)
        var swordAll = GetComponentsInChildren<SwordSkills>(true);
        foreach (var s in swordAll) if (s) s.enabled = isSword;

        var axeAll = GetComponentsInChildren<AxeSkill>(true);
        foreach (var a in axeAll) if (a) a.enabled = isAxe;

        var mageAll = GetComponentsInChildren<MageSkills>(true);
        foreach (var m in mageAll) if (m) m.enabled = isMage;
    }

    private void ApplySocket(Transform instance, SocketOffset s)
    {
        if (!instance) return;
        instance.localPosition = s.localPosition;
        instance.localRotation = Quaternion.Euler(s.localEuler);
        if (s.localScale != Vector3.zero) instance.localScale = s.localScale;
#if UNITY_EDITOR
        // Debug nhẹ để xác thực offset được áp
        // Debug.Log($"ApplySocket pos={s.localPosition} euler={s.localEuler} scale={s.localScale}", instance);
#endif
    }

    private void ClearHeld()
    {
        if (currentHeldInstance)
        {
            Destroy(currentHeldInstance);
            currentHeldInstance = null;
        }
    }

    private void ClearSheath()
    {
        if (currentSheathInstance)
        {
            Destroy(currentSheathInstance);
            currentSheathInstance = null;
        }
    }

    private void ApplyWeaponLayersAndParams()
    {
        int typeInt = currentWeapon != null ? (int)currentWeapon.weaponType : (int)WeaponType.None;
        animator.SetInteger(weaponTypeParam, typeInt);

        SetLayerWeightSafe(baseLayer, 1f);
        SetLayerWeightSafe(armsLayer, 1f);
        SetLayerWeightSafe(swordLayer, (typeInt == (int)WeaponType.Sword) ? 1f : 0f);
        SetLayerWeightSafe(axeLayer, (typeInt == (int)WeaponType.Axe) ? 1f : 0f);
        SetLayerWeightSafe(mageLayer, (typeInt == (int)WeaponType.Mage) ? 1f : 0f);
    }

    /// <summary>
    /// Public API to re-apply weapon layer weights.
    /// Called by GetHitState after hit stun ends to restore correct layers.
    /// </summary>
    public void ReapplyWeaponLayers()
    {
        ApplyWeaponLayersAndParams();
    }

    private void SetLayerWeightSafe(int layer, float weight)
    {
        if (layer >= 0 && layer < animator.layerCount)
            animator.SetLayerWeight(layer, weight);
    }

    public void UpdateSpeedForArmsLayer(float speed)
    {
        animator.SetFloat(speedParam, speed);
    }

    // Animation Event: Set weapon type for Arms Layer
    public void AE_SetWeaponTypeForArms(int weaponTypeIndex)
    {
        animator.SetInteger(weaponTypeParam, weaponTypeIndex);
    }

    // AutoBindWeaponHitRunner method removed - effects handled by separate scripts

    private void SyncWithEquipmentSystem()
    {
        // Tìm EquipmentSystem và sync weapon
        var equip = equipmentSystem;
        if (equip == null) equip = GetComponent<EquipmentSystem>();

        if (equip != null)
        {
            equip.SyncWeapon(currentWeapon);
        }
    }

    // ========== Animation Events ==========
    public void AE_DrawWeapon()
    {
        DrawWeaponVisual();

        var equip = equipmentSystem;
        if (equip != null && currentHeldInstance != null)
        {
            var dealer = currentHeldInstance.GetComponentInChildren<DamageDealer>();
            equip.BindHeldDamageDealer(dealer);
        }
    }

    public void AE_SheathWeapon()
    {
        var auraCtrl = GetComponent<WeaponAuraController>();
        if (auraCtrl != null) { auraCtrl.AE_AuraOff(); auraCtrl.UnbindAura(); }

        var equip = equipmentSystem;
        if (equip != null) equip.UnbindHeld();

        SheathWeaponVisual();

        // Disable all weapon skill scripts when sheathing
        var swordAll = GetComponentsInChildren<SwordSkills>(true);
        foreach (var s in swordAll) if (s) s.enabled = false;

        var axeAll = GetComponentsInChildren<AxeSkill>(true);
        foreach (var a in axeAll) if (a) a.enabled = false;

        var mageAll = GetComponentsInChildren<MageSkills>(true);
        foreach (var m in mageAll) if (m) m.enabled = false;

        Debug.Log("[WeaponController] Disabled all weapon skill scripts");

        // Clear ability icons when weapon is sheathed
        if (abilityManager != null)
        {
            abilityManager.AE_ClearWeaponAbilities();
        }

        // Unassign Ultimate Icon Shader material when sheathing
        var shaderController = FindFirstObjectByType<WeaponUltimateShaderController>();
        if (shaderController != null)
        {
            shaderController.UnassignMaterial();
            Debug.Log("[WeaponController] Unassigned Ultimate shader material on sheath");
        }
    }

    // ===== Wand helpers (Mage) =====
    private bool IsCurrentWand()
    {
        return currentWeapon != null && currentWeapon.weaponType == WeaponType.Mage && handHolder != null;
    }


    private void EnsureWandInstance()
    {
        if (!IsCurrentWand()) return;
        if (currentHeldInstance == null && currentWeapon != null && currentWeapon.weaponPrefab)
        {
            currentHeldInstance = Instantiate(currentWeapon.weaponPrefab, handHolder, false);
            ApplySocket(currentHeldInstance.transform, currentWeapon.handSocket);

            // THÊM: Bind Aura cho Wand như Axe/Sword
            var auraCtrl = GetComponent<WeaponAuraController>();
            if (auraCtrl != null) auraCtrl.BindAuraFrom(currentHeldInstance.transform, "Aura");

            // WeaponHitRunner removed - effects handled by separate scripts
        }
        else if (currentHeldInstance != null)
        {
            currentHeldInstance.transform.SetParent(handHolder, false);
            ApplySocket(currentHeldInstance.transform, currentWeapon.handSocket);
        }
    }

    private void SetWandActive(bool active)
    {
        if (currentHeldInstance == null) return;
        currentHeldInstance.SetActive(active);
    }

    private void StartWandDrawTween()
    {
        if (currentHeldInstance == null) return;
        if (wandScaleRoutine != null) { StopCoroutine(wandScaleRoutine); wandScaleRoutine = null; }
        // Start from slim-tall (Y=3) to normal (Y=1); X/Z stay 1
        var t = currentHeldInstance.transform;
        Vector3 start = new Vector3(0.0001f, 3f, 0.0001f);
        Vector3 end = new Vector3(1f, 1f, 1f);
        t.localScale = start;
        wandScaleRoutine = StartCoroutine(TweenScaleY(t, start.y, end.y, 0.5f, true));
    }

    private void StartWandSheathTween()
    {
        if (currentHeldInstance == null) return;
        if (wandScaleRoutine != null) { StopCoroutine(wandScaleRoutine); wandScaleRoutine = null; }
        // Ensure visible during tween, then hide
        SetWandActive(true);
        var t = currentHeldInstance.transform;
        Vector3 start = new Vector3(1f, 1f, 1f);
        Vector3 end = new Vector3(0.0001f, 0.5f, 0.0001f);
        t.localScale = start;
        wandScaleRoutine = StartCoroutine(TweenScaleY(t, start.y, end.y, 0.5f, false));
    }

    private System.Collections.IEnumerator TweenScaleY(Transform target, float fromY, float toY, float duration, bool showAtEnd)
    {
        float elapsed = 0f;
        // Ease.InOutQuad approximation using SmoothStep over t
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            float y = Mathf.Lerp(fromY, toY, eased);
            target.localScale = new Vector3(1f, y, 1f);
            yield return null;
        }
        target.localScale = new Vector3(1f, toY, 1f);
        if (!showAtEnd)
        {
            // After sheath tween finishes, hide wand
            SetWandActive(false);
        }
        wandScaleRoutine = null;
    }

    // ========== Animation Event: Active weapon script by type ==========
    // Gọi từ clip Draw: AE_ActiveWeaponScript((int)WeaponType.Axe | Sword | Mage)
    public void AE_ActiveWeaponScript(int weaponTypeIndex)
    {
        var type = (WeaponType)weaponTypeIndex;

        bool isSword = type == WeaponType.Sword;
        bool isAxe = type == WeaponType.Axe;
        bool isMage = type == WeaponType.Mage;

        var swordAll = GetComponentsInChildren<SwordSkills>(true);
        foreach (var s in swordAll)
        {
            if (s)
            {
                s.enabled = isSword;
                if (isSword) Debug.Log("[WeaponController] Enabled SwordSkills");
                else Debug.Log("[WeaponController] Disabled SwordSkills");
            }
        }

        var axeAll = GetComponentsInChildren<AxeSkill>(true);
        foreach (var a in axeAll)
        {
            if (a)
            {
                a.enabled = isAxe;
                if (isAxe) Debug.Log("[WeaponController] Enabled AxeSkill");
                else Debug.Log("[WeaponController] Disabled AxeSkill");
            }
        }

        var mageAll = GetComponentsInChildren<MageSkills>(true);
        foreach (var m in mageAll)
        {
            if (m)
            {
                m.enabled = isMage;
                if (isMage) Debug.Log("[WeaponController] Enabled MageSkills");
                else Debug.Log("[WeaponController] Disabled MageSkills");
            }
        }


        // Với Wand: đảm bảo instance dưới handHolder được bật khi kích hoạt bằng AE
        if (isMage)
        {
            EnsureWandInstance();
            SetWandActive(true);

            // THÊM: Bind Aura khi AE_ActiveWeaponScript kích hoạt Mage
            var auraCtrl = GetComponent<WeaponAuraController>();
            if (auraCtrl != null && currentHeldInstance != null)
                auraCtrl.BindAuraFrom(currentHeldInstance.transform, "Aura");
        }

        // Set ability icons when weapon is drawn
        if (abilityManager != null)
        {
            abilityManager.AE_SetWeaponAbilities();
            Debug.Log("[WeaponController] AE_SetWeaponAbilities called");
        }
        else
        {
            // Try to find WeaponAbilityManager in current weapon instance
            if (currentHeldInstance != null)
            {
                abilityManager = currentHeldInstance.GetComponent<WeaponAbilityManager>();
                if (abilityManager != null)
                {
                    Debug.Log("[WeaponController] Found WeaponAbilityManager in weapon instance");
                    abilityManager.AE_SetWeaponAbilities();
                }
                else
                {
                    // Try to find in children
                    abilityManager = currentHeldInstance.GetComponentInChildren<WeaponAbilityManager>();
                    if (abilityManager != null)
                    {
                        Debug.Log("[WeaponController] Found WeaponAbilityManager in weapon instance children");
                        abilityManager.AE_SetWeaponAbilities();
                    }
                    else
                    {
                        Debug.LogWarning("[WeaponController] No WeaponAbilityManager found in weapon instance! Cannot set ability icons.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[WeaponController] abilityManager is null and no weapon instance! Cannot set ability icons.");
            }
        }

        // Handle Ultimate Icon Shader - only assign material if Ultimate is ready
        HandleUltimateIconShader(type);
    }

    // Handle Ultimate Icon Shader based on weapon type and cooldown state
    private void HandleUltimateIconShader(WeaponType weaponType)
    {
        // Find Ultimate Icon Shader Controller
        var shaderController = FindFirstObjectByType<WeaponUltimateShaderController>();
        if (shaderController == null)
        {
            Debug.LogWarning("[WeaponController] WeaponUltimateShaderController not found!");
            return;
        }

        // Check if Ultimate is on cooldown
        var abilityIconManager = FindFirstObjectByType<AbilityIconManager>();
        bool isUltimateOnCooldown = false;
        if (abilityIconManager != null)
        {
            isUltimateOnCooldown = abilityIconManager.IsOnCooldown(AbilityInput.Q_Ultimate);
        }

        // Only assign material if Ultimate is ready (not on cooldown)
        if (!isUltimateOnCooldown)
        {
            // Update material for current weapon type
            shaderController.UpdateMaterialForWeapon(weaponType);
            Debug.Log($"[WeaponController] Assigned Ultimate shader for {weaponType} (Ultimate ready)");
        }
        else
        {
            // Don't assign material if Ultimate is on cooldown
            Debug.Log($"[WeaponController] Skipped Ultimate shader assignment for {weaponType} (Ultimate on cooldown)");
        }
    }
}
