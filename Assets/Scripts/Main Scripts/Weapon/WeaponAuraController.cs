using UnityEngine;
using UnityEngine.Profiling;

public class WeaponAuraController : MonoBehaviour
{
    [Header("Aura root trong prefab vũ khí (gán động sau khi rút)")]
    [SerializeField] private GameObject auraRoot; // có thể null khi chưa rút vũ khí
    [SerializeField] private float autoOffAfter = 0f;
    private float offAtTime = -1f;

    // EquipmentSystem sẽ gọi ngay sau khi instantiate vũ khí
    public void BindAuraFrom(Transform weaponRoot, string auraPathInWeapon = "Aura")
    {
        if (!weaponRoot) { auraRoot = null; return; }
        var t = string.IsNullOrEmpty(auraPathInWeapon) ? null : weaponRoot.Find(auraPathInWeapon);
        auraRoot = t ? t.gameObject : null;
        Debug.Log($"BindAuraFrom: {auraRoot}");
    }

    public void UnbindAura() => auraRoot = null;

    private void Update()
    {
        if (autoOffAfter > 0f && auraRoot && auraRoot.activeSelf && offAtTime > 0f && Time.time >= offAtTime)
        {
            auraRoot.SetActive(false);
            offAtTime = -1f;
        }
    }

    // Animation Event / Timeline Signal
    public void AE_AuraOn()
    {
        if (!auraRoot) return;
        auraRoot.SetActive(true);
        offAtTime = (autoOffAfter > 0f) ? Time.time + autoOffAfter : -1f;
    }
    public void AE_AuraOff()
    {
        if (!auraRoot) return;
        auraRoot.SetActive(false);
        offAtTime = -1f;
    }
    public void AE_AuraOnFor(float seconds)
    {
        if (!auraRoot) return;
        auraRoot.SetActive(true);
        offAtTime = Time.time + Mathf.Max(0f, seconds);
    }
}