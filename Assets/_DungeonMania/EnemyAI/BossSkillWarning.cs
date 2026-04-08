using UnityEngine;

/// <summary>
/// Hiển thị vùng cảnh cáo (đỏ) trên mặt đất trước khi boss dùng skill.
/// Tự tạo mesh + material tại runtime, không cần prefab/texture.
/// 
/// 2 chế độ:
/// - STATIC: spawn tại vị trí cố định, fade in + flash
/// - TRACKING: theo dõi boss + hướng về player, mở rộng dần (fill up)
/// </summary>
public class BossSkillWarning : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private Material warningMaterial;
    private float duration;
    private float elapsed = 0f;
    private LineRenderer edgeRing;
    
    // === TRACKING MODE ===
    private Transform sourceTransform; // Boss
    private Transform targetTransform; // Player
    private bool isTracking = false;

    // ===================== STATIC SPAWN =====================
    
    public static BossSkillWarning SpawnCircle(Vector3 center, float radius, float warningDuration = 1.2f)
    {
        GameObject go = new GameObject("SkillWarning_Circle");
        go.transform.position = center + Vector3.up * 0.06f;
        
        BossSkillWarning warning = go.AddComponent<BossSkillWarning>();
        warning.duration = warningDuration;
        warning.CreateCircleMesh(radius);
        warning.CreateEdgeRing(radius, 48, false, Vector3.forward, 360f);
        warning.CreateMaterial();
        
        return warning;
    }
    
    public static BossSkillWarning SpawnCone(Vector3 center, Vector3 forward, float radius, float angle, float warningDuration = 1.2f)
    {
        GameObject go = new GameObject("SkillWarning_Cone");
        go.transform.position = center + Vector3.up * 0.06f;
        
        BossSkillWarning warning = go.AddComponent<BossSkillWarning>();
        warning.duration = warningDuration;
        warning.CreateConeMesh(radius, angle, Vector3.forward); // LOCAL Z+
        warning.CreateEdgeRing(radius, 32, true, Vector3.forward, angle);
        warning.CreateMaterial();
        
        // Xoay transform theo hướng
        forward.y = 0;
        if (forward.sqrMagnitude > 0.001f)
            go.transform.rotation = Quaternion.LookRotation(forward);
        
        return warning;
    }

    // ===================== TRACKING SPAWN =====================
    
    /// <summary>
    /// Spawn AoE warning theo dõi boss, mở rộng dần
    /// </summary>
    public static BossSkillWarning SpawnCircleTracking(Transform source, float radius, float warningDuration)
    {
        GameObject go = new GameObject("SkillWarning_Circle_Track");
        go.transform.position = source.position + Vector3.up * 0.06f;
        
        BossSkillWarning warning = go.AddComponent<BossSkillWarning>();
        warning.duration = warningDuration;
        warning.sourceTransform = source;
        warning.isTracking = true;
        
        warning.CreateCircleMesh(radius);
        warning.CreateEdgeRing(radius, 48, false, Vector3.forward, 360f);
        warning.CreateMaterial();
        
        go.transform.localScale = new Vector3(0.1f, 1f, 0.1f); // Bắt đầu nhỏ
        return warning;
    }
    
    /// <summary>
    /// Spawn cone warning theo dõi boss + hướng về player, mở rộng dần
    /// </summary>
    public static BossSkillWarning SpawnConeTracking(Transform source, Transform target, float radius, float angle, float warningDuration)
    {
        GameObject go = new GameObject("SkillWarning_Cone_Track");
        go.transform.position = source.position + Vector3.up * 0.06f;
        
        BossSkillWarning warning = go.AddComponent<BossSkillWarning>();
        warning.duration = warningDuration;
        warning.sourceTransform = source;
        warning.targetTransform = target;
        warning.isTracking = true;
        
        // Mesh hướng LOCAL Z+ → transform.rotation xử lý hướng thực tế
        warning.CreateConeMesh(radius, angle, Vector3.forward);
        warning.CreateEdgeRing(radius, 32, true, Vector3.forward, angle);
        warning.CreateMaterial();
        
        // Hướng ban đầu về player
        Vector3 dir = target.position - source.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
            go.transform.rotation = Quaternion.LookRotation(dir);
        
        go.transform.localScale = new Vector3(0.1f, 1f, 0.1f); // Bắt đầu nhỏ
        return warning;
    }

    // ===================== MESH CREATION =====================
    
    void CreateCircleMesh(float radius)
    {
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        
        int segments = 48;
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];
        Color[] colors = new Color[segments + 1];
        
        vertices[0] = Vector3.zero;
        colors[0] = new Color(1f, 0f, 0f, 0.12f);
        
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            colors[i + 1] = new Color(1f, 0.15f, 0f, 0.45f);
        }
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments + 1;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = next;
            triangles[i * 3 + 2] = i + 1;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mf.mesh = mesh;
    }
    
    void CreateConeMesh(float radius, float angle, Vector3 forward)
    {
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        
        int segments = 32;
        float halfAngle = angle / 2f;
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];
        Color[] colors = new Color[segments + 2];
        
        forward.y = 0;
        forward.Normalize();
        
        vertices[0] = Vector3.zero;
        colors[0] = new Color(1f, 0f, 0f, 0.12f);
        
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float currentAngle = -halfAngle + t * angle;
            Vector3 dir = Quaternion.Euler(0, currentAngle, 0) * forward;
            vertices[i + 1] = dir * radius;
            colors[i + 1] = new Color(1f, 0.15f, 0f, 0.45f);
        }
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 2;
            triangles[i * 3 + 2] = i + 1;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mf.mesh = mesh;
    }
    
    void CreateEdgeRing(float radius, int segments, bool isCone, Vector3 forward, float angle)
    {
        GameObject ringObj = new GameObject("EdgeRing");
        ringObj.transform.SetParent(transform);
        ringObj.transform.localPosition = Vector3.zero;
        ringObj.transform.localRotation = Quaternion.identity;
        ringObj.transform.localScale = Vector3.one;
        
        edgeRing = ringObj.AddComponent<LineRenderer>();
        edgeRing.useWorldSpace = false;
        edgeRing.loop = !isCone;
        edgeRing.widthMultiplier = 0.1f;
        
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        Material lineMat = new Material(shader);
        lineMat.color = new Color(1f, 0f, 0f, 0.8f);
        edgeRing.material = lineMat;
        edgeRing.startColor = new Color(1f, 0.1f, 0f, 0f);
        edgeRing.endColor = new Color(1f, 0.1f, 0f, 0f);
        
        if (isCone)
        {
            forward.y = 0;
            forward.Normalize();
            float halfAngle = angle / 2f;
            edgeRing.positionCount = segments + 3;
            edgeRing.SetPosition(0, Vector3.zero);
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float a = -halfAngle + t * angle;
                Vector3 dir = Quaternion.Euler(0, a, 0) * forward;
                edgeRing.SetPosition(i + 1, dir * radius);
            }
            edgeRing.SetPosition(segments + 2, Vector3.zero);
        }
        else
        {
            edgeRing.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                edgeRing.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius));
            }
        }
    }
    
    void CreateMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        
        warningMaterial = new Material(shader);
        warningMaterial.color = Color.white;
        warningMaterial.renderQueue = 3001;
        
        if (meshRenderer != null)
        {
            meshRenderer.material = warningMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }
    }

    // ===================== UPDATE =====================
    
    void Update()
    {
        elapsed += Time.deltaTime;
        
        if (elapsed >= duration)
        {
            Cleanup();
            return;
        }
        
        float t = elapsed / duration;
        
        // === TRACKING MODE ===
        if (isTracking)
        {
            // Theo dõi vị trí boss
            if (sourceTransform != null)
                transform.position = sourceTransform.position + Vector3.up * 0.06f;
            
            // Xoay hướng về player (cho cone)
            if (targetTransform != null && sourceTransform != null)
            {
                Vector3 dir = targetTransform.position - sourceTransform.position;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir);
            }
            
            // Mở rộng dần (fill up effect)
            float scale = Mathf.Lerp(0.1f, 1f, t);
            transform.localScale = new Vector3(scale, 1f, scale);
        }
        
        // === ALPHA ANIMATION ===
        float alpha;
        if (isTracking)
        {
            // Tracking: alpha tăng dần theo fill
            alpha = Mathf.Lerp(0.3f, 1f, t);
            // Flash mạnh ở 90% cuối (sắp bắn!)
            if (t > 0.85f)
                alpha = 0.8f + Mathf.Sin((t - 0.85f) / 0.15f * Mathf.PI * 6f) * 0.2f;
        }
        else
        {
            // Static: fade in + flash
            if (t < 0.6f)
                alpha = Mathf.Lerp(0f, 1f, t / 0.6f);
            else
            {
                float norm = (t - 0.6f) / 0.4f;
                alpha = 0.7f + Mathf.Sin(norm * Mathf.PI * 8f) * 0.3f;
            }
        }
        
        if (warningMaterial != null)
            warningMaterial.color = new Color(1f, 1f, 1f, alpha);
        
        if (edgeRing != null)
        {
            Color ringColor = new Color(1f, 0.1f, 0f, alpha * 0.9f);
            edgeRing.startColor = ringColor;
            edgeRing.endColor = ringColor;
            edgeRing.widthMultiplier = 0.1f + Mathf.Sin(t * Mathf.PI * 6f) * 0.04f;
        }
    }
    
    void Cleanup()
    {
        if (warningMaterial != null) Destroy(warningMaterial);
        if (edgeRing != null && edgeRing.material != null) Destroy(edgeRing.material);
        Destroy(gameObject);
    }
    
    void OnDestroy()
    {
        if (warningMaterial != null) Destroy(warningMaterial);
    }
}
