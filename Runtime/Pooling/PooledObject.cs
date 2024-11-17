using UnityEngine;
using UnityEngine.Pool;

namespace Nexus.Pooling
{
    public class PooledObject : MonoBehaviour
    {
        private IObjectPool<GameObject> pool;
        private float spawnTime;
        private float timeout = -1f;
        
        private bool _destroyed;

        public void Initialize(IObjectPool<GameObject> objectPool, float recycleTimeout = -1f)
        {
            pool = objectPool;
            timeout = recycleTimeout;
            spawnTime = Time.time;
            _destroyed = false;
        }

        public void ReturnToPool()
        {
            if (_destroyed)
            {
                return;
            }
            _destroyed = true;
            
            if (pool != null)
            {
                pool.Release(gameObject);
                return;
            }
            
            Destroy(gameObject);
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