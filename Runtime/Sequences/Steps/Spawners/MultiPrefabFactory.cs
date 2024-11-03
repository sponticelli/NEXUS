using Nexus.Core.ServiceLocation;
using Nexus.Pooling;
using UnityEngine;

namespace Nexus.Sequences
{
    public class MultiPrefabFactory : BaseSpawnFactory
    {
        [SerializeField] private GameObject[] prefabs;
        [SerializeField] private bool randomExtraction;
        
        [SerializeField] private bool usePooling;
        
        private IPoolingService _poolingService;
        private bool _isInitialized;
        private int _lastIndex = -1;
        
        private async void Start()
        {
            if (!usePooling)
            {
                _isInitialized = true;
                return;
            }
            _poolingService = ServiceLocator.Instance.GetService<IPoolingService>();
            if (_poolingService == null)
            {
                Debug.LogError("MultiPrefabFactory requires a pooling service to be present in the service locator");
                return;
            }
            await _poolingService.WaitForInitialization();
            _isInitialized = true;
        }
        
        public override GameObject CreateSpawnObject(Vector3 position, Quaternion rotation)
        {
            if (!_isInitialized)
            {
                Debug.LogError("MultiPrefabFactory is not initialized");
                return null;
            }
            
            _lastIndex = randomExtraction ? Random.Range(0, prefabs.Length) : (_lastIndex + 1) % prefabs.Length;
            var prefab = prefabs[_lastIndex];
            if (usePooling)
            {
                return _poolingService.GetFromPool(prefab, position, rotation);
            }
            
            return Instantiate(prefab, position, rotation);
        }
    }
}