using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus.Core.ServiceLocation
{
    public class ServiceLifetimeManager : IServiceLifetimeManager
    {
        private readonly Dictionary<string, Dictionary<Type, object>> sceneScopedServices = 
            new Dictionary<string, Dictionary<Type, object>>();

        public object GetOrCreateInstance(ServiceRegistry registry, Type serviceType)
        {
            Debug.Log($"GetOrCreateInstance called for {serviceType.Name} with lifetime {registry.Lifetime}");
            
            return registry.Lifetime switch
            {
                ServiceLifetime.Singleton => GetOrCreateSingleton(registry, serviceType),
                ServiceLifetime.SceneScoped => GetOrCreateSceneScoped(registry, serviceType),
                ServiceLifetime.Transient => CreateTransient(registry, serviceType),
                _ => throw new ArgumentException($"Unsupported lifetime: {registry.Lifetime}")
            };
        }

        private object GetOrCreateSingleton(ServiceRegistry registry, Type serviceType)
        {
            Debug.Log($"GetOrCreateSingleton called for {serviceType.Name}");
            
            if (registry.SingletonInstance != null)
            {
                Debug.Log($"Returning existing singleton instance of {serviceType.Name}");
                return registry.SingletonInstance;
            }

            lock (registry)
            {
                if (registry.SingletonInstance != null)
                {
                    return registry.SingletonInstance;
                }

                try
                {
                    Debug.Log($"Creating new singleton instance of {serviceType.Name} using factory");
                    var instance = registry.Factory();

                    if (instance == null)
                    {
                        throw new InvalidOperationException($"Factory failed to create instance of {serviceType.Name}");
                    }

                    registry.SingletonInstance = instance;
                    Debug.Log($"Successfully created singleton instance of {serviceType.Name}");
                    return instance;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create singleton instance of {serviceType.Name}: {ex}");
                    throw;
                }
            }
        }

        private object GetOrCreateSceneScoped(ServiceRegistry registry, Type serviceType)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            Debug.Log($"GetOrCreateSceneScoped called for {serviceType.Name} in scene {currentScene}");
            
            EnsureSceneDictionary(currentScene);
            var sceneServices = sceneScopedServices[currentScene];

            if (!sceneServices.TryGetValue(serviceType, out object service))
            {
                try
                {
                    Debug.Log($"Creating new scene-scoped instance of {serviceType.Name} in scene {currentScene}");
                    service = registry.Factory();
                    
                    if (service == null)
                    {
                        throw new InvalidOperationException($"Factory failed to create scene-scoped instance of {serviceType.Name}");
                    }

                    sceneServices[serviceType] = service;
                    Debug.Log($"Successfully created scene-scoped instance of {serviceType.Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create scene-scoped instance of {serviceType.Name}: {ex}");
                    throw;
                }
            }
            else
            {
                Debug.Log($"Returning existing scene-scoped instance of {serviceType.Name} from scene {currentScene}");
            }

            return service;
        }

        private object CreateTransient(ServiceRegistry registry, Type serviceType)
        {
            Debug.Log($"CreateTransient called for {serviceType.Name}");
            
            try
            {
                var instance = registry.Factory();
                
                if (instance == null)
                {
                    throw new InvalidOperationException($"Factory failed to create transient instance of {serviceType.Name}");
                }

                Debug.Log($"Successfully created transient instance of {serviceType.Name}");
                return instance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create transient instance of {serviceType.Name}: {ex}");
                throw;
            }
        }

        private void EnsureSceneDictionary(string sceneName)
        {
            if (!sceneScopedServices.ContainsKey(sceneName))
            {
                sceneScopedServices[sceneName] = new Dictionary<Type, object>();
            }
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"Scene loaded: {scene.name}, Mode: {mode}");
            EnsureSceneDictionary(scene.name);
        }

        public void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"Scene unloaded: {scene.name}");
            if (!sceneScopedServices.TryGetValue(scene.name, out var sceneServices)) 
            {
                return;
            }

            foreach (var service in sceneServices.Values)
            {
                if (service is MonoBehaviour mb)
                {
                    Debug.Log($"Destroying scene-scoped service {mb.GetType().Name} in scene {scene.name}");
                    GameObject.Destroy(mb.gameObject);
                }
                else if (service is IDisposable disposable)
                {
                    Debug.Log($"Disposing scene-scoped service {service.GetType().Name}");
                    disposable.Dispose();
                }
            }

            sceneScopedServices.Remove(scene.name);
        }

        public void CleanupServices()
        {
            Debug.Log("Cleaning up all services");
            foreach (var sceneServices in sceneScopedServices.Values)
            {
                foreach (var service in sceneServices.Values)
                {
                    if (service is MonoBehaviour mb)
                    {
                        GameObject.Destroy(mb.gameObject);
                    }
                    else if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }

            sceneScopedServices.Clear();
        }
    }
}