using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Bootstrap;
using Nexus.Core.Bootstrap.Scenes;
using UnityEngine;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Enhanced scene bootstrapper with service lifecycle management
    /// </summary>
    public abstract class ServiceAwareSceneBootstrapper : EnhancedSceneBootstrapper
    {
        private readonly List<IServiceLifecycle> sceneServices = new List<IServiceLifecycle>();
        private CancellationTokenSource sceneCts;

        protected override async Task RegisterSceneServices()
        {
            sceneCts = new CancellationTokenSource();
            
            try
            {
                // Register core scene services
                await base.RegisterSceneServices();
                
                // Initialize all registered scene-scoped services
                foreach (var service in sceneServices)
                {
                    await service.Initialize(sceneCts.Token)
                        .WithErrorHandling($"Initializing {service.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to register scene services: {ex}");
                await CleanupSceneServices();
                throw;
            }
        }

        protected override async Task InitializeScene()
        {
            try
            {
                await base.InitializeScene();
                Debug.Log($"Scene {gameObject.scene.name} initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Scene initialization failed: {ex}");
                await CleanupSceneServices();
                throw;
            }
        }

        protected override void CleanupScene()
        {
            base.CleanupScene();
            CleanupSceneServices().GetAwaiter().GetResult();
        }

        private async Task CleanupSceneServices()
        {
            // Cancel any ongoing operations
            sceneCts?.Cancel();

            // Shutdown services in reverse order
            for (int i = sceneServices.Count - 1; i >= 0; i--)
            {
                try
                {
                    await sceneServices[i].Shutdown();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error shutting down service: {ex}");
                }
            }

            sceneServices.Clear();
            sceneCts?.Dispose();
            sceneCts = null;
        }

        protected void RegisterSceneService<T>(T service) where T : class, IServiceLifecycle
        {
            GameInitializer.Instance.RegisterServiceWithLifetime(service, ServiceLifetimeScope.SceneScoped);
            sceneServices.Add(service);
        }
    }
}