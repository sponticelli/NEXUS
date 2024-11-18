using System;
using Nexus.Core.ServiceLocation;
using UnityEngine;

namespace Nexus.Pooling
{
    /// <summary>
    /// A serializable reference to a pool that can be configured in the Unity Inspector.
    /// Provides type-safe access to pool operations.
    /// </summary>
    [Serializable]
    public class PoolReference : ISerializationCallbackReceiver
    {
        [SerializeField] private string poolId;
        [SerializeField] private GameObject prefab;
        
        private IPoolingService _poolingService;
        
        // Runtime state - not serialized
        private bool _isInitialized;
        
        /// <summary>
        /// Gets an object from the pool
        /// </summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            if (!EnsureInitialized())
                return null;
                
            if (prefab != null)
                return _poolingService.GetFromPool(prefab, position, rotation);
                
            if (!string.IsNullOrEmpty(poolId))
                return _poolingService.GetFromPool(poolId, position, rotation);
                
            Debug.LogError("PoolReference has neither prefab nor pool ID configured");
            return null;
        }
        
        private bool EnsureInitialized()
        {
            if (_isInitialized)
                return true;
                
            _poolingService = ServiceLocator.Instance.GetService<IPoolingService>();
            if (_poolingService == null)
            {
                Debug.LogError("PoolingService not found");
                return false;
            }
            
            _isInitialized = true;
            return true;
        }

        // ISerializationCallbackReceiver implementation
        public void OnBeforeSerialize()
        {
            // Clear runtime state
            _isInitialized = false;
            _poolingService = null;
        }

        public void OnAfterDeserialize() { }
        
        public int GetHashCode()
        {
            if (prefab != null)
                return prefab.GetHashCode();
            
            if (!string.IsNullOrEmpty(poolId))
                return poolId.GetHashCode();
            
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// A type-safe pool reference that ensures the spawned object has a specific component
    /// </summary>
    [Serializable]
    public class PoolReference<T> : ISerializationCallbackReceiver where T : Component
    {
        [SerializeField] private PoolReference baseReference = new PoolReference();
        
        public T Get(Vector3 position, Quaternion rotation)
        {
            var obj = baseReference.Get(position, rotation);
            if (obj == null)
                return null;
                
            var component = obj.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"Pooled object does not have required component {typeof(T).Name}");
                return null;
            }
            
            return component;
        }
        
        // ISerializationCallbackReceiver implementation
        public void OnBeforeSerialize() => baseReference.OnBeforeSerialize();
        public void OnAfterDeserialize() => baseReference.OnAfterDeserialize();
        
        public int GetHashCode()
        {
            return baseReference.GetHashCode();
        }
    }

}