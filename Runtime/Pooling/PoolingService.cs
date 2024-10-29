using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexus.Core.ServiceLocation;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.Pool;

namespace Nexus.Pooling
{
    [ServiceImplementation]
    public class PoolingService : MonoBehaviour, IPoolingService, IConfigurable<PoolingServiceConfig>
    {
        private readonly Dictionary<int, PoolInfo> pools = new Dictionary<int, PoolInfo>();
        private readonly Dictionary<string, int> idToPoolId = new Dictionary<string, int>();
        private readonly object poolLock = new object();
        private PoolingServiceConfig config;
        private bool isInitialized;
        private TaskCompletionSource<bool> initializationTcs;

        private class PoolInfo
        {
            public IObjectPool<GameObject> Pool { get; set; }
            public PoolConfiguration Config { get; set; }
            public int ActiveCount { get; set; }
            public int TotalCreated { get; set; }
            public Transform Container { get; set; }
        }

        public void Configure(PoolingServiceConfig configuration)
        {
            config = configuration;
        }

        public bool IsInitialized => isInitialized;

        public async Task InitializeAsync()
        {
            if (isInitialized) return;

            initializationTcs = new TaskCompletionSource<bool>();

            try
            {
                if (config == null)
                {
                    throw new InvalidOperationException("PoolingService not configured!");
                }

                // Validate pool configurations
                ValidatePoolConfigurations();

                // Create pools
                foreach (var poolConfig in config.Configurations)
                {
                    CreatePool(poolConfig);
                }

                isInitialized = true;
                initializationTcs.SetResult(true);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize PoolingService: {ex}");
                initializationTcs.SetException(ex);
                throw;
            }
        }
        
        private void ValidatePoolConfigurations()
        {
            config.Validate();
        }

        public Task WaitForInitialization()
        {
            return initializationTcs?.Task ?? Task.CompletedTask;
        }

        private void CreatePool(PoolConfiguration poolConfig)
        {
            if (poolConfig.prefab == null)
            {
                Debug.LogError($"Cannot create pool for null prefab with ID {poolConfig.id}!");
                return;
            }

            var poolId = poolConfig.prefab.GetInstanceID();
            
            if (pools.ContainsKey(poolId))
            {
                Debug.LogWarning($"Pool for prefab {poolConfig.prefab.name} already exists!");
                return;
            }

            if (idToPoolId.ContainsKey(poolConfig.id))
            {
                Debug.LogError($"Pool with ID {poolConfig.id} already exists!");
                return;
            }

            // Create container GameObject for this pool
            var containerName = string.IsNullOrEmpty(poolConfig.id) 
                ? $"Pool-{poolConfig.prefab.name}" 
                : $"Pool-{poolConfig.id}";
            var containerObject = new GameObject(containerName);
            containerObject.transform.SetParent(transform);

            var poolInfo = new PoolInfo
            {
                Config = poolConfig,
                ActiveCount = 0,
                TotalCreated = 0,
                Container = containerObject.transform
            };

            var pool = new UnityEngine.Pool.ObjectPool<GameObject>(
                createFunc: () =>
                {
                    poolInfo.TotalCreated++;
                    return CreatePooledObject(poolConfig, poolInfo.Container);
                },
                actionOnGet: obj =>
                {
                    poolInfo.ActiveCount++;
                    OnObjectRetrieved(obj);
                },
                actionOnRelease: obj =>
                {
                    poolInfo.ActiveCount--;
                    OnObjectReturned(obj, poolInfo.Container);
                },
                actionOnDestroy: obj => Destroy(obj),
                collectionCheck: true,
                defaultCapacity: poolConfig.initialSize,
                maxSize: poolConfig.maxSize
            );

            poolInfo.Pool = pool;

            // Pre-instantiate initial pool size
            var warmupObjects = new List<GameObject>();
            for (int i = 0; i < poolConfig.initialSize; i++)
            {
                warmupObjects.Add(pool.Get());
            }
            foreach (var obj in warmupObjects)
            {
                pool.Release(obj);
            }

            pools[poolId] = poolInfo;
            idToPoolId[poolConfig.id] = poolId;
            
            // Update container with current pool status
            UpdateContainerName(poolInfo);
            
            Debug.Log($"Created pool {poolConfig.id} for {poolConfig.prefab.name} with initial size {poolConfig.initialSize}");
        }

        private GameObject CreatePooledObject(PoolConfiguration config, Transform container)
        {
            var obj = Instantiate(config.prefab, container);
            var pooledObj = obj.GetComponent<PooledObject>();
            
            if (pooledObj == null)
            {
                pooledObj = obj.AddComponent<PooledObject>();
            }

            obj.SetActive(false);
            return obj;
        }

        private void OnObjectRetrieved(GameObject obj)
        {
            if (obj == null) return;
            
            // When retrieved, the object should be at root level for proper scene organization
            obj.transform.SetParent(null);
            obj.SetActive(true);
        }

