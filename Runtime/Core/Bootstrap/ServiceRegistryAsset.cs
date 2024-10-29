using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Core.Bootstrap
{
    [CreateAssetMenu(fileName = "ServiceRegistry", menuName = "Nexus/ServiceLocator/Service Registry")]
    public class ServiceRegistryAsset : ScriptableObject
    {
        [SerializeField] private ServiceRegistryAsset parentRegistry;
        [SerializeField] private List<ServiceDefinition> services = new List<ServiceDefinition>();
        
        // Mark as non-serialized to prevent Unity from persisting it
        [System.NonSerialized]
        private List<ServiceDefinition> _allServices;

        private void OnEnable()
        {
            // Clear the cache when the asset is loaded
            _allServices = null;
        }

        public List<ServiceDefinition> GetServices()
        {
            if (_allServices == null || _allServices.Count == 0)
            {
                Debug.Log($"Building service list for {name}");
                _allServices = new List<ServiceDefinition>();
                
                if (parentRegistry != null)
                {
                    _allServices.AddRange(parentRegistry.GetServices());
                    Debug.Log($"Added {parentRegistry.name} services to {name}");
                }
                
                // Add all the services that are not already in the list (SameInterfaceAs)
                foreach (var service in services)
                {
                    if (!_allServices.Exists(s => s.SameInterfaceAs(service)))
                    {
                        _allServices.Add(service);
                        Debug.Log($"Added {service.serviceName} to {name} with lifetime {service.lifetime}");
                    } 
                    else
                    {
                        Debug.LogWarning($"Service {service.serviceName} already exists in the registry");
                    }
                }
            }

            return _allServices;
        }

        private void OnDisable()
        {
            // Clear the cache when the asset is unloaded
            _allServices = null;
        }

        private void OnValidate()
        {
            // Clear the cache when the asset is modified in the inspector
            _allServices = null;
        }
    }
}