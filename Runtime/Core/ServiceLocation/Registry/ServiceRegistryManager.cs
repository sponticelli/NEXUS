using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Core.ServiceLocation
{
    public class ServiceRegistryManager : IServiceRegistry
    {
        private readonly Dictionary<Type, ServiceRegistry> registries = new Dictionary<Type, ServiceRegistry>();
        private readonly IServiceFactory serviceFactory;

        public ServiceRegistryManager(IServiceFactory serviceFactory)
        {
            Debug.Log("Initializing ServiceRegistryManager with factory");
            this.serviceFactory = serviceFactory;
        }

        public void Register<TInterface, TImplementation>(ServiceLifetime lifetime)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            Debug.Log($"Registering service: Interface={typeof(TInterface).Name}, Implementation={typeof(TImplementation).Name}, Lifetime={lifetime}");
            RegisterWithConfig<TInterface, TImplementation, object>(lifetime, null);
        }

        public void RegisterWithConfig<TInterface, TImplementation, TConfig>(
            ServiceLifetime lifetime,
            TConfig configuration)
            where TInterface : class
            where TImplementation : class, TInterface
            where TConfig : class
        {
            Type interfaceType = typeof(TInterface);
            Type implementationType = typeof(TImplementation);

            Debug.Log($"Registering service with config: Interface={interfaceType.Name}, Implementation={implementationType.Name}, Lifetime={lifetime}");

            bool isMonoBehaviour = typeof(MonoBehaviour).IsAssignableFrom(implementationType);
            Debug.Log($"Service {implementationType.Name} is MonoBehaviour: {isMonoBehaviour}");

            var registry = new ServiceRegistry
            {
                ImplementationType = implementationType,
                Lifetime = lifetime,
                IsMonoBehaviour = isMonoBehaviour,
                Configuration = configuration
            };

            registry.Factory = () =>
            {
                Debug.Log($"Factory creating instance of {implementationType.Name}");
                return serviceFactory.CreateInstance(implementationType, isMonoBehaviour, configuration);
            };
            
            registries[interfaceType] = registry;
            Debug.Log($"Service registered successfully: {interfaceType.Name}");
        }

        public void Register(Type interfaceType, Type implementationType, ServiceLifetime lifetime, Func<object> factory = null)
        {
            Debug.Log($"Registering service: Interface={interfaceType.Name}, Implementation={implementationType.Name}, Lifetime={lifetime}");
    
            bool isMonoBehaviour = typeof(MonoBehaviour).IsAssignableFrom(implementationType);
            Debug.Log($"Service {implementationType.Name} is MonoBehaviour: {isMonoBehaviour}");

            // If we're replacing an existing registry, clean up any existing instances
            if (registries.TryGetValue(interfaceType, out var existingRegistry))
            {
                if (existingRegistry.SingletonInstance is MonoBehaviour mb)
                {
                    Debug.Log($"Cleaning up existing instance of {interfaceType.Name}");
                    GameObject.Destroy(mb.gameObject);
                }
            }

            var registry = new ServiceRegistry
            {
                ImplementationType = implementationType,
                Lifetime = lifetime,  // Make sure we preserve the specified lifetime
                IsMonoBehaviour = isMonoBehaviour,
                Factory = factory ?? (() => serviceFactory.CreateInstance(implementationType, isMonoBehaviour, null))
            };

            registries[interfaceType] = registry;
            Debug.Log($"Service {interfaceType.Name} registered with lifetime {lifetime}");
        }

        public void RegisterInstance<TInterface>(TInterface instance,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TInterface : class
        {
            Type interfaceType = typeof(TInterface);
            Debug.Log($"Registering instance: Interface={interfaceType.Name}, Lifetime={lifetime}");

            bool isMonoBehaviour = instance is MonoBehaviour;

            var registry = new ServiceRegistry
            {
                ImplementationType = instance.GetType(),
                Lifetime = lifetime,
                IsMonoBehaviour = isMonoBehaviour,
                SingletonInstance = lifetime == ServiceLifetime.Singleton ? instance : null,
                Factory = () => instance
            };

            registries[interfaceType] = registry;
            Debug.Log($"Instance registered successfully: {interfaceType.Name}");
        }

        public bool IsRegistered<T>() where T : class
        {
            return registries.ContainsKey(typeof(T));
        }

        public bool IsRegistered(Type type)
        {
            return registries.ContainsKey(type);
        }

        public ServiceRegistry GetRegistration(Type type)
        {
            Debug.Log($"Getting registration for type: {type.Name}");
            if (!registries.TryGetValue(type, out var registry))
            {
                throw new InvalidOperationException($"No service of type {type.Name} has been registered!");
            }
            return registry;
        }
        
        public override string ToString()
        {
            return GetRegisteredServices();
        }

        private string GetRegisteredServices()
        {
            string result = "Registered Services:\n";
            foreach (var registry in registries)
            {
                result += $"{registry.Key.Name} -> {registry.Value.ImplementationType.Name} ({registry.Value.Lifetime})\n";
            }
            return result;
        }
    }
}