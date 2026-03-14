namespace ModularFootstepSystem.Extensions
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Pool;

    /// <summary>
    /// Extended pool as a unity behaviour class.
    /// </summary>
    public class ExtendedBehaviourPool : MonoBehaviour, IExtendedPool
    {
        public GameObject Get
        {
            get
            {
                if (isReusedActiveObjects && activeObjects.Count >= maxSize)
                {
                    activeObjects.Add(activeObjects[0]);
                    activeObjects.RemoveAt(0);
                    return activeObjects[^1];
                }
                else
                {
                    return pool.Get();
                }
            }
        }

        public IReadOnlyList<GameObject> ActiveObjects => activeObjects;

        [SerializeField]
        protected GameObject prefab = default;

        [SerializeField, Min(1)]
        protected int defaultCapacity = 10;
        [SerializeField, Min(1)]
        protected int maxSize = 50;

        [SerializeField]
        protected bool isReusedActiveObjects = false;

        protected ObjectPool<GameObject> pool = null;

        protected List<GameObject> activeObjects = new List<GameObject>();

        protected PooledObject pooledObject = default;

        protected GameObject createdObject = default;

        protected virtual void Awake()
        {
            pool = new ObjectPool<GameObject>
            (
                () =>
                {
                    createdObject = Instantiate(prefab);

                    if (createdObject.TryGetComponent(out pooledObject))
                    {
                        pooledObject.Initialize(this);
                    }
                    else
                    {
                        createdObject.AddComponent<PooledObject>().Initialize(this);
                    }

                    return createdObject;
                },
                (createdObject) =>
                {
                    createdObject.gameObject.SetActive(true);
                    activeObjects.Add(createdObject);
                },
                (createdObject) =>
                {
                    createdObject.gameObject.SetActive(false);
                    activeObjects.Remove(createdObject);
                },
                (createdObject) => Destroy(createdObject),
                true,
                defaultCapacity,
                maxSize
            );
        }

        public bool TryGet<T>(out T pooledObject) where T : Behaviour
        {
            pooledObject = Get.GetComponent<T>();

            if (pooledObject == null)
            {
                pool.Release(pooledObject.gameObject);
                return false;
            }
            else
            {
                return true;
            }
        }

        public void Release(GameObject releasingObject)
        {
            if (releasingObject.TryGetComponent(out pooledObject))
            {
                if (pooledObject.Pool == (IExtendedPool)this)
                {
                    pool.Release(releasingObject);
                }
                else
                {
                    Debug.LogWarning($"It was not possible to return the object to the pool. The returned object is not from this pool.");
                }
            }
            else
            {
                Debug.LogWarning($"It was not possible to return the object to the pool. The returned object does not contain a component {nameof(PooledObject)}.");
            }
        }
    }
}