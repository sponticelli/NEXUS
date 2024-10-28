using System;
using Nexus.Core.ServiceLocation;
using UnityEngine;

namespace Nexus.Core.Bootstrap
{
    [Serializable]
    public class ServiceDefinition
    {
        public string serviceName;

        public TypeReference interfaceType;
        public TypeReference implementationType;

        public ServiceLifetime lifetime;

        public MonoBehaviour monoBehaviourPrefab;

        public ScriptableObject configuration;
        
        public bool SameAs(ServiceDefinition other)
        {
            return serviceName == other.serviceName &&
                   interfaceType == other.interfaceType &&
                   implementationType == other.implementationType &&
                   lifetime == other.lifetime &&
                   monoBehaviourPrefab == other.monoBehaviourPrefab &&
                   configuration == other.configuration;
        }
        
        public bool SameInterfaceAs(ServiceDefinition other)
        {
            return interfaceType == other.interfaceType;
        }
    }
}