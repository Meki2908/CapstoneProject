using System.Collections.Generic;
using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    [Tooltip("Nếu để trống, sẽ tự lấy tất cả con trực tiếp của Path theo thứ tự trong Hierarchy.")]
    public List<Transform> points = new List<Transform>();

    public bool autoCollectChildren = true;

    private void OnValidate()
    {
        if (!autoCollectChildren) return;

        points.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            points.Add(transform.GetChild(i));
        }
    }

    private void OnDrawGizmos()
    {
        if (points == null || points.Count < 2) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < points.Count - 1; i++)
        {
            if (points[i] && points[i + 1])
                Gizmos.DrawLine(points[i].position, points[i + 1].position);
        }

        // vẽ điểm
        Gizmos.color = Color.cyan;
        foreach (var p in points)
        {
            if (p) Gizmos.DrawSphere(p.position, 0.15f);
        }
    }
}
