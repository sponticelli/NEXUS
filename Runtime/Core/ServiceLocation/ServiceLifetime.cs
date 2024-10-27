using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus.Core.ServiceLocation
{
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

        private class ServiceRegistry
        {
            public Type ImplementationType { get; set; }
            public ServiceLifetime Lifetime { get; set; }
            public object SingletonInstance { get; set; }
            public bool IsMonoBehaviour { get; set; }
            public Func<object> Factory { get; set; }
            public object Configuration { get; set; }
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

            registries[interfaceType] = new ServiceRegistry
            {
                ImplementationType = implementationType,
                Lifetime = lifetime,
                IsMonoBehaviour = isMonoBehaviour,
                Configuration = configuration,
                Factory = () => CreateAndConfigureInstance(implementationType, isMonoBehaviour, configuration)
            };
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
            bool isMonoBehaviour = instance is MonoBehaviour;

            // Configure the instance if it's configurable
            if (instance is IConfigurable<TConfig> configurable && configuration != null)
            {
                configurable.Configure(configuration);
            }

            var registry = new ServiceRegistry
            {
                ImplementationType = instance.GetType(),
                Lifetime = lifetime,
                IsMonoBehaviour = isMonoBehaviour,
                SingletonInstance = lifetime == ServiceLifetime.Singleton ? instance : null,
                // Remove the Configuration property since it's not needed
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

            switch (registry.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    return GetOrCreateSingleton<T>(registry);
                
                case ServiceLifetime.SceneScoped:
                    return GetOrCreateSceneScoped<T>(registry);
                
                case ServiceLifetime.Transient:
                    return CreateTransient<T>(registry);
                
                default:
                    throw new ArgumentException($"Unsupported lifetime: {registry.Lifetime}");
            }
        }
        
        private T GetOrCreateSingleton<T>(ServiceRegistry registry) where T : class
        {
            registry.SingletonInstance ??= registry.Factory();
            return registry.SingletonInstance as T;
        }
        
        private T GetOrCreateSceneScoped<T>(ServiceRegistry registry) where T : class
        {
            string currentScene = SceneManager.GetActiveScene().name;
            EnsureSceneDictionary(currentScene);

            var sceneServices = sceneScopedServices[currentScene];
            Type type = typeof(T);

            if (!sceneServices.TryGetValue(type, out object service))
            {
                service = registry.Factory();
                sceneServices[type] = service;
            }

            return service as T;
        }
        
        private T CreateTransient<T>(ServiceRegistry registry) where T : class
        {
            return registry.Factory() as T;
        }
        
        private object CreateAndConfigureInstance(Type type, bool isMonoBehaviour, object configuration)
        {
            object instance;
            
            if (!isMonoBehaviour)
            {
                instance = Activator.CreateInstance(type);
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
            }

            // Configure the instance if it's configurable
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
    }
    
    
}