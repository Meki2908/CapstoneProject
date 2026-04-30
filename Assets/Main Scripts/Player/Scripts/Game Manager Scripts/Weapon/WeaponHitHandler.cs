using System.Collections;
using UnityEngine;

public class WeaponHitRunner : MonoBehaviour
{
    [Header("Bindings (set when drawing/equipping)")]
    public WeaponSO weapon;                  // Data for the current weapon
    public EquipmentSystem equipment;        // Provides StartDealDamage/EndDealDamage on held weapon
    public Transform vfxSpawn;               // Optional anchor (hand tip). Position can be overridden dynamically.
    public Transform handReference;          // Optional hand bone for better anchoring
    public Transform characterRoot;          // Usually the character transform

    [Header("Behavior")]
    [Tooltip("Use input/camera forward for slash direction instead of character forward.")]
    public bool useInputDirection = true;
    [Tooltip("Auto-destroy spawned VFX after seconds.")]
    public float vfxLifetime = 2f;

    // (Coroutine removed in favor of Animation Events)
    private int currentHitIndex = 0; // Track current hit index

    public void Bind(WeaponSO weaponSO, EquipmentSystem equip, Transform spawnPoint, Transform hand, Transform root)
    {
        weapon = weaponSO;
        equipment = equip;
        vfxSpawn = spawnPoint;
        handReference = hand;
        characterRoot = root;
    }

    public void StartHit(int hitIndex)
    {
        if (weapon == null || weapon.hitTimings == null) return;
        if (hitIndex < 0 || hitIndex >= weapon.hitTimings.Length) return;

        currentHitIndex = hitIndex; // Store current hit index
        Debug.Log($"StartHit: hitIndex={hitIndex}, currentHitIndex={currentHitIndex}");

        // HỦY BỎ COROUTINE TẠI ĐÂY - Hãy để Animation Event (AE_StartHitbox / AE_PlayNormalVFX) tự làm việc của nó!
    }

    public void CancelCurrentHit()
    {
        // Coroutine removed
        equipment?.EndDealDamage();
    }

    // Gắn Event này vào đầu frame vung kiếm trúng đích
    public void AE_StartHitbox()
    {
        equipment?.StartDealDamage();
    }

    // Gắn Event này vào cuối frame vung kiếm
    public void AE_EndHitbox()
    {
        equipment?.EndDealDamage();
    }

    private (Vector3 pos, Quaternion rot, Vector3 scl) BuildSpawnTransform(HitTiming timing)
    {
        var rule = timing.spawnRule;

        // Base forward: input/camera or character
        Vector3 baseForward = GetBaseForward();
        if (baseForward.sqrMagnitude < 0.0001f) baseForward = Vector3.forward;

        // Yaw + pitch + roll rotations
        Quaternion yawRot = Quaternion.AngleAxis(rule.yawOffset, Vector3.up);
        Vector3 right = Vector3.Cross(Vector3.up, baseForward).normalized;
        Quaternion pitchRot = Quaternion.AngleAxis(rule.pitchOffset, right);
        Quaternion rollRot = Quaternion.AngleAxis(rule.rollOffset, baseForward);
        Quaternion finalRot = Quaternion.LookRotation(baseForward) * yawRot * pitchRot * rollRot;

        // Anchor selection (vfxSpawn -> hand -> character root -> self)
        Transform anchor =
            (vfxSpawn != null ? vfxSpawn :
            (handReference != null ? handReference :
            (characterRoot != null ? characterRoot : transform)));

        Vector3 worldOffset = finalRot * rule.localOffset;
        Vector3 pos = anchor.position + worldOffset;
        Vector3 scl = Vector3.one * (rule.scale <= 0f ? 1f : rule.scale);

        return (pos, finalRot, scl);
    }

    private Vector3 GetBaseForward()
    {
        // Always use player direction for VFX spawn
        return characterRoot != null ? characterRoot.forward : transform.forward;
    }

