using UnityEngine;

/// <summary>
/// Gắn script này vào root của VFX prefab để đảm bảo nó luôn spawn
/// thẳng đứng vuông góc với mặt đất, bất kể rotation được truyền vào
/// lúc Instantiate hay rotation của spawnpoint.
/// </summary>
public class VFXAlignToWorld : MonoBehaviour
{
    [Tooltip("Nếu true: chỉ reset rotation, giữ nguyên position và scale.")]
    [SerializeField] private bool alignOnAwake = true;

    [Tooltip("Rotation world-space cuối cùng sau khi align. Để (0,0,0) là thẳng đứng.")]
    [SerializeField] private Vector3 targetWorldEuler = Vector3.zero;

    private void Awake()
    {
        if (alignOnAwake)
            transform.rotation = Quaternion.Euler(targetWorldEuler);
    }
}
