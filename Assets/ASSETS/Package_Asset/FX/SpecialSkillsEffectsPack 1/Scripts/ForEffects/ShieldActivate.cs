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
    
    // ── SHIELD STATE (Global flag for PlayerHealth) ──────────────────────────
    public static bool IsShieldActive { get; private set; }
    
    public static void ForceReset() => IsShieldActive = false;
    
    // ──────────────────────────────────────────────────────────────────────────

    void OnEnable()
    {
        IsShieldActive = true;
        StartCoroutine(PushEnemiesOutSmoothly());
    }

    IEnumerator PushEnemiesOutSmoothly()
    {
        var obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
        if (obstacle != null) obstacle.carving = false;
        
        float disableDuration = 0.8f; // Thời gian bay trên không và rơi xuống đất
        
        List<UnityEngine.AI.NavMeshAgent> agents = new List<UnityEngine.AI.NavMeshAgent>();
        List<Rigidbody> rbs = new List<Rigidbody>();
        List<bool> originalKinematicState = new List<bool>();
        
        Collider[] hits = Physics.OverlapSphere(transform.position, 2.8f);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy") || hit.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                UnityEngine.AI.NavMeshAgent agent = hit.GetComponent<UnityEngine.AI.NavMeshAgent>();
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                
                // Tắt agent và chuẩn bị hất văng
                if (agent != null && rb != null && agent.enabled)
                {
                    agent.enabled = false; 
                    agents.Add(agent);
                    
                    rbs.Add(rb);
                    originalKinematicState.Add(rb.isKinematic);
                    
                    // Bật vật lý để nhận lực
                    rb.isKinematic = false;
                    
                    // Hất văng (Lực nổ từ tâm, bán kính 3.5m, hơi hất nhẹ lên trên 0.5m)
                    rb.AddExplosionForce(1200f, transform.position, 3.5f, 0.5f);
                }
            }
        }
        
        // Chờ quái văng xong và rớt xuống
        yield return new WaitForSeconds(disableDuration);
        
        for (int i = 0; i < agents.Count; i++)
        {
            if (agents[i] != null && rbs[i] != null)
            {
                // Stop mọi chuyển động vật lý tàn dư
                rbs[i].linearVelocity = Vector3.zero;
                rbs[i].angularVelocity = Vector3.zero;
                rbs[i].isKinematic = originalKinematicState[i];
                
                // Ép rớt xuống Navmesh an toàn tránh lọt map
                UnityEngine.AI.NavMeshHit navHit;
                if (UnityEngine.AI.NavMesh.SamplePosition(agents[i].transform.position, out navHit, 3.0f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    agents[i].transform.position = navHit.position;
                }
                
                agents[i].enabled = true; 
            }
        }
        
        if (obstacle != null) obstacle.carving = true; 
    }

    void OnDisable()
    {
        // Chỉ reset nếu không còn ShieldActivate nào khác đang active (trường hợp nhiều shield)
        // Tuy nhiên thường shield player chỉ có 1 cái active.
        // Để an toàn, ta có thể dùng counter hoặc đơn giản là check scene.
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

    private void OnDestroy()
    {
        IsShieldActive = false;
    }
}