    private void OnEnable()
    {
        // Auto-find hand reference if not set
        if (handReference == null)
        {
            FindHandReference();
        }

        // Tự lắng nghe OnWeaponChanged để rebind khi đổi vũ khí
        var wc = GetComponent<WeaponController>();
        if (wc != null)
        {
            wc.OnWeaponChanged -= OnWeaponChangedHandler;
            wc.OnWeaponChanged += OnWeaponChangedHandler;
        }
    }

    private void OnDisable()
    {
        var wc = GetComponent<WeaponController>();
        if (wc != null) wc.OnWeaponChanged -= OnWeaponChangedHandler;
    }

    private void OnWeaponChangedHandler(WeaponSO so)
    {
        // Rebind với vũ khí mới (giữ lại equipment/vfxSpawn/handRef nếu có)
        var equip = equipment != null ? equipment : GetComponent<EquipmentSystem>();
        Transform handRef = handReference != null ? handReference : null;
        Transform spawn = vfxSpawn != null ? vfxSpawn : handRef != null ? handRef : transform;
        Bind(so, equip, spawn, handRef, transform);
    }

    private void FindHandReference()
    {
        // Try to find hand reference in common locations
        if (handReference == null)
        {
            // Look for common hand bone names
            string[] handNames = { "Hand_R", "RightHand", "R_Hand", "hand_r", "HandR", "Right_Hand" };

            foreach (string handName in handNames)
            {
                Transform found = FindChildRecursive(transform, handName);
                if (found != null)
                {
                    handReference = found;
                    break;
                }
            }
        }

        // If still not found, try to find any bone with "hand" in the name
        if (handReference == null)
        {
            Transform found = FindChildRecursive(transform, "hand");
            if (found != null)
            {
                handReference = found;
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(name.ToLower()))
                return child;

            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    // Animation Event methods - FOOLPROOF VERSION
    public void AE_PlayNormalVFX_Current()
    {
        AE_PlayNormalVFX_Index(currentHitIndex);
    }

    public void AE_PlayNormalVFX_Index(int hitIndex)
    {
        if (weapon == null || weapon.normalVfxSpawnMode != WeaponSO.VfxSpawnMode.AnimationEvent) return;
        
        // Bảo hiểm 1: Nếu user quên đổi parameter trong Animation Event (để nguyên số 0 cho đòn 2, 3)
        if (hitIndex == 0 && currentHitIndex > 0)
        {
            hitIndex = currentHitIndex;
        }

        if (weapon.normalHitVfx == null || weapon.normalHitVfx.Length == 0) return;

        // Bảo hiểm 2: Nếu user chỉ nhét đúng 1 VFX vào mảng nhưng có 3 đòn đánh, thì xài lại VFX đó
        int prefabIndex = Mathf.Min(hitIndex, weapon.normalHitVfx.Length - 1);
        var prefab = weapon.normalHitVfx[prefabIndex];
        if (prefab == null) return;

        // Bảo hiểm 3: Rút đúng thông số Timing (localOffset, yawOffset) của đòn đánh đó để tránh sai lệch Y/Rotation
        if (weapon.hitTimings != null && weapon.hitTimings.Length > 0)
        {
            int timingIndex = Mathf.Min(hitIndex, weapon.hitTimings.Length - 1);
            var timing = weapon.hitTimings[timingIndex];
            
            var (pos, rot, scl) = BuildSpawnTransform(timing);

            var vfx = Instantiate(prefab, pos, rot);
            vfx.transform.localScale = Vector3.Scale(vfx.transform.localScale, scl);

            if (timing.spawnRule.extraEulerOffset != Vector3.zero)
            {
                vfx.transform.rotation = vfx.transform.rotation * Quaternion.Euler(timing.spawnRule.extraEulerOffset);
            }

            if (vfxLifetime > 0f) Destroy(vfx, vfxLifetime);
        }
    }
}
