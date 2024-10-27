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
            if (registry.SingletonInstance != null)
            {
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

        private void EnsureSceneDictionary(string sceneName)
        {
            if (!sceneScopedServices.ContainsKey(sceneName))
            {
                sceneScopedServices[sceneName] = new Dictionary<Type, object>();
            }
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureSceneDictionary(scene.name);
        }

        public void OnSceneUnloaded(Scene scene)
        {
            if (!sceneScopedServices.TryGetValue(scene.name, out var sceneServices)) return;
            foreach (var service in sceneServices.Values)
            {
                switch (service)
                {
                    case MonoBehaviour mb:
                        GameObject.Destroy(mb.gameObject);
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }

            sceneScopedServices.Remove(scene.name);
        }

        public void CleanupServices()
        {
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
                        GameObject.Destroy(mb.gameObject);
                    }
                }
            }

            sceneScopedServices.Clear();
        }
    }
}