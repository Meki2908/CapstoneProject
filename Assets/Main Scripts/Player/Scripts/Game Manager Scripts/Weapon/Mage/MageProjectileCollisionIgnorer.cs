using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bỏ qua va chạm giữa projectile VFX và collider của owner trong một khoảng thời gian ngắn
/// (tránh OnCollisionEnter ngay frame đầu khi spawn trùng capsule/player mesh).
/// </summary>
public sealed class MageProjectileCollisionIgnorer : MonoBehaviour
{
    readonly List<Collider> _self = new List<Collider>();
    readonly List<Collider> _owner = new List<Collider>();
    bool _applied;

    public static void Attach(GameObject projectileRoot, GameObject ownerRoot, float ignoreSeconds)
    {
        if (projectileRoot == null || ownerRoot == null || ignoreSeconds <= 0f) return;
        var c = projectileRoot.GetComponent<MageProjectileCollisionIgnorer>();
        if (c == null) c = projectileRoot.AddComponent<MageProjectileCollisionIgnorer>();
        c.Apply(ownerRoot, ignoreSeconds);
    }

    void Apply(GameObject ownerRoot, float ignoreSeconds)
    {
        _self.Clear();
        _owner.Clear();
        _self.AddRange(GetComponentsInChildren<Collider>(true));
        _owner.AddRange(ownerRoot.GetComponentsInChildren<Collider>(true));

        foreach (var s in _self)
        {
            if (s == null) continue;
            foreach (var o in _owner)
            {
                if (o == null || s == o) continue;
                Physics.IgnoreCollision(s, o, true);
            }
        }
        _applied = true;
        Destroy(this, ignoreSeconds);
    }

    void OnDestroy()
    {
        if (!_applied) return;
        foreach (var s in _self)
        {
            if (s == null) continue;
            foreach (var o in _owner)
            {
                if (o == null) continue;
                Physics.IgnoreCollision(s, o, false);
            }
        }
    }
}
