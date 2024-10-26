using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Nexus.Core.Bootstrap
{
    public class GameInitializer : MonoBehaviour
    {
        private static GameInitializer instance;
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        private readonly List<IInitializable> initializableServices = new List<IInitializable>();
        private bool isInitialized;
        private TaskCompletionSource<bool> initializationComplete;

        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private bool isDebugInstance;

        public static GameInitializer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<GameInitializer>();

                    if (instance == null)
                    {
                        var go = new GameObject("GameInitializer (Debug)");
                        instance = go.AddComponent<GameInitializer>();
                        instance.isDebugInstance = true;

#if UNITY_EDITOR
                        instance.gameConfig =
                            UnityEditor.AssetDatabase.LoadAssetAtPath<GameConfig>(
                                "Assets/Resources/DefaultDebugConfig.asset");
#endif
                    }

                    DontDestroyOnLoad(instance.gameObject);
                }

                return instance;
            }
        }

        public bool IsInitialized => isInitialized;

        public Task WaitForInitialization()
        {
            if (isInitialized) return Task.CompletedTask;
            initializationComplete ??= new TaskCompletionSource<bool>();
            return initializationComplete.Task;
        }

        private async void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            initializationComplete ??= new TaskCompletionSource<bool>();

            await Initialize();
        }

        public async Task Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning("GameInitializer is already initialized!");
                return;
            }

            try
            {
                RegisterCoreServices();
                await InitializeServices();
                isInitialized = true;
                initializationComplete?.TrySetResult(true);
                Debug.Log("Game initialized!");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize game: {e}");
                initializationComplete?.TrySetException(e);
                throw;
            }
        }

        private void RegisterCoreServices()
        {
            RegisterService<IGameConfig>(gameConfig);
        }

        private async Task InitializeServices()
        {
            foreach (var service in initializableServices)
            {
                await service.Initialize();
            }
        }

        public void RegisterService<T>(T service) where T : class
        {
            var serviceType = typeof(T);
            if (services.ContainsKey(serviceType))
            {
                throw new Exception($"Service of type {serviceType.Name} is already registered!");
            }

            services[serviceType] = service;

            if (service is IInitializable initializable)
            {
                initializableServices.Add(initializable);
            }
        }

        public void RegisterService(Type serviceType, object service)
        {
            if (services.ContainsKey(serviceType))
            {
                throw new Exception($"Service of type {serviceType.Name} is already registered!");
            }

            services[serviceType] = service;

            if (service is IInitializable initializable)
            {
                initializableServices.Add(initializable);
            }
        }

        public T GetService<T>() where T : class
        {
            var serviceType = typeof(T);
            if (!services.TryGetValue(serviceType, out var service))
            {
                throw new Exception($"Service of type {serviceType.Name} is not registered!");
            }

            return (T)service;
        }

        private void OnDestroy()
        {
            foreach (var service in services.Values)
            {
                if (service is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            services.Clear();
            initializableServices.Clear();
        }
    }
}