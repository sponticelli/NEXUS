using Nexus.Core.Services;
using UnityEngine;

namespace Nexus.Pooling
{
    [ServiceInterface]
    public interface IPoolingService : IInitiable
    {
        /// <summary>
        /// Gets an object from the pool for the specified prefab
        /// </summary>
        GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation);

        /// <summary>
        /// Gets an object from the pool using its unique identifier
        /// </summary>
        GameObject GetFromPool(string poolId, Vector3 position, Quaternion rotation);
        
        /// <summary>
        /// Checks if a pool exists for the specified prefab
        /// </summary>
        bool HasPool(GameObject prefab);

        /// <summary>
        /// Checks if a pool exists for the specified ID
        /// </summary>
        bool HasPool(string poolId);
    }
}