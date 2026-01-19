using UnityEngine;

public class PathFollower3D : MonoBehaviour
{
    [Header("Path Root (chứa các waypoint con)")]
    public Transform pathRoot;

    [Header("Movement")]
    public float moveSpeed = 4f;
    public float turnSpeed = 240f;      // độ/giây
    public float arriveDistance = 0.5f;

    [Header("Mode")]
    public bool loop = true;
    public bool pingPong = false;

    [Header("Options")]
    public bool faceMoveDirection = true;
    public bool ignoreY = false;        // bật nếu muốn bay ngang, không lên/xuống theo waypoint

    private Transform[] points;
    private int index = 0;
    private int dir = 1;

    void Awake()
    {
        CachePoints();
    }

    void CachePoints()
    {
        if (!pathRoot) return;

        int n = pathRoot.childCount;
        points = new Transform[n];
        for (int i = 0; i < n; i++)
            points[i] = pathRoot.GetChild(i);
    }

    void Update()
    {
        if (points == null || points.Length == 0) return;

        Transform target = points[index];
        if (!target) return;

        Vector3 targetPos = target.position;

        if (ignoreY)
            targetPos.y = transform.position.y;

        Vector3 toTarget = targetPos - transform.position;
        float dist = toTarget.magnitude;

        // tới waypoint -> next
        if (dist <= arriveDistance)
        {
            AdvanceIndex();
            target = points[index];
            if (!target) return;

            targetPos = target.position;
            if (ignoreY) targetPos.y = transform.position.y;

            toTarget = targetPos - transform.position;
        }

        Vector3 moveDir = (toTarget.sqrMagnitude > 0.0001f) ? toTarget.normalized : Vector3.zero;

        // xoay theo hướng di chuyển (tuỳ chọn)
        if (faceMoveDirection && moveDir != Vector3.zero)
        {
            Quaternion look = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        // di chuyển
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }

    void AdvanceIndex()
    {
        int count = points.Length;

        if (pingPong)
        {
            index += dir;
            if (index >= count) { index = count - 2; dir = -1; }
            else if (index < 0) { index = 1; dir = 1; }
            return;
        }

        index++;
        if (index >= count)
            index = loop ? 0 : count - 1;
    }
}
