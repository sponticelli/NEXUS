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
        
        public void RegisterService<TInterface, TImplementation>(ServiceLifetime lifetime) 
            where TInterface : class 
            where TImplementation : class, TInterface
        {
            Type interfaceType = typeof(TInterface);
            Type implementationType = typeof(TImplementation);

            bool isMonoBehaviour = typeof(MonoBehaviour).IsAssignableFrom(implementationType);

            registries[interfaceType] = new ServiceRegistry
            {
                ImplementationType = implementationType,
                Lifetime = lifetime,
                IsMonoBehaviour = isMonoBehaviour,
                Factory = () => CreateInstance(implementationType, isMonoBehaviour)
            };
        }
        
        public void RegisterInstance<TInterface>(TInterface instance, ServiceLifetime lifetime = ServiceLifetime.Singleton) 
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

            if (lifetime == ServiceLifetime.SceneScoped)
            {
                string currentScene = SceneManager.GetActiveScene().name;
                EnsureSceneDictionary(currentScene);
                sceneScopedServices[currentScene][interfaceType] = instance;
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
        
        private object CreateInstance(Type type, bool isMonoBehaviour)
        {
            if (!isMonoBehaviour) return Activator.CreateInstance(type);
            
            GameObject serviceObject = new GameObject($"{type.Name}Service");
        
            // Get the registry for this type to check its lifetime
            var registry = registries.FirstOrDefault(r => r.Value.ImplementationType == type).Value;
        
            // If it's a singleton MonoBehaviour, parent it to the ServiceLocator and mark it DontDestroyOnLoad
            if (registry != null && registry.Lifetime == ServiceLifetime.Singleton)
            {
                serviceObject.transform.SetParent(this.transform);
                DontDestroyOnLoad(serviceObject);
            }
        
            return serviceObject.AddComponent(type);

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