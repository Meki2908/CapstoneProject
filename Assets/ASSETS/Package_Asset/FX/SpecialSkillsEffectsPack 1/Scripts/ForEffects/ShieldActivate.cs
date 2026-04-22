using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI; // Thêm thư viện này để dùng NavMeshAgent

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
    public float pushRadius = 3f; // Bán kính đẩy quái
    public float pushForce = 15f; // Lực hất văng
    public LayerMask enemyLayer;  // Layer của quái (nhớ set trong Inspector)
    public float stunDuration = 0.5f; // Thời gian quái bị choáng/văng ra trước khi bò dậy đi tiếp
    
    // ── SHIELD STATE (Global flag for PlayerHealth) ──────────────────────────
    public static bool IsShieldActive { get; private set; }
    
    public static void ForceReset() => IsShieldActive = false;
    
    // ──────────────────────────────────────────────────────────────────────────

    void OnEnable()
    {
        IsShieldActive = true;
        // KHÔNG gọi đẩy 1 lần ở đây nữa
    }

    void FixedUpdate() // Chạy liên tục song song với khung hình vật lý
    {
        PushEnemiesContinuously();
    }

    void OnDisable()
    {
        IsShieldActive = false;
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
        // ... (Giữ nguyên code phần update Shader của bạn) ...
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

    // --- THÊM LOGIC ĐẨY QUÁI LIÊN TỤC Ở ĐÂY ---
    private void PushEnemiesContinuously()
    {
        // Liên tục quét những kẻ xâm nhập
        Collider[] enemiesInRadius = Physics.OverlapSphere(transform.position, pushRadius, enemyLayer);

        foreach (Collider enemyCollider in enemiesInRadius)
        {
            NavMeshAgent agent = enemyCollider.GetComponentInParent<NavMeshAgent>();
            Rigidbody rb = enemyCollider.GetComponentInParent<Rigidbody>();

            if (rb != null)
            {
                // Tạm tắt AI để nó không cưỡng lại vật lý
                if (agent != null && agent.enabled)
                {
                    StartCoroutine(TemporarilyDisableAgent(agent));
                }

                // Tính hướng từ tâm khiên đẩy ra
                Vector3 pushDirection = (rb.position - transform.position).normalized;
                pushDirection.y = 0.2f; // Ép một chút lực hất lên trời để quái lùi dễ hơn

                // Áp dụng lực đẩy liên tục (ForceMode.Force) thay vì Impulse
                rb.isKinematic = false;
                rb.AddForce(pushDirection * pushForce, ForceMode.Force);
            }
        }
    }

    // Coroutine nhỏ để bật lại AI sau khi quái bị văng ra khỏi khiên 1 thời gian ngắn
    private IEnumerator TemporarilyDisableAgent(NavMeshAgent agent)
    {
        agent.enabled = false;
        yield return new WaitForSeconds(stunDuration); // Dùng stunDuration thay vì fix cứng 0.5f
        if (agent != null && agent.gameObject.activeInHierarchy)
        {
            agent.enabled = true;
        }
    }

    public void AddHitObject(Vector3 position)
    {
        // ... (Giữ nguyên)
    }

    public void AddEmpty()
    {
        // ... (Giữ nguyên)
    }

    private void OnDestroy()
    {
        IsShieldActive = false;
    }
}