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
            GameObject serviceObject = new GameObject($"{type.Name}Service");
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