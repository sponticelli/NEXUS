using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus.Core.ServiceLocation
{
    // Attribute to mark constructor for dependency injection
    [AttributeUsage(AttributeTargets.Constructor)]
    public class ServiceConstructorAttribute : Attribute { }
    
    public enum ServiceLifetime
    {
        Singleton,
        SceneScoped,
        Transient
    }
    
    public interface IConfigurable<in TConfig>
    {
        void Configure(TConfig configuration);
    }

    public class ServiceLocator : MonoBehaviour
    {
        private static ServiceLocator instance;
        private readonly Dictionary<Type, ServiceRegistry> registries = new Dictionary<Type, ServiceRegistry>();
        private readonly Dictionary<string, Dictionary<Type, object>> sceneScopedServices = new Dictionary<string, Dictionary<Type, object>>();
        private readonly HashSet<Type> servicesBeingResolved = new HashSet<Type>();

        private class ServiceRegistry
        {
            public Type ImplementationType { get; set; }
            public ServiceLifetime Lifetime { get; set; }
            public object SingletonInstance { get; set; }
            public bool IsMonoBehaviour { get; set; }
            public Func<object> Factory { get; set; }
            public object Configuration { get; set; }
            public Type[] Dependencies { get; set; }
        }

        public static ServiceLocator Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }
                GameObject go = new GameObject("ServiceLocator");
                instance = go.AddComponent<ServiceLocator>();
                DontDestroyOnLoad(go);
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
        
            instance = this;
            DontDestroyOnLoad(gameObject);
        
            // Subscribe to scene events
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
        
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            CleanupAllServices();
        }
        
        public void RegisterService<TInterface, TImplementation, TConfig>(
            ServiceLifetime lifetime, 
            TConfig configuration = default) 
            where TInterface : class 
            where TImplementation : class, TInterface
            where TConfig : class
        {
            Type interfaceType = typeof(TInterface);
            Type implementationType = typeof(TImplementation);

            bool isMonoBehaviour = typeof(MonoBehaviour).IsAssignableFrom(implementationType);
            Type[] dependencies = GetDependencies(implementationType);

            var registry = new ServiceRegistry
            {
                ImplementationType = implementationType,
                Lifetime = lifetime,
                IsMonoBehaviour = isMonoBehaviour,
                Configuration = configuration,
                Dependencies = dependencies,
                Factory = () => CreateAndConfigureInstance(implementationType, isMonoBehaviour, configuration)
            };

            registries[interfaceType] = registry;
        }
        
        // Register without configuration (overload for backward compatibility)
        public void RegisterService<TInterface, TImplementation>(ServiceLifetime lifetime) 
            where TInterface : class 
            where TImplementation : class, TInterface
        {
            RegisterService<TInterface, TImplementation, object>(lifetime, null);
        }
        
        // Register instance with configuration
        public void RegisterInstance<TInterface, TConfig>(
            TInterface instance, 
            ServiceLifetime lifetime = ServiceLifetime.Singleton,
            TConfig configuration = default) 
            where TInterface : class
            where TConfig : class
        {
            Type interfaceType = typeof(TInterface);
            Type implementationType = instance.GetType();
            bool isMonoBehaviour = instance is MonoBehaviour;

            // Configure the instance if it's configurable
            if (instance is IConfigurable<TConfig> configurable && configuration != null)
            {
                configurable.Configure(configuration);
            }

            var registry = new ServiceRegistry
            {
                ImplementationType = implementationType,
                Lifetime = lifetime,
                IsMonoBehaviour = isMonoBehaviour,
                SingletonInstance = lifetime == ServiceLifetime.Singleton ? instance : null,
                Dependencies = GetDependencies(implementationType),  // Add this
                Factory = () => instance
            };

            registries[interfaceType] = registry;

            if (lifetime == ServiceLifetime.SceneScoped)
            {
                string currentScene = SceneManager.GetActiveScene().name;
                EnsureSceneDictionary(currentScene);
                sceneScopedServices[currentScene][interfaceType] = instance;
            }
        }
        
        public void ReconfigureService<TInterface, TConfig>(TConfig newConfiguration)
            where TInterface : class
            where TConfig : class
        {
            Type interfaceType = typeof(TInterface);
    
            if (!registries.TryGetValue(interfaceType, out var registry))
            {
                Debug.LogError($"No service of type {interfaceType.Name} has been registered!");
                return;
            }

            object instance = null;
    
            // Get the instance based on lifetime
            switch (registry.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    instance = registry.SingletonInstance;
                    break;
                case ServiceLifetime.SceneScoped:
                    string currentScene = SceneManager.GetActiveScene().name;
                    if (sceneScopedServices.TryGetValue(currentScene, out var sceneServices))
                    {
                        sceneServices.TryGetValue(interfaceType, out instance);
                    }
                    break;
                case ServiceLifetime.Transient:
                    Debug.LogWarning("Cannot reconfigure transient services as they are created on-demand.");
                    return;
            }

            // Configure the instance if it exists and is configurable
            if (instance is IConfigurable<TConfig> configurable)
            {
                configurable.Configure(newConfiguration);
                Debug.Log($"Service of type {interfaceType.Name} has been reconfigured");
            }
            else
            {
                Debug.LogError($"Service of type {interfaceType.Name} is not configurable with configuration type {typeof(TConfig).Name}");
            }
        }
        
        public T GetService<T>() where T : class
        {
            Type type = typeof(T);

            if (!registries.TryGetValue(type, out ServiceRegistry registry))
            {
                Debug.LogError($"No service of type {type.Name} has been registered!");
                return null;
            }

            return registry.Lifetime switch
            {
                ServiceLifetime.Singleton => GetOrCreateSingleton(registry, type) as T,
                ServiceLifetime.SceneScoped => GetOrCreateSceneScoped(registry, type) as T,
                ServiceLifetime.Transient => CreateTransient(registry, type) as T,
                _ => throw new ArgumentException($"Unsupported lifetime: {registry.Lifetime}")
            };
        }
        
        private object GetOrCreateSingleton(ServiceRegistry registry, Type serviceType)
        {
            // If instance already exists, return it (thread-safe way)
            if (registry.SingletonInstance != null)
            {
                return registry.SingletonInstance;
            }

            // Lock to prevent multiple threads from creating instances simultaneously
            lock (registry)
            {
                // Double-check in case another thread created the instance
                if (registry.SingletonInstance != null)
                {
                    return registry.SingletonInstance;
                }

                try
                {
                    // Create the instance using the factory
                    registry.SingletonInstance = registry.Factory();
            
                    Debug.Log($"Created singleton instance of {serviceType.Name}");

                    if (registry.SingletonInstance == null)
                    {
                        throw new InvalidOperationException(
                            $"Factory failed to create instance of {serviceType.Name}");
                    }

                    return registry.SingletonInstance;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create singleton instance of {serviceType.Name}: {ex.Message}");
                    throw;
                }
            }
        }
        
        private object GetOrCreateSceneScoped(ServiceRegistry registry, Type serviceType)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            EnsureSceneDictionary(currentScene);

            var sceneServices = sceneScopedServices[currentScene];

            if (!sceneServices.TryGetValue(serviceType, out object service))
            {
                try
                {
                    service = registry.Factory();
            
                    if (service == null)
                    {
                        throw new InvalidOperationException(
                            $"Factory failed to create scene-scoped instance of {serviceType.Name}");
                    }

                    sceneServices[serviceType] = service;
                    Debug.Log($"Created scene-scoped instance of {serviceType.Name} for scene {currentScene}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create scene-scoped instance of {serviceType.Name}: {ex.Message}");
                    throw;
                }
            }

            return service;
        }
        
        private object CreateTransient(ServiceRegistry registry, Type serviceType)
        {
            try
            {
                var instance = registry.Factory();
        
                if (instance == null)
                {
                    throw new InvalidOperationException(
                        $"Factory failed to create transient instance of {serviceType.Name}");
                }

                return instance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create transient instance of {serviceType.Name}: {ex.Message}");
                throw;
            }
        }
        
        private object CreateAndConfigureInstance(Type type, bool isMonoBehaviour, object configuration)
        {
            // Check for circular dependencies
            if (servicesBeingResolved.Contains(type))
            {
                throw new InvalidOperationException(
                    $"Circular dependency detected while resolving {type.Name}. Resolution path: {string.Join(" -> ", servicesBeingResolved)}");
            }

            servicesBeingResolved.Add(type);

            try
            {
                object instance;

                if (!isMonoBehaviour)
                {
                    // Get constructor dependencies
                    var dependencies = GetDependencies(type);
                    var resolvedDependencies = dependencies.Select(ResolveServiceType).ToArray();

                    // Create instance with dependencies
                    instance = Activator.CreateInstance(type, resolvedDependencies);
                }
                else
                {
                    GameObject serviceObject = new GameObject($"{type.Name}Service");
                    
                    var registry = registries.FirstOrDefault(r => r.Value.ImplementationType == type).Value;
                    
                    if (registry != null && registry.Lifetime == ServiceLifetime.Singleton)
                    {
                        serviceObject.transform.SetParent(transform);
                        DontDestroyOnLoad(serviceObject);
                    }
                    
                    instance = serviceObject.AddComponent(type);

                    // For MonoBehaviours, inject dependencies through properties
                    InjectProperties(instance);
                }

                // Configure if needed
                if (configuration != null)
                {
                    Type configurableType = typeof(IConfigurable<>).MakeGenericType(configuration.GetType());
                    if (configurableType.IsAssignableFrom(type))
                    {
                        var configureMethod = type.GetMethod("Configure", new[] { configuration.GetType() });
                        configureMethod?.Invoke(instance, new[] { configuration });
                    }
                }

                return instance;
            }
            finally
            {
                servicesBeingResolved.Remove(type);
            }
        }
        
        private void EnsureSceneDictionary(string sceneName)
        {
            if (!sceneScopedServices.ContainsKey(sceneName))
            {
                sceneScopedServices[sceneName] = new Dictionary<Type, object>();
            }
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureSceneDictionary(scene.name);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (!sceneScopedServices.TryGetValue(scene.name, out var sceneServices)) return;
            foreach (var service in sceneServices.Values)
            {
                switch (service)
                {
                    case MonoBehaviour mb:
                        Destroy(mb.gameObject);
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }
            sceneScopedServices.Remove(scene.name);
        }
        
        private void CleanupAllServices()
        {
            // Cleanup singletons
            foreach (var registry in registries.Values)
            {
                if (registry.SingletonInstance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                else if (registry.SingletonInstance is MonoBehaviour mb)
                {
                    Destroy(mb.gameObject);
                }
            }

            // Cleanup scene-scoped services
            foreach (var sceneServices in sceneScopedServices.Values)
            {
                foreach (var service in sceneServices.Values)
                {
                    if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    else if (service is MonoBehaviour mb)
                    {
                        Destroy(mb.gameObject);
                    }
                }
            }

            registries.Clear();
            sceneScopedServices.Clear();
        }
        
        private Type[] GetDependencies(Type type)
        {
            // Try to find constructor with [ServiceConstructor] attribute
            var constructor = type.GetConstructors()
                .FirstOrDefault(c => c.GetCustomAttributes(typeof(ServiceConstructorAttribute), true).Any());

            // If no attributed constructor found, use the one with the most parameters
            if (constructor == null)
            {
                constructor = type.GetConstructors()
                    .OrderByDescending(c => c.GetParameters().Length)
                    .FirstOrDefault();
            }

            return constructor?.GetParameters()
                .Select(p => p.ParameterType)
                .ToArray() ?? Array.Empty<Type>();
        }
        
        private void InjectProperties(object instance)
        {
            var properties = instance.GetType()
                .GetProperties()
                .Where(p => p.CanWrite && registries.ContainsKey(p.PropertyType));

            foreach (var property in properties)
            {
                var service = ResolveServiceType(property.PropertyType);
                property.SetValue(instance, service);
            }
        }
        
        private object ResolveServiceType(Type serviceType)
        {
            if (!registries.TryGetValue(serviceType, out var registry))
            {
                throw new InvalidOperationException(
                    $"Cannot resolve dependency of type {serviceType.Name} as it hasn't been registered.");
            }

            return registry.Lifetime switch
            {
                ServiceLifetime.Singleton => GetOrCreateSingleton(registry, serviceType),
                ServiceLifetime.SceneScoped => GetOrCreateSceneScoped(registry, serviceType),
                ServiceLifetime.Transient => CreateTransient(registry, serviceType),
                _ => throw new ArgumentException($"Unsupported lifetime: {registry.Lifetime}")
            };
        }
    }
    
    
}