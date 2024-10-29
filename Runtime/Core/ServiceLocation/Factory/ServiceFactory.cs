using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nexus.Core.ServiceLocation
{
    public class ServiceFactory : IServiceFactory
    {
        private readonly IServiceResolver resolver;
        private readonly IDependencyInjector dependencyInjector;
        private readonly HashSet<Type> servicesBeingResolved = new HashSet<Type>();

        public ServiceFactory(IServiceResolver resolver, IDependencyInjector dependencyInjector)
        {
            this.resolver = resolver;
            this.dependencyInjector = dependencyInjector;
        }

        public object CreateInstance(Type type, bool isMonoBehaviour, object configuration)
        {
            if (servicesBeingResolved.Contains(type))
            {
                throw new InvalidOperationException(
                    $"Circular dependency detected while resolving {type.Name}. Resolution path: {string.Join(" -> ", servicesBeingResolved)}");
            }

            servicesBeingResolved.Add(type);

            try
            {
                Debug.Log($"Creating instance of {type.Name}");
                object instance = isMonoBehaviour
                    ? CreateMonoBehaviourInstance(type)
                    : CreateRegularInstance(type);

                ConfigureInstance(instance, configuration);
                return instance;
            }
            finally
            {
                servicesBeingResolved.Remove(type);
            }
        }

        private object CreateRegularInstance(Type type)
        {
            var dependencies = dependencyInjector.GetDependencies(type);
            var resolvedDependencies = dependencies.Select(resolver.ResolveType).ToArray();
            return Activator.CreateInstance(type, resolvedDependencies);
        }

        private object CreateMonoBehaviourInstance(Type type)
        {
            // Get the ServiceLocator instance
            var serviceLocator = ServiceLocator.Instance;
            if (serviceLocator == null)
            {
                throw new InvalidOperationException("ServiceLocator instance not found");
            }

            // Format the service name
            var serviceName = type.Name;
            if (!serviceName.EndsWith("Service"))
            {
                serviceName += "Service";
            }
            
            Debug.Log($"Creating MonoBehaviour instance of {serviceName}");

            // Check if a GameObject with this name already exists under ServiceLocator
            Transform existingService = serviceLocator.transform.Find(serviceName);
            if (existingService != null)
            {
                var existingComponent = existingService.GetComponent(type);
                if (existingComponent != null)
                {
                    Debug.Log($"Found existing instance of {serviceName}");
                    return existingComponent;
                }
                
                // If GameObject exists but doesn't have the right component, destroy it
                GameObject.Destroy(existingService.gameObject);
            }
            
            // Create a new GameObject as a child of the ServiceLocator
            var serviceObject = new GameObject(serviceName);
            serviceObject.transform.SetParent(serviceLocator.transform);
            Debug.Log($"Created new instance of {serviceName} as child of ServiceLocator");

            // Add the component and inject dependencies
            var instance = serviceObject.AddComponent(type);
            dependencyInjector.InjectProperties(instance);
            
            return instance;
        }

        private void ConfigureInstance(object instance, object configuration)
        {
            if (configuration == null) return;

            Type configurableType = typeof(IConfigurable<>).MakeGenericType(configuration.GetType());
            if (configurableType.IsAssignableFrom(instance.GetType()))
            {
                var configureMethod = instance.GetType().GetMethod("Configure", new[] { configuration.GetType() });
                configureMethod?.Invoke(instance, new[] { configuration });
            }
        }
    }
}