using UnityEngine;
using UnityEngine.Pool;

namespace Nexus.Pooling
{
    public class PooledObject : MonoBehaviour
    {
        private IObjectPool<GameObject> pool;
        private float spawnTime;
        private float timeout = -1f;

        public void Initialize(IObjectPool<GameObject> objectPool, float recycleTimeout = -1f)
        {
            pool = objectPool;
            timeout = recycleTimeout;
            spawnTime = Time.time;
        }

        public void ReturnToPool()
        {
            if (pool != null)
            {
                pool.Release(gameObject);
            }
            else
            {
                Debug.LogWarning($"PooledObject {gameObject.name} has no associated pool!");
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (timeout > 0 && Time.time - spawnTime >= timeout)
            {
                ReturnToPool();
            }
        }
    }
}