using Nexus.Core.Bootstrap;
using UnityEngine;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Extension methods for GameInitializer to handle service lifetimes
    /// </summary>
    public static class GameInitializerLifetimeExtensions
    {
        public static void RegisterSingletonService<TService>(
            this GameInitializer initializer,
            TService implementation) where TService : class
        {
            var serviceType = typeof(TService);
            
            // Validate singleton registration
            ServiceLifetimeManager.ValidateServiceRegistration(serviceType, implementation);

            // If it's a MonoBehaviour, ensure it persists
            if (implementation is MonoBehaviour mono)
            {
                GameObject.DontDestroyOnLoad(mono.gameObject);
            }

            initializer.RegisterService(serviceType, implementation);
        }

        public static void RegisterScopedService<TService>(
            this GameInitializer initializer,
            TService implementation,
            UnityEngine.SceneManagement.Scene scene) where TService : class
        {
            var serviceType = typeof(TService);
            
            // Validate scoped registration
            ServiceLifetimeManager.ValidateServiceRegistration(serviceType, implementation);

            // Register with scene context
            initializer.RegisterService(serviceType, implementation);

            // Optional: Track the service's scene association
            SceneContext.RegisterSceneService(scene, implementation);
        }

        public static TService CreateTransientService<TService>(
            this GameInitializer initializer) where TService : class, new()
        {
            // Create new instance each time
            var implementation = new TService();
            
            // Validate transient registration
            ServiceLifetimeManager.ValidateServiceRegistration(typeof(TService), implementation);

            return implementation;
        }
    }
}