using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultipleObjectsMake : _ObjectsMakeBase
{
    [Header("Pooling")]
    [SerializeField] private bool usePooling = false;
    [SerializeField] private int prewarmCountPerPrefab = 2;
    [SerializeField] private int maxPoolSizePerPrefab = 50;

    public float m_startDelay;
    public int m_makeCount;
    public float m_makeDelay;
    public Vector3 m_randomPos;
    public Vector3 m_randomRot;
    public Vector3 m_randomScale;
    public bool isObjectAttachToParent = true;

    float m_Time;
    float m_Time2;
    float m_delayTime;
    float m_count;
    float m_scalefactor;

    private static readonly Dictionary<int, Queue<GameObject>> s_poolByPrefab = new Dictionary<int, Queue<GameObject>>();
    private static readonly Dictionary<GameObject, int> s_instanceToPrefab = new Dictionary<GameObject, int>();
    private static readonly Dictionary<int, int> s_maxPoolSizeByPrefab = new Dictionary<int, int>();

    void Start()
    {
        m_Time = m_Time2 = Time.time;
        m_scalefactor = VariousEffectsScene.m_gaph_scenesizefactor; //transform.parent.localScale.x; 
        PrewarmPools();
    }


    void Update()
    {
        if (Time.time > m_Time + m_startDelay)
        {
            if (Time.time > m_Time2 + m_makeDelay && m_count < m_makeCount)
            {
                Vector3 m_pos = transform.position + GetRandomVector(m_randomPos)* m_scalefactor; 
                Quaternion m_rot = transform.rotation * Quaternion.Euler(GetRandomVector(m_randomRot));
                

                for (int i = 0; i < m_makeObjs.Length; i++)
                {
                    if (m_makeObjs[i] == null) continue;

                    GameObject m_obj = SpawnObject(m_makeObjs[i], m_pos, m_rot);
                    Vector3 m_scale = (m_makeObjs[i].transform.localScale + GetRandomVector2(m_randomScale));
                    if (isObjectAttachToParent)
                        m_obj.transform.SetParent(this.transform);
                    else
                        m_obj.transform.SetParent(null);
                    m_obj.transform.localScale = m_scale;
                }

                m_Time2 = Time.time;
                m_count++;
            }
        }
    }

    private void PrewarmPools()
    {
        int prewarm = Mathf.Max(0, prewarmCountPerPrefab);
        if (prewarm == 0 || m_makeObjs == null) return;

        for (int i = 0; i < m_makeObjs.Length; i++)
        {
            GameObject prefab = m_makeObjs[i];
            if (prefab == null) continue;
            if (!ShouldPoolPrefab(prefab)) continue;

            int prefabId = prefab.GetInstanceID();
            EnsurePool(prefab, prefabId);

            Queue<GameObject> pool = s_poolByPrefab[prefabId];
            while (pool.Count < prewarm)
            {
                GameObject instance = Instantiate(prefab, transform.position, transform.rotation);
                instance.SetActive(false);
                instance.transform.SetParent(null);
                RegisterInstance(instance, prefabId);
                pool.Enqueue(instance);
            }
        }
    }

    private GameObject SpawnObject(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (!ShouldPoolPrefab(prefab))
        {
            return Instantiate(prefab, pos, rot);
        }

        int prefabId = prefab.GetInstanceID();
        EnsurePool(prefab, prefabId);
        Queue<GameObject> pool = s_poolByPrefab[prefabId];

        GameObject instance = null;
        while (pool.Count > 0 && instance == null)
        {
            instance = pool.Dequeue();
        }

        if (instance == null)
        {
            instance = Instantiate(prefab, pos, rot);
            RegisterInstance(instance, prefabId);
        }
        else
        {
            instance.transform.SetPositionAndRotation(pos, rot);
            instance.SetActive(true);
        }

        // Restart particle systems when reusing pooled VFX
        ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            systems[i].Clear(true);
            systems[i].Play(true);
        }

        return instance;
    }

    private static void EnsurePool(GameObject prefab, int prefabId)
    {
        if (!s_poolByPrefab.ContainsKey(prefabId))
        {
            s_poolByPrefab[prefabId] = new Queue<GameObject>();
        }
    }

    private bool ShouldPoolPrefab(GameObject prefab)
    {
        if (prefab == null) return false;
        if (usePooling) return true;

        // Auto-optimize only Meteor by default to avoid changing behavior globally.
        return prefab.name.Contains("Effect_49_Meteor");
    }

    private void RegisterInstance(GameObject instance, int prefabId)
    {
        if (instance == null) return;
        s_instanceToPrefab[instance] = prefabId;
        s_maxPoolSizeByPrefab[prefabId] = Mathf.Max(1, maxPoolSizePerPrefab);
    }

    public static bool TryReturnToPool(GameObject instance)
    {
        if (instance == null) return false;
        if (!s_instanceToPrefab.TryGetValue(instance, out int prefabId)) return false;

        if (!s_poolByPrefab.TryGetValue(prefabId, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            s_poolByPrefab[prefabId] = pool;
        }

        int maxPoolSize = s_maxPoolSizeByPrefab.TryGetValue(prefabId, out int configuredSize) ? configuredSize : 50;
        if (pool.Count >= maxPoolSize)
        {
            s_instanceToPrefab.Remove(instance);
            Destroy(instance);
            return true;
        }

        instance.SetActive(false);
        instance.transform.SetParent(null);
        pool.Enqueue(instance);
        return true;
    }
}
