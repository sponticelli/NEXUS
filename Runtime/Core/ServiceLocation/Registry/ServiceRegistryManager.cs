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
            this.serviceFactory = serviceFactory;
        }

        public void Register<TInterface, TImplementation>(ServiceLifetime lifetime)
            where TInterface : class
            where TImplementation : class, TInterface
        {
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

            bool isMonoBehaviour = typeof(MonoBehaviour).IsAssignableFrom(implementationType);

            var registry = new ServiceRegistry
            {
                ImplementationType = implementationType,
                Lifetime = lifetime,
                IsMonoBehaviour = isMonoBehaviour,
                Configuration = configuration,
                Factory = () => serviceFactory.CreateInstance(implementationType, isMonoBehaviour, configuration)
            };

            registries[interfaceType] = registry;
        }

        public void RegisterInstance<TInterface>(TInterface instance,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TInterface : class
        {
            Type interfaceType = typeof(TInterface);
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
        }

        public bool IsRegistered<T>() where T : class
        {
            return registries.ContainsKey(typeof(T));
        }

        // Added method to check registration by Type
        public bool IsRegistered(Type type)
        {
            return registries.ContainsKey(type);
        }

        public ServiceRegistry GetRegistration(Type type)
        {
            if (!registries.TryGetValue(type, out var registry))
            {
                throw new InvalidOperationException($"No service of type {type.Name} has been registered!");
            }

            return registry;
        }
    }
}