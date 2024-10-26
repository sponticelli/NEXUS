using System;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Nexus.Core.Bootstrap.Scenes
{
    /// <summary>
    /// Base class for scene-specific initialization
    /// </summary>
    public abstract class SceneBootstrapper : MonoBehaviour
    {
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private bool useDebugServices = false;
        [SerializeField] private List<ScriptableObject> debugServices = new List<ScriptableObject>();

        private bool isInitialized;
        private TaskCompletionSource<bool> initializationComplete;
        
        public Task WaitForInitialization()
        {
            if (isInitialized) return Task.CompletedTask;
            initializationComplete ??= new TaskCompletionSource<bool>();
            return initializationComplete.Task;
        }
        
        public bool IsInitialized => isInitialized;
        
        protected virtual async void Awake()
        {
            if (initializeOnAwake)
            {
                await Initialize();
            }
        }

        public virtual async Task Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning($"SceneBootrapper {gameObject.name} is already initialized!");
                return;
            }
            
            initializationComplete ??= new TaskCompletionSource<bool>();
            await GameInitializer.Instance.WaitForInitialization();
            try
            {
                if (useDebugServices)
                {
                    RegisterDebugServices();
                }

                await RegisterSceneServices();
                await InitializeScene();
                initializationComplete?.TrySetResult(true);
                isInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize game: {e}");
                initializationComplete?.TrySetException(e);
                throw;
            }
        }

        private void RegisterDebugServices()
        {
            foreach (var service in debugServices)
            {
                var interfaces = service.GetType().GetInterfaces();
                foreach (var serviceInterface in interfaces)
                {
                    if (serviceInterface.Namespace?.StartsWith("UnityEngine") == true)
                        continue;

                    try
                    {
                        GameInitializer.Instance.RegisterService(serviceInterface, service);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to register debug service {service} as {serviceInterface}: {e.Message}");
                    }
                }
            }
        }

        protected virtual Task RegisterSceneServices()
        {
            return Task.CompletedTask;
        }

        protected virtual Task InitializeScene()
        {
            return Task.CompletedTask;
        }

        protected virtual void OnDestroy()
        {
            CleanupScene();
        }

        protected virtual void CleanupScene()
        {
        }
    }
}