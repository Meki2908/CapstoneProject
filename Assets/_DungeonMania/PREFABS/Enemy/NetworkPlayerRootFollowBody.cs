using UnityEngine;

/// <summary>
/// Root follow CharacterController child — local physics (Fusion sync thân sau).
/// </summary>
[DefaultExecutionOrder(100)]
public class NetworkPlayerRootFollowBody : MonoBehaviour
{
    Transform _body;
    Vector3 _localPos;
    Quaternion _bodyWorldRotation = Quaternion.identity;

    void Awake()
    {
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

        Vector3 childWorldPos = _body.position;
        Quaternion childWorldRot = _body.rotation;

        Quaternion rootRot = transform.rotation;
        Vector3 rootPos = childWorldPos - rootRot * _localPos;

        transform.SetPositionAndRotation(rootPos, rootRot);
        _body.localPosition = _localPos;
        _body.localRotation = Quaternion.Inverse(rootRot) * childWorldRot;
        _bodyWorldRotation = childWorldRot;
    }
}
