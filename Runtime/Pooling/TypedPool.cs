using System;
using UnityEngine;

namespace Nexus.Pooling
{
    /// <summary>
    /// A strongly-typed pool that manages objects of a specific type
    /// </summary>
    public class TypedPool<T> where T : Component
    {
        private readonly IPoolingService _poolingService;
        private readonly GameObject _prefab;
        private readonly string _poolId;
        
        public TypedPool(IPoolingService poolingService, GameObject prefab)
        {
            _poolingService = poolingService;
            _prefab = prefab;
            
            // Validate prefab has required component
            if (!_prefab.GetComponent<T>())
                throw new ArgumentException($"Prefab must have component {typeof(T).Name}");
        }
        
        public TypedPool(IPoolingService poolingService, string poolId)
        {
            _poolingService = poolingService;
            _poolId = poolId;
        }
        
        public T Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj;
            if (_prefab != null)
                obj = _poolingService.GetFromPool(_prefab, position, rotation);
            else
                obj = _poolingService.GetFromPool(_poolId, position, rotation);
                
            if (obj == null)
                return null;
                
            return obj.GetComponent<T>();
        }
    }

}