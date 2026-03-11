using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShieldActivate : MonoBehaviour
{
    public float ImpactLife;
    Vector4[] points;
    Material m_material;
    List<Vector4> Hitpoints;
    MeshRenderer m_meshRenderer;
    float time;

    [Header("=== Shield Blocking ===")]
    [Tooltip("Bật chức năng chặn enemy và damage")]
    public bool enableBlocking = true;

    /// <summary>
    /// Static flag — PlayerHealth kiểm tra để chặn damage khi shield active
    /// </summary>
    public static bool IsShieldActive { get; private set; }

    private SphereCollider sphereCollider;
    private float worldRadius; // bán kính thực tế (tính cả scale)

    void Start()
    {
        time = Time.time;
        points = new Vector4[30];
        Hitpoints = new List<Vector4>();
        m_meshRenderer = GetComponent<MeshRenderer>();
        m_material = m_meshRenderer.material;

        if (enableBlocking)
        {
            // SphereCollider phải là TRIGGER để OnTriggerStay hoạt động
            // (NavMeshAgent bỏ qua physical collider nên không dùng non-trigger được)
            sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                sphereCollider.isTrigger = true;
                // Tính bán kính thực tế = collider radius * max scale
                float maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
                worldRadius = sphereCollider.radius * maxScale;
            }

            IsShieldActive = true;
            Debug.Log($"[ShieldActivate] Shield blocking ON! worldRadius={worldRadius}");
        }
    }

    void Update()
    {
        //Set material ( based on Shader_IntegratedEffect ) point array
        m_material.SetVectorArray("_Points", points);

        //Find available points 
        Hitpoints = Hitpoints
        .Select(s => new Vector4(s.x, s.y, s.z, s.w + Time.deltaTime / ImpactLife))
        .Where(w => w.w <= 1).ToList();

        //Fill empty point for list circle
        if (Time.time > time + 0.1f)
        {
            time = Time.time;
            AddEmpty();
        }

        //Set array
        Hitpoints.ToArray().CopyTo(points, 0);
    }

    public void AddHitObject(Vector3 position)
    {
        position -= transform.position;
        position = position.normalized/2;
        Hitpoints.Add(new Vector4(position.x, position.y, position.z, 0));
    }

    public void AddEmpty()
    {
        Hitpoints.Add(new Vector4(0, 0, 0, 0));
    }

    /// <summary>
    /// Khi enemy vào bên trong shield trigger → warp enemy ra rìa shield.
    /// Enemy vẫn di chuyển và chạy animation bình thường, nhưng mỗi frame bị đẩy ra rìa.
    /// Hiệu ứng: shield hoạt động như bức tường vô hình.
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        if (!enableBlocking) return;

        var enemyScript = other.GetComponent<EnemyScript>();
        if (enemyScript == null) enemyScript = other.GetComponentInParent<EnemyScript>();
        if (enemyScript == null) return;

        // Tính hướng từ tâm shield đến enemy (chỉ trên mặt phẳng XZ)
        Vector3 center = transform.position;
        Vector3 enemyPos = other.transform.position;
        Vector3 dir = enemyPos - center;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
        dir = dir.normalized;

        // Warp enemy ra rìa shield (vị trí ngay bên ngoài)
        Vector3 edgePos = center + dir * (worldRadius + 0.1f);
        edgePos.y = enemyPos.y; // giữ nguyên độ cao

        if (enemyScript.navMeshAgent != null)
        {
            enemyScript.navMeshAgent.Warp(edgePos);
        }

        // Hiệu ứng hit ripple trên bề mặt shield
        AddHitObject(center + dir * worldRadius);
    }

    private void OnDestroy()
    {
        if (enableBlocking)
        {
            IsShieldActive = false;
            Debug.Log("[ShieldActivate] Shield deactivated!");
        }
    }
}
