using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Service factory for creating MonoBehaviour-based services
    /// </summary>
    public static class ServiceFactory
    {
        private static readonly Dictionary<Type, GameObject> serviceContainers = new Dictionary<Type, GameObject>();

        public static T CreateMonoBehaviourService<T>(string containerName = null) where T : MonoBehaviourServiceBase
        {
            var serviceType = typeof(T);
            
            // Check if we already have a container for this service type
            if (serviceContainers.TryGetValue(serviceType, out var existingContainer))
            {
                var existingService = existingContainer.GetComponent<T>();
                if (existingService != null)
                    return existingService;
            }

            // Create a new container if needed
            var container = new GameObject(containerName ?? $"{serviceType.Name}Container");
            serviceContainers[serviceType] = container;
            
            // Make it persistent if it's a singleton service
            if (typeof(ISingletonService).IsAssignableFrom(serviceType))
            {
                GameObject.DontDestroyOnLoad(container);
            }

            return container.AddComponent<T>();
        }

        public static void CleanupService<T>() where T : MonoBehaviourServiceBase
        {
            var serviceType = typeof(T);
            if (serviceContainers.TryGetValue(serviceType, out var container))
            {
                GameObject.Destroy(container);
                serviceContainers.Remove(serviceType);
            }
        }
    }
}