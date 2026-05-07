using UnityEngine;
using System; // th�m d? d�ng Action

[RequireComponent(typeof(Animator))]
public class WeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handHolder;
    [SerializeField] private Transform sheathHolder;
    [SerializeField] private EquipmentSystem equipmentSystem; // ch? d�ng cho damage hooks n?u c?n

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

    // TH�M: S? ki?n b?n ra khi d?i vu kh�
    public event Action<WeaponSO> OnWeaponChanged;

    private Animator animator;
    private GameObject currentHeldInstance;
    private GameObject currentSheathInstance;
    private Coroutine wandScaleRoutine;

    private void Awake()
    {
        // Animator may live on a child (model/rig hierarchy).
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

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
        var loadedFromDisk = false;
        if (WeaponSelectionPersistence.TryLoad(out var savedType) && savedType != WeaponType.None)
        {
            var so = WeaponSelectionPersistence.ResolveWeaponSO(savedType);
            if (so != null)
            {
                EquipWeapon(so);
                loadedFromDisk = true;
            }
        }

        if (!loadedFromDisk)
            ApplyDefaultStartWeaponVisuals();
    }

    void ApplyDefaultStartWeaponVisuals()
    {
        ApplyWeaponLayersAndParams();
        if (IsCurrentWand())
        {
            EnsureWandInstance();
            SetWandActive(false);
        }
        else
        {
            ShowWeaponInSheath();
        }

        OnWeaponChanged?.Invoke(currentWeapon);
        SyncWithEquipmentSystem();
    }

    void OnApplicationQuit()
    {
        if (currentWeapon != null)
            WeaponSelectionPersistence.Save(currentWeapon.weaponType);
    }

    // TH�M: API set weapon t? pickup (c� th? d�ng chung)
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
            SetWandActive(false); // sheath = ?n
        }
        else
        {
            ShowWeaponInSheath();
        }

        // Auto-bind v� sync
        // WeaponHitRunner removed - effects handled by separate scripts
        SyncWithEquipmentSystem();

        // B?t/t?t script theo weapon type m?i
        RefreshWeaponScripts();

        // B?N s? ki?n cho t?t c? consumer (Skills/HitRunner/UIs...)
        OnWeaponChanged?.Invoke(currentWeapon);

        if (weapon != null)
            WeaponSelectionPersistence.Save(weapon.weaponType);
    }

    public WeaponSO GetCurrentWeapon() => currentWeapon;

    /// <summary>
    /// Trigger draw animation on the animator without swapping visuals/scripts immediately.
    /// Used by the no-AE timing flow (swap happens later at a configured normalizedTime).
    /// </summary>
    public void TriggerDrawAnimationOnly()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (animator == null) return;

        ApplyWeaponLayersAndParams();
        animator.ResetTrigger(sheathTrigger);
        animator.SetTrigger(drawTrigger);
    }

    /// <summary>
    /// Trigger sheath animation on the animator without swapping visuals/scripts immediately.
    /// Used by the no-AE timing flow (swap happens later at a configured normalizedTime).
    /// </summary>
    public void TriggerSheathAnimationOnly()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (animator == null) return;

        ApplyWeaponLayersAndParams();
        animator.ResetTrigger(drawTrigger);
        animator.SetTrigger(sheathTrigger);
    }

    /// <summary>
    /// Idempotent: ensure weapon is visually drawn and scripts/layers are active.
    /// Safe to call repeatedly (does not spam animator triggers by default).
    /// </summary>
    public void EnsureDrawn(bool requestAnimation = false)
    {
        if (currentWeapon == null) return;

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        ApplyWeaponLayersAndParams();
        RefreshWeaponScripts();

        if (IsCurrentWand())
        {
            EnsureWandInstance();
            SetWandActive(true);
        }
        else
        {
            // Ensure held instance exists
            if (currentHeldInstance == null && currentWeapon.weaponPrefab != null && handHolder != null)
            {
                currentHeldInstance = Instantiate(currentWeapon.weaponPrefab, handHolder, false);
                StripPhysicalColliders(currentHeldInstance);
                ApplySocket(currentHeldInstance.transform, currentWeapon.handSocket);
            }
            else if (currentHeldInstance != null && handHolder != null && currentHeldInstance.transform.parent != handHolder)
            {
                currentHeldInstance.transform.SetParent(handHolder, false);
                ApplySocket(currentHeldInstance.transform, currentWeapon.handSocket);
            }

            // Hide sheath instance if any
            if (currentSheathInstance != null)
            {
                Destroy(currentSheathInstance);
                currentSheathInstance = null;
            }
        }

        ReapplyWeaponLayers();

        if (requestAnimation && animator != null)
        {
            Debug.Log($"[ToggleWeapon][WeaponController.EnsureDrawn] frame={Time.frameCount} time={Time.time:F3} weapon={(currentWeapon != null ? currentWeapon.weaponName : "null")} type={(currentWeapon != null ? currentWeapon.weaponType.ToString() : "null")} combatMove={animator.GetBool("combatMove")}");
            animator.ResetTrigger(sheathTrigger);
            animator.SetTrigger(drawTrigger);
        }
    }

    /// <summary>
    /// Idempotent: ensure weapon is visually sheathed and scripts are disabled.
    /// Safe to call repeatedly (does not spam animator triggers by default).
    /// </summary>
    public void EnsureSheathed(bool requestAnimation = false)
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        ApplyWeaponLayersAndParams();

        // Disable scripts for all weapons (same as AE_SheathWeapon would do)
        var swordAll = GetComponentsInChildren<SwordSkills>(true);
        foreach (var s in swordAll) if (s) s.enabled = false;
        var axeAll = GetComponentsInChildren<AxeSkill>(true);
        foreach (var a in axeAll) if (a) a.enabled = false;
        var mageAll = GetComponentsInChildren<MageSkills>(true);
        foreach (var m in mageAll) if (m) m.enabled = false;

        if (IsCurrentWand())
        {
            EnsureWandInstance();
            SetWandActive(false);
        }
        else
        {
            // Ensure sheath instance exists
            if (currentSheathInstance == null)
                ShowWeaponInSheath();

            if (currentHeldInstance != null)
            {
                Destroy(currentHeldInstance);
                currentHeldInstance = null;
            }
        }

        // Turn off weapon layers when sheathed
        SetLayerWeightSafe(swordLayer, 0f);
        SetLayerWeightSafe(axeLayer, 0f);
        SetLayerWeightSafe(mageLayer, 0f);

        if (requestAnimation && animator != null)
        {
            Debug.Log($"[ToggleWeapon][WeaponController.EnsureSheathed] frame={Time.frameCount} time={Time.time:F3} weapon={(currentWeapon != null ? currentWeapon.weaponName : "null")} type={(currentWeapon != null ? currentWeapon.weaponType.ToString() : "null")} combatMove={animator.GetBool("combatMove")}");
            animator.ResetTrigger(drawTrigger);
            animator.SetTrigger(sheathTrigger);
        }
    }

    public void DrawWeaponVisual() // g?i t? Animation/State
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
                StripPhysicalColliders(currentHeldInstance);
                ApplySocket(currentHeldInstance.transform, currentWeapon.handSocket);

                // Bind Aura (n?u c�)
                var auraCtrl = GetComponent<WeaponAuraController>();
                if (auraCtrl != null) auraCtrl.BindAuraFrom(currentHeldInstance.transform, "Aura");

                // Auto-bind WeaponHitRunner n?u c�
                // WeaponHitRunner removed - effects handled by separate scripts

                // B?N s? ki?n v� instance thay d?i (n?u consumer c?n rebind theo instance)
                OnWeaponChanged?.Invoke(currentWeapon);
            }
        }
    }

    public void SheathWeaponVisual() // g?i t? Animation/State
    {
        animator.ResetTrigger(drawTrigger);
        animator.SetTrigger(sheathTrigger);
        // T?t Aura v� unbind
        var auraCtrl = GetComponent<WeaponAuraController>();
        if (auraCtrl != null) { auraCtrl.AE_AuraOff(); auraCtrl.UnbindAura(); }

        if (IsCurrentWand())
        {
            EnsureWandInstance();
            StartWandSheathTween();
            // Kh�ng t?o sheath instance cho Wand; ch? ?n
        }
        else
        {
            ClearSheath();
            if (currentHeldInstance) { Destroy(currentHeldInstance); currentHeldInstance = null; }
            ShowWeaponInSheath();
        }

        // B?N s? ki?n n?u consumer quan t�m tr?ng th�i sheath
        OnWeaponChanged?.Invoke(currentWeapon);
    }

    private void ShowWeaponInSheath()
    {
        // Wand KH�NG d�ng sheathHolder, ch? ?n/hi?n du?i handHolder
        if (IsCurrentWand()) return;
        if (currentWeapon != null && currentWeapon.weaponPrefab && sheathHolder)
        {
            currentSheathInstance = Instantiate(currentWeapon.weaponPrefab, sheathHolder, false);
            StripPhysicalColliders(currentSheathInstance);
            ApplySocket(currentSheathInstance.transform, currentWeapon.sheathSocket);
        }
    }

    // B?t d�ng script skill theo lo?i vu kh�, t?t c�c script c�n l?i
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
        // Debug nh? d? x�c th?c offset du?c �p
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
        
        // 1. C?p nh?t Parameter cho Base Layer (Nested Blend Tree) v� UpperBody_Layer (GetHit)
        animator.SetInteger(weaponTypeParam, typeInt);
        // TH�M: �?ng qu�n c?p nh?t WeaponIndex (Float) cho Blend Tree!
        animator.SetFloat("WeaponIndex", (float)typeInt); 

        // 2. Base Layer (0) v� UpperBody Layer (ArmsLayer - 5) lu�n s�ng d�n
        SetLayerWeightSafe(baseLayer, 1f);
        SetLayerWeightSafe(armsLayer, 1f);
        
        // 3. Quy t?c S�t th?: ��ng bang m?i layer vu kh� n?u chua r�t ra
        // Tr?ng th�i n�y s? du?c g?i l?i b?i Animator Event sau khi DrawWeapon k?t th�c!
        // T?m th?i ? d�y, n?u dang c?t vu kh� th� set b?ng 0 h?t.
        bool isDrawn = animator.GetBool("combatMove"); // L?y tr?ng th�i r�t/c?t hi?n t?i

        if (isDrawn)
        {
            SetLayerWeightSafe(swordLayer, (typeInt == (int)WeaponType.Sword) ? 1f : 0f);
            SetLayerWeightSafe(axeLayer, (typeInt == (int)WeaponType.Axe) ? 1f : 0f);
            SetLayerWeightSafe(mageLayer, (typeInt == (int)WeaponType.Mage) ? 1f : 0f);
        }
        else
        {
            SetLayerWeightSafe(swordLayer, 0f);
            SetLayerWeightSafe(axeLayer, 0f);
            SetLayerWeightSafe(mageLayer, 0f);
        }
    }

    /// <summary>
    /// Public API to re-apply weapon layer weights.
    /// Called by GetHitState after hit stun ends to restore correct layers.
    /// </summary>
    public void ReapplyWeaponLayers()
    {
        ApplyWeaponLayersAndParams();
    }

    /// <summary>
    /// Khi đang cất vũ khí, <see cref="ApplyWeaponLayersAndParams"/> tắt hết Sword/Axe/Mage layer → GetHit trên layer đúng không chạy.
    /// Gọi trước <c>SetTrigger("gethit")</c> để bật đúng layer theo vũ khí đang trang bị (vẫn giữ base + arms).
    /// </summary>
    public void SetWeaponLayersForSheathedGetHit()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null || currentWeapon == null) return;

        int typeInt = (int)currentWeapon.weaponType;
        if (typeInt == (int)WeaponType.None) return;

        animator.SetInteger(weaponTypeParam, typeInt);
        animator.SetFloat("WeaponIndex", typeInt);

        SetLayerWeightSafe(baseLayer, 1f);
        SetLayerWeightSafe(armsLayer, 1f);
        SetLayerWeightSafe(swordLayer, typeInt == (int)WeaponType.Sword ? 1f : 0f);
        SetLayerWeightSafe(axeLayer, typeInt == (int)WeaponType.Axe ? 1f : 0f);
        SetLayerWeightSafe(mageLayer, typeInt == (int)WeaponType.Mage ? 1f : 0f);
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
        // T�m EquipmentSystem v� sync weapon
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

        // Áp tất cả các Layer vũ khí về 0 để Base/UpperBody tự do hoạt động
        SetLayerWeightSafe(swordLayer, 0f);
        SetLayerWeightSafe(axeLayer, 0f);
        SetLayerWeightSafe(mageLayer, 0f);
    }

    // ===== Wand helpers (Mage) =====
    private bool IsCurrentWand()
    {
        return currentWeapon != null && currentWeapon.weaponType == WeaponType.Mage && handHolder != null;
    }

    /// <summary>
    /// Neo spawn VFX/projectile: instance vũ khí đang cầm (wand) hoặc <see cref="handHolder"/> — tránh spawn ở pivot chân nhân vật.
    /// </summary>
    public Transform GetHeldWeaponSpawnAnchor()
    {
        if (currentHeldInstance != null)
            return currentHeldInstance.transform;
        return handHolder;
    }

    private void EnsureWandInstance()
    {
        if (!IsCurrentWand()) return;
        if (currentHeldInstance == null && currentWeapon != null && currentWeapon.weaponPrefab)
        {
            currentHeldInstance = Instantiate(currentWeapon.weaponPrefab, handHolder, false);
                StripPhysicalColliders(currentHeldInstance);
            ApplySocket(currentHeldInstance.transform, currentWeapon.handSocket);

            // TH�M: Bind Aura cho Wand nhu Axe/Sword
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
    // G?i t? clip Draw: AE_ActiveWeaponScript((int)WeaponType.Axe | Sword | Mage)
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


        // V?i Wand: d?m b?o instance du?i handHolder du?c b?t khi k�ch ho?t b?ng AE
        if (isMage)
        {
            EnsureWandInstance();
            SetWandActive(true);

            // TH�M: Bind Aura khi AE_ActiveWeaponScript k�ch ho?t Mage
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

        // �?y layer vu kh� tuong ?ng l�n 1, d?p c�c layer kh�c v? 0
        SetLayerWeightSafe(swordLayer, isSword ? 1f : 0f);
        SetLayerWeightSafe(axeLayer, isAxe ? 1f : 0f);
        SetLayerWeightSafe(mageLayer, isMage ? 1f : 0f);
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

    // === FIX FUSION PHYSICS BUG ===
    // X�a t?t c? Collider c?ng (kh�ng ph?i Trigger) kh?i prefab vu kh� khi Spawn
    // �? tr�nh vi?c ki?m d?p tr�ng Player ho?c qu�i g�y n? physics, vang kh?i map
    private void StripPhysicalColliders(GameObject obj)
    {
        if (obj == null) return;
        Collider[] cols = obj.GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
        {
            // Ch? x�a c�c Collider v?t l�, gi? l?i c�c Trigger n?u c� (m?c d� DamageDealer d�ng SphereCast)
            if (c != null && !c.isTrigger) 
            {
                c.enabled = false; // T?t ngay l?p t?c d? tr�nh n? Physics trong frame d?u ti�n
            }
        }
    }

}