        private void OnObjectReturned(GameObject obj, Transform container)
        {
            if (obj == null) return;
            obj.SetActive(false);
            obj.transform.SetParent(container);
        }

        private void UpdateContainerName(PoolInfo poolInfo)
        {
            var baseName = string.IsNullOrEmpty(poolInfo.Config.id) 
                ? $"Pool-{poolInfo.Config.prefab.name}" 
                : $"Pool-{poolInfo.Config.id}";
            
            poolInfo.Container.name = $"{baseName} ({poolInfo.ActiveCount}/{poolInfo.TotalCreated})";
        }

        public GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException("PoolingService not initialized!");
            }

            var poolId = prefab.GetInstanceID();
            
            lock (poolLock)
            {
                if (!pools.TryGetValue(poolId, out var poolInfo))
                {
                    Debug.LogError($"No pool found for prefab {prefab.name}!");
                    return null;
                }

                GameObject obj = null;
                try
                {
                    if (poolInfo.ActiveCount >= poolInfo.Config.maxSize)
                    {
                        if (!poolInfo.Config.autoExpand)
                        {
                            Debug.LogWarning($"Pool for {prefab.name} is at maximum capacity ({poolInfo.Config.maxSize}) and autoExpand is disabled!");
                            return null;
                        }
                        
                        // If autoExpand is true, increase maxSize by 50% (rounded up)
                        int increase = Mathf.CeilToInt(poolInfo.Config.maxSize * 0.5f);
                        poolInfo.Config.maxSize += increase;
                        Debug.Log($"Auto-expanding pool for {prefab.name} to new max size: {poolInfo.Config.maxSize}");
                    }

                    obj = poolInfo.Pool.Get();
                    if (obj != null)
                    {
                        obj.transform.position = position;
                        obj.transform.rotation = rotation;
                        
                        var pooledObj = obj.GetComponent<PooledObject>();
                        if (pooledObj != null)
                        {
                            pooledObj.Initialize(poolInfo.Pool, poolInfo.Config.recycleTimeout);
                        }
                        
                        // Update container name to reflect new counts
                        UpdateContainerName(poolInfo);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error getting object from pool: {ex.Message}");
                    return null;
                }

                return obj;
            }
        }

        public bool HasPool(GameObject prefab)
        {
            if (prefab == null) return false;
            return pools.ContainsKey(prefab.GetInstanceID());
        }
        
        public GameObject GetFromPool(string poolId, Vector3 position, Quaternion rotation)
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException("PoolingService not initialized!");
            }

            lock (poolLock)
            {
                if (!idToPoolId.TryGetValue(poolId, out var poolInstanceId))
                {
                    Debug.LogError($"No pool found with ID {poolId}!");
                    return null;
                }

                if (!pools.TryGetValue(poolInstanceId, out var poolInfo))
                {
                    Debug.LogError($"Pool data inconsistency for ID {poolId}!");
                    return null;
                }

                GameObject obj = null;
                try
                {
                    if (poolInfo.ActiveCount >= poolInfo.Config.maxSize)
                    {
                        if (!poolInfo.Config.autoExpand)
                        {
                            Debug.LogWarning($"Pool for {poolId} is at maximum capacity ({poolInfo.Config.maxSize}) and autoExpand is disabled!");
                            return null;
                        }
                        
                        // If autoExpand is true, increase maxSize by 50% (rounded up)
                        int increase = Mathf.CeilToInt(poolInfo.Config.maxSize * 0.5f);
                        poolInfo.Config.maxSize += increase;
                        Debug.Log($"Auto-expanding pool for {poolId} to new max size: {poolInfo.Config.maxSize}");
                    }

                    obj = poolInfo.Pool.Get();
                    if (obj != null)
                    {
                        obj.transform.position = position;
                        obj.transform.rotation = rotation;
                        
                        var pooledObj = obj.GetComponent<PooledObject>();
                        if (pooledObj != null)
                        {
                            pooledObj.Initialize(poolInfo.Pool, poolInfo.Config.recycleTimeout);
                        }
                        
                        // Update container name to reflect new counts
                        UpdateContainerName(poolInfo);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error getting object from pool: {ex.Message}");
                    return null;
                }

                return obj;
            }
        }

        public bool HasPool(string poolId)
        {
            return !string.IsNullOrEmpty(poolId) && idToPoolId.ContainsKey(poolId);
        }

        private void OnDestroy()
        {
            // Clear all pools
            foreach (var poolInfo in pools.Values)
            {
                if (poolInfo.Pool is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                
                // Destroy the container GameObject
                if (poolInfo.Container != null)
                {
                    Destroy(poolInfo.Container.gameObject);
                }
            }
            pools.Clear();
        }
    }
}