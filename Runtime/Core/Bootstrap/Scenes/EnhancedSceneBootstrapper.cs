using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Nexus.Core.Bootstrap.Scenes
{
    public abstract class EnhancedSceneBootstrapper : SceneBootstrapper
    {
        [Header("Service Configuration")]
        [SerializeField] private bool enableServiceOverrides = false;
        [SerializeField] private List<ServiceOverride> serviceOverrides = new List<ServiceOverride>();

        // Cache for type lookups
        private Dictionary<string, Type> serviceTypeCache = new Dictionary<string, Type>();

        protected override async Task RegisterSceneServices()
        {
            if (enableServiceOverrides)
            {
                RegisterOverriddenServices();
            }

            await RegisterDefaultServices();
        }

        /// <summary>
        /// Override this to register your normal services
        /// </summary>
        protected virtual async Task RegisterDefaultServices()
        {
            await Task.CompletedTask;
        }

        private void RegisterOverriddenServices()
        {
            foreach (var serviceOverride in serviceOverrides)
            {
                try
                {
                    switch (serviceOverride.ImplementationType)
                    {
                        case ServiceImplementationType.Default:
                            // Skip - will use default implementation
                            continue;

                        case ServiceImplementationType.Debug:
                            if (serviceOverride.DebugImplementation != null)
                            {
                                RegisterDebugService(serviceOverride.ServiceName, serviceOverride.DebugImplementation);
                            }
                            break;

                        case ServiceImplementationType.Custom:
                            if (serviceOverride.CustomImplementation != null)
                            {
                                RegisterCustomService(serviceOverride.ServiceName, serviceOverride.CustomImplementation);
                            }
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to register service override for {serviceOverride.ServiceName}: {e}");
                }
            }
        }

        private void RegisterDebugService(string serviceName, ScriptableObject debugImplementation)
        {
            var serviceType = GetServiceType(serviceName);
            if (serviceType == null) return;

            if (!serviceType.IsInstanceOfType(debugImplementation))
            {
                Debug.LogError($"Debug implementation {debugImplementation.GetType().Name} does not implement {serviceName}");
                return;
            }

            GameInitializer.Instance.RegisterService(serviceType, debugImplementation);
        }

        private void RegisterCustomService(string serviceName, MonoBehaviour customImplementation)
        {
            var serviceType = GetServiceType(serviceName);
            if (serviceType == null) return;

            var serviceInterface = customImplementation.GetComponent(serviceType);
            if (serviceInterface == null)
            {
                Debug.LogError($"Custom implementation {customImplementation.GetType().Name} does not implement {serviceName}");
                return;
            }

            GameInitializer.Instance.RegisterService(serviceType, serviceInterface);
        }

        private Type GetServiceType(string serviceName)
        {
            if (serviceTypeCache.TryGetValue(serviceName, out var cachedType))
            {
                return cachedType;
            }

            // Look for the type in all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(serviceName);
                if (type != null)
                {
                    serviceTypeCache[serviceName] = type;
                    return type;
                }
            }

            Debug.LogError($"Could not find type for service: {serviceName}");
            return null;
        }
    }
}