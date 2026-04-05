using Unity.Netcode;
using UnityEngine;

/// <summary>
/// CC + Character nằm trên child "player", còn <see cref="NetworkObject"/> + <see cref="NetworkTransform"/> trên root.
/// Chỉ đồng bộ vị trí root với CC; không gán root.rotation theo thân nhân vật (camera là con của root → sẽ xoay theo hướng đi).
/// Hướng mesh giữ bằng localRotation của body: Inverse(root) * childWorldRot.
/// </summary>
[DefaultExecutionOrder(100)]
public class NetworkPlayerRootFollowBody : MonoBehaviour
{
    Transform _body;
    Vector3 _localPos;
    NetworkObject _netObj;

    void Awake()
    {
        _netObj = GetComponent<NetworkObject>();
        var cc = GetComponentInChildren<CharacterController>(true);
        if (cc != null)
        {
            _body = cc.transform;
            _localPos = _body.localPosition;
        }
    }

    void FixedUpdate()
    {
        if (_body == null) return;

        if (_netObj != null && _netObj.IsSpawned && !_netObj.IsOwner)
            return;

        // Không được đặt root.position = child.position (offset local) — đã xử lý bằng rootPos.
        Vector3 childWorldPos = _body.position;
        Quaternion childWorldRot = _body.rotation;

        // Giữ nguyên góc root (spawn / hệ camera nếu có chỉnh root). Không dùng rotation của body làm root → camera không quay theo hướng đi.
        Quaternion rootRot = transform.rotation;
        Vector3 rootPos = childWorldPos - rootRot * _localPos;

        transform.SetPositionAndRotation(rootPos, rootRot);
        _body.localPosition = _localPos;
        _body.localRotation = Quaternion.Inverse(rootRot) * childWorldRot;
    }
}
