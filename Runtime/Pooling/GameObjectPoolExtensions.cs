using UnityEngine;
using Nexus.Core.ServiceLocation;

namespace Nexus.Pooling
{
    // Extension method for easier pool access
    public static class GameObjectPoolExtensions
    {
        public static GameObject SpawnFromPool(this GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var poolingService = ServiceLocator.Instance.GetService<IPoolingService>();
            return poolingService.GetFromPool(prefab, position, rotation);
        }

        public static GameObject SpawnFromPool(this string poolId, Vector3 position, Quaternion rotation)
        {
            var poolingService = ServiceLocator.Instance.GetService<IPoolingService>();
            return poolingService.GetFromPool(poolId, position, rotation);
        }
    }
}