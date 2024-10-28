using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Core.Bootstrap
{
    [CreateAssetMenu(fileName = "ServiceRegistry", menuName = "Nexus/ServiceLocator/Service Registry")]
    public class ServiceRegistryAsset : ScriptableObject
    {
        [SerializeField] private ServiceRegistryAsset parentRegistry;
        [SerializeField] private List<ServiceDefinition> services = new List<ServiceDefinition>();
        
        private List<ServiceDefinition> _allServices;
        
        public List<ServiceDefinition> GetServices()
        {
            if (_allServices == null)
            {
                _allServices = new List<ServiceDefinition>();
                if (parentRegistry != null)
                {
                    _allServices.AddRange(parentRegistry.GetServices());
                }
                
                // Add all the services that are not already in the list (SameInterfaceAs)
                foreach (var service in services)
                {
                    if (!_allServices.Exists(s => s.SameInterfaceAs(service)))
                    {
                        _allServices.Add(service);
                    } 
                    else
                    {
                        Debug.LogWarning($"Service {service.serviceName} already exists in the registry");
                    }
                }
                
            }
            

            return _allServices;
        }
    }
}