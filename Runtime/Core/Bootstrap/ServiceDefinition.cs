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
    }
}