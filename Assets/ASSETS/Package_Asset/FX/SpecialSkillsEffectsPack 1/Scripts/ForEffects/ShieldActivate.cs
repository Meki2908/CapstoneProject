using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class ShieldActivate : MonoBehaviour
{
    [Header("Visual Effects")]
    public float ImpactLife;
    Vector4[] points;
    Material m_material;
    List<Vector4> Hitpoints;
    MeshRenderer m_meshRenderer;
    float time;

    [Header("Push Enemies Logic")]
    public float pushRadius = 3f;
    public float pushForce = 8f; // Gán thẳng vận tốc nên 8-10 là lướt rất mượt rồi
    public LayerMask enemyLayer;
    public float stunDuration = 0.5f;

    // === SỬA LỖI 2: Dùng Dictionary để nhớ cả Agent lẫn Rigidbody ===
    private readonly Dictionary<int, (NavMeshAgent agent, Rigidbody rb)> _stunnedEnemies = new Dictionary<int, (NavMeshAgent, Rigidbody)>();
    
    public static bool IsShieldActive { get; private set; }
    
    public static void ForceReset() => IsShieldActive = false;

    void OnEnable()
    {
        IsShieldActive = true;
    }

    void FixedUpdate() 
    {
        PushEnemiesContinuously();
    }

    void OnDisable()
    {
        IsShieldActive = false;
        
        // === BẢO HIỂM LỖI 2: TRẢ LẠI AI KHI CẤT KHIÊN ĐỘT NGỘT ===
        // Nếu Coroutine bị giết ngang, ta phải tự tay giải cứu bọn quái đang bị choáng
        foreach (var kvp in _stunnedEnemies)
        {
            var data = kvp.Value;
            if (data.rb != null)
            {
                data.rb.linearVelocity = Vector3.zero;
                data.rb.angularVelocity = Vector3.zero;
                data.rb.isKinematic = true;
            }
            if (data.agent != null && data.agent.gameObject.activeInHierarchy)
            {
                data.agent.enabled = true;
                // Đánh thức ngay lập tức: ép tìm đường lại tới Player
                if (data.agent.isOnNavMesh) data.agent.SetDestination(transform.root.position);
            }
        }
        _stunnedEnemies.Clear();
        // ==========================================================
        
        // === BÍ QUYẾT: ĐÁNH THỨC BỌN QUÁI ĐANG KẸT Ở MÉP KHIÊN ===
        // Quét rộng thêm để gom bọn đang "chạy tại chỗ", reset path và ép tìm đường mới
        Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, pushRadius + 3f, enemyLayer);
        foreach (Collider enemyCollider in nearbyEnemies)
        {
            NavMeshAgent agent = enemyCollider.GetComponentInParent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.SetDestination(transform.root.position);
            }
        }
        // ==========================================================
    }

    void Start()
    {
        time = Time.time;
        points = new Vector4[30];
        Hitpoints = new List<Vector4>();
        m_meshRenderer = GetComponent<MeshRenderer>();
        m_material = m_meshRenderer.material;
    }

    void Update()
    {
        m_material.SetVectorArray("_Points", points);

        Hitpoints = Hitpoints
        .Select(s => new Vector4(s.x, s.y, s.z, s.w + Time.deltaTime / ImpactLife))
        .Where(w => w.w <= 1).ToList();

        if (Time.time > time + 0.1f)
        {
            time = Time.time;
            AddEmpty();
        }

        Hitpoints.ToArray().CopyTo(points, 0);
    }

    private void PushEnemiesContinuously()
    {
        Collider[] enemiesInRadius = Physics.OverlapSphere(transform.position, pushRadius, enemyLayer);

        foreach (Collider enemyCollider in enemiesInRadius)
        {
            NavMeshAgent agent = enemyCollider.GetComponentInParent<NavMeshAgent>();
            Rigidbody rb = enemyCollider.GetComponentInParent<Rigidbody>();

            if (rb != null)
            {
                if (agent != null && agent.enabled)
                {
                    int id = agent.GetInstanceID();
                    if (!_stunnedEnemies.ContainsKey(id))
                    {
                        // Lưu vào danh sách bảo hiểm
                        _stunnedEnemies.Add(id, (agent, rb));
                        StartCoroutine(TemporarilyDisableAgent(agent, rb, id));
                    }
                }

                // Tính hướng hất
                Vector3 pushDirection = rb.position - transform.position;
                pushDirection.y = 0f; 

                if (pushDirection.sqrMagnitude > 0.01f)
                {
                    pushDirection = pushDirection.normalized;
                    rb.isKinematic = false;
                    
                    // === SỬA LỖI 1: GÁN THẲNG VẬN TỐC ===
                    // Giữ nguyên gia tốc rơi (trục Y), chỉ can thiệp lực đẩy ngang (X, Z)
                    rb.linearVelocity = new Vector3(
                        pushDirection.x * pushForce, 
                        rb.linearVelocity.y, 
                        pushDirection.z * pushForce
                    );
                    // ====================================
                }
            }
        }
    }

    private IEnumerator TemporarilyDisableAgent(NavMeshAgent agent, Rigidbody rb, int agentId)
    {
        if (agent != null) agent.enabled = false;
        
        yield return new WaitForSeconds(stunDuration);

        // Chỉ phục hồi nếu nó chưa được OnDisable() giải cứu trước đó
        if (_stunnedEnemies.ContainsKey(agentId))
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            if (agent != null && agent.gameObject.activeInHierarchy)
            {
                if (NavMesh.SamplePosition(agent.transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
                agent.enabled = true;
                
                // Ép quái xác định lại mục tiêu ngay lập tức (tránh đứng chờ player di chuyển)
                if (agent.isOnNavMesh)
                {
                    agent.SetDestination(transform.root.position);
                }
            }

            _stunnedEnemies.Remove(agentId);
        }
    }

    public void AddHitObject(Vector3 position) { /* Giữ nguyên */ }
    public void AddEmpty() { /* Giữ nguyên */ }

    private void OnDestroy()
    {
        IsShieldActive = false;
    }
}