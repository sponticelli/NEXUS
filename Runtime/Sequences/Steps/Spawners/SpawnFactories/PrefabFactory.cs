using Nexus.Core.ServiceLocation;
using Nexus.Pooling;
using UnityEngine;

namespace Nexus.Sequences
{
    public class PrefabFactory : BaseSpawnFactory
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private bool usePooling;
        
        private IPoolingService _poolingService;
        private bool _isInitialized;
        
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
                Debug.LogError("PrefabFactory requires a pooling service to be present in the service locator");
                return;
            }
            await _poolingService.WaitForInitialization();
            _isInitialized = true;
        }
        
        public override GameObject CreateSpawnObject(Vector3 position, Quaternion rotation)
        {
            if (!_isInitialized)
            {
                Debug.LogError("PrefabFactory is not initialized");
                return null;
            }
            
            if (usePooling)
            {
                return _poolingService.GetFromPool(prefab, position, rotation);
            }
            
            return Instantiate(prefab, position, rotation);
        }
    }
}