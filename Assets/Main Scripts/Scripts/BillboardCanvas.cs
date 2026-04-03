using UnityEngine;

/// <summary>
/// Gắn script này vào Canvas (hoặc bất kỳ object nào) trên đầu NPC
/// để nó luôn xoay mặt về phía camera — Billboard effect.
/// </summary>
public class BillboardCanvas : MonoBehaviour
{
    [Tooltip("Bật để chỉ xoay trục Y (giữ thẳng đứng). Tắt để xoay theo cả trục X (nhìn từ mọi góc).")]
    public bool lockVertical = true;

    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;
    }

    void LateUpdate()
    {
        if (_cam == null)
        {
            _cam = Camera.main;
            return;
        }

        if (lockVertical)
        {
            // Chỉ xoay quanh Y — text luôn thẳng đứng, quay mặt theo camera ngang
            Vector3 dir = transform.position - _cam.transform.position;
            dir.y = 0f;
            if (dir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(dir);
        }
        else
        {
            // Xoay hoàn toàn — nhìn từ mọi góc (kể cả nhìn từ trên xuống)
            transform.LookAt(_cam.transform.position);
            transform.Rotate(0f, 180f, 0f); // flip để mặt chữ quay ra ngoài
        }
    }
}
