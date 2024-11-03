using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Nexus.Sequences
{
    /// <summary>
    /// Manages spawning objects with various strategies and tracks their lifetime
    /// </summary>
    public class SpawnerStep : CoroutineStep
    {
        [Header("Spawn Setup")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private PrefabFactory prefabFactory;
        [SerializeField] private GameObject[] fallbackPrefabs;

        [Header("Configuration")]
        [SerializeField] private SpawnConfig config = new SpawnConfig();

        [Header("Events")]
        public UnityEvent<GameObject> onObjectSpawned;
        public UnityEvent<GameObject> onObjectDestroyed;
        public UnityEvent onAllSpawned;
        public UnityEvent onAllDestroyed;

        // Tracking
        private readonly List<GameObject> activeSpawns = new List<GameObject>();
        private int totalSpawned;
        private int currentSpawnPointIndex;
        private ISpawnFactory factory;

        #region Properties
        public int ActiveCount => activeSpawns.Count;
        public int TotalSpawned => totalSpawned;
        public bool HasFinishedSpawning => totalSpawned >= config.spawnCount;
        public float SpawnProgress => config.spawnCount > 0 ? (float)totalSpawned / config.spawnCount : 0f;
        #endregion

        protected override void Awake()
        {
            base.Awake();
            factory = prefabFactory != null ? prefabFactory : new DefaultPrefabFactory(fallbackPrefabs);
        }

        private void OnValidate()
        {
            // Ensure we have valid spawn points
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                // Try to find spawn points as children
                spawnPoints = GetComponentsInChildren<Transform>()
                    .Where(t => t != transform)
                    .ToArray();
            }
        }

        protected override IEnumerator StepRoutine()
        {
            if (!ValidateSetup())
            {
                Debug.LogError("Spawner setup validation failed!");
                yield break;
            }

            yield return SpawnRoutine();

            // Mark as complete once spawning is done
            Complete();

            // Wait for all objects to be destroyed/disabled
            yield return WaitForObjectsCleanup();

            // Mark as finished when all objects are gone
            Finish();
        }

        private bool ValidateSetup()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("No spawn points assigned!");
                return false;
            }

            if (factory == null && (fallbackPrefabs == null || fallbackPrefabs.Length == 0))
            {
                Debug.LogError("No prefab factory or fallback prefabs assigned!");
                return false;
            }

            return true;
        }

        private IEnumerator SpawnRoutine()
        {
            totalSpawned = 0;
            currentSpawnPointIndex = 0;

            if (config.spawnStrategy == SpawnStrategy.AllAtOnce)
            {
                // Spawn all objects at once
                for (int i = 0; i < config.spawnCount; i++)
                {
                    SpawnObject();
                }
                onAllSpawned?.Invoke();
            }
            else
            {
                // Spawn objects over time
                while (totalSpawned < config.spawnCount)
                {
                    SpawnObject();
                    
                    // Calculate next interval
                    float interval = config.spawnInterval;
                    if (config.intervalVariation > 0)
                    {
                        interval += Random.Range(-config.intervalVariation, config.intervalVariation);
                    }
                    
                    yield return new WaitForSeconds(Mathf.Max(0.01f, interval));
                }
                onAllSpawned?.Invoke();
            }
        }

        private void SpawnObject()
        {
            // Get spawn position based on strategy
            Transform spawnPoint = GetNextSpawnPoint();
            if (spawnPoint == null) return;

            // Create the object
            GameObject spawnedObject = factory.CreateSpawnObject(spawnPoint.position, spawnPoint.rotation);
            if (spawnedObject == null) return;

            // Position the object
            Vector3 position = spawnPoint.position;
            if (config.positionVariation != Vector3.zero)
            {
                position += new Vector3(
                    Random.Range(-config.positionVariation.x, config.positionVariation.x),
                    Random.Range(-config.positionVariation.y, config.positionVariation.y),
                    Random.Range(-config.positionVariation.z, config.positionVariation.z)
                );
            }

            Quaternion rotation = spawnPoint.rotation;
            if (config.rotationVariation != Vector3.zero)
            {
                rotation *= Quaternion.Euler(
                    Random.Range(-config.rotationVariation.x, config.rotationVariation.x),
                    Random.Range(-config.rotationVariation.y, config.rotationVariation.y),
                    Random.Range(-config.rotationVariation.z, config.rotationVariation.z)
                );
            }

            spawnedObject.transform.SetPositionAndRotation(position, rotation);

            // Apply scale variation
            if (config.scaleVariation != Vector3.zero)
            {
                Vector3 baseScale = spawnedObject.transform.localScale;
                spawnedObject.transform.localScale = new Vector3(
                    baseScale.x + Random.Range(-config.scaleVariation.x, config.scaleVariation.x),
                    baseScale.y + Random.Range(-config.scaleVariation.y, config.scaleVariation.y),
                    baseScale.z + Random.Range(-config.scaleVariation.z, config.scaleVariation.z)
                );
            }

            // Play effects
            if (config.spawnEffect != null)
            {
                Instantiate(config.spawnEffect, position, Quaternion.identity).Play();
            }

            if (config.spawnSound != null && Camera.main != null)
            {
                AudioSource.PlayClipAtPoint(config.spawnSound, Camera.main.transform.position);
            }

            // Track the object
            activeSpawns.Add(spawnedObject);
            totalSpawned++;

            // Subscribe to object destruction
            var tracker = spawnedObject.AddComponent<SpawnTracker>();
            tracker.Initialize(this);

            onObjectSpawned?.Invoke(spawnedObject);
        }

        private Transform GetNextSpawnPoint()
        {
            switch (config.spawnStrategy)
            {
                case SpawnStrategy.Sequential:
                    return spawnPoints[currentSpawnPointIndex++ % spawnPoints.Length];

                case SpawnStrategy.Random:
                    return spawnPoints[Random.Range(0, spawnPoints.Length)];

                case SpawnStrategy.RoundRobin:
                    int pointsPerObject = Mathf.Max(1, spawnPoints.Length / config.spawnCount);
                    return spawnPoints[(totalSpawned * pointsPerObject) % spawnPoints.Length];

                case SpawnStrategy.AllAtOnce:
                    return spawnPoints[totalSpawned % spawnPoints.Length];

                default:
                    return spawnPoints[0];
            }
        }

        private IEnumerator WaitForObjectsCleanup()
        {
            while (activeSpawns.Count > 0)
            {
                // Clean up any null references
                activeSpawns.RemoveAll(obj => obj == null || !obj.activeInHierarchy);
                
                // Wait for remaining active objects
                yield return new WaitForSeconds(0.5f);
            }
        }

        public void HandleObjectDestroyed(GameObject obj)
        {
            if (activeSpawns.Remove(obj))
            {
                onObjectDestroyed?.Invoke(obj);

                if (activeSpawns.Count == 0 && HasFinishedSpawning)
                {
                    onAllDestroyed?.Invoke();
                }
            }
        }

        protected override void HandleStepError(Exception error)
        {
            // Cleanup any active spawns
            foreach (var obj in activeSpawns.ToArray())
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            activeSpawns.Clear();

            base.HandleStepError(error);
        }

        public override void CleanupStep()
        {
            // Ensure all spawned objects are destroyed
            foreach (var obj in activeSpawns.ToArray())
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            activeSpawns.Clear();

            base.CleanupStep();
        }

        // Helper class for tracking spawned objects
        private class SpawnTracker : MonoBehaviour
        {
            private SpawnerStep spawner;

            public void Initialize(SpawnerStep spawner)
            {
                this.spawner = spawner;
            }

            private void OnDestroy()
            {
                spawner?.HandleObjectDestroyed(gameObject);
            }

            private void OnDisable()
            {
                spawner?.HandleObjectDestroyed(gameObject);
            }
        }

        // Default factory implementation using prefabs
        private class DefaultPrefabFactory : ISpawnFactory
        {
            private readonly GameObject[] prefabs;

            public DefaultPrefabFactory(GameObject[] prefabs)
            {
                this.prefabs = prefabs;
            }

            public GameObject CreateSpawnObject(Vector3 position, Quaternion rotation)
            {
                if (prefabs == null || prefabs.Length == 0) return null;
                var prefab = prefabs[Random.Range(0, prefabs.Length)];
                return prefab != null ? Instantiate(prefab, position, rotation) : null;
            }
        }
    }
}