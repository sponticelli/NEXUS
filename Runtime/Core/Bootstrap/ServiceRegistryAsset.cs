using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Core.Bootstrap
{
    [CreateAssetMenu(fileName = "ServiceRegistry", menuName = "Nexus/ServiceLocator/Service Registry")]
    public class ServiceRegistryAsset : ScriptableObject
    {
        public List<ServiceDefinition> services = new List<ServiceDefinition>();
    }
}