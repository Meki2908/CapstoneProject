namespace ModularFootstepSystem.Extensions
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Pool;

    /// <summary>
    /// Extended pool as a CSharp class.
    /// </summary>
    public class ExtendedSharpPool : IExtendedPool
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

        public virtual IReadOnlyList<GameObject> ActiveObjects => activeObjects;

        protected ObjectPool<GameObject> pool = null;

        protected List<GameObject> activeObjects = new List<GameObject>();

        protected int maxSize = 0;

        protected bool isReusedActiveObjects = false;

        protected PooledObject pooledObject = default;

        protected GameObject createdObject = default;

        /// <summary>
        /// A constructor that initializes the pool.
        /// </summary>
        /// <param name="prefab">Prefab of created objects.</param>
        /// <param name="defaultCapacity">Default capacity.</param>
        /// <param name="_maxSize">Maximum size.</param>
        /// <param name="_isReusedActiveObjects">Reuse active objects.</param>
        public ExtendedSharpPool(GameObject prefab, int defaultCapacity, int _maxSize, bool _isReusedActiveObjects = false)
        {
            maxSize = _maxSize;
            isReusedActiveObjects = _isReusedActiveObjects;

            pool = new ObjectPool<GameObject>
            (
                () =>
                {
                    createdObject = Object.Instantiate(prefab);

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
                    createdObject.SetActive(true);
                    activeObjects.Add(createdObject);
                },
                (createdObject) =>
                {
                    createdObject.SetActive(false);
                    activeObjects.Remove(createdObject);
                },
                (createdObject) => Object.Destroy(createdObject),
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