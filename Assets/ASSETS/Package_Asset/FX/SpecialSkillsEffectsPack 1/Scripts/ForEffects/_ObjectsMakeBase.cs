using UnityEngine;

public class _ObjectsMakeBase : MonoBehaviour
{
    [Header("Objects To Spawn")]
    public GameObject[] m_makeObjs = new GameObject[0];

    protected Vector3 GetRandomVector(Vector3 range)
    {
        // random in [-range.x .. range.x]
        return new Vector3(
            Random.Range(-Mathf.Abs(range.x), Mathf.Abs(range.x)),
            Random.Range(-Mathf.Abs(range.y), Mathf.Abs(range.y)),
            Random.Range(-Mathf.Abs(range.z), Mathf.Abs(range.z))
        );
    }

    protected Vector3 GetRandomVector2(Vector3 range)
    {
        // random in [-range.x .. range.x] (used for scale offset)
        return new Vector3(
            Random.Range(-Mathf.Abs(range.x), Mathf.Abs(range.x)),
            Random.Range(-Mathf.Abs(range.y), Mathf.Abs(range.y)),
            Random.Range(-Mathf.Abs(range.z), Mathf.Abs(range.z))
        );
    }
}


