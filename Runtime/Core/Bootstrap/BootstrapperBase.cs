using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexus.Core.ServiceLocation;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.Events;

namespace Nexus.Core.Bootstrap
{
    public abstract class BootstrapperBase : MonoBehaviour
    {
        [SerializeField] private ServiceRegistryAsset serviceRegistryAsset;

        [SerializeField] protected UnityEvent onInitialized;

        private bool isInitialized = false;
        private TaskCompletionSource<bool> initializationTcs = new TaskCompletionSource<bool>();

        public async Task Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            await RegisterServices();
            isInitialized = true;
            initializationTcs.SetResult(true);
            onInitialized?.Invoke();
        }

        public bool IsInitialized => isInitialized;

        public async Task WaitForInitialization()
        {
            if (!isInitialized)
            {
                await initializationTcs.Task;
            }
        }

        protected async Task RegisterServices()
        {
            List<Task> initializationTasks = new List<Task>();
            List<IInitiable> initiableServices = new List<IInitiable>();

            foreach (var serviceDef in serviceRegistryAsset.services)
            {
                // Check if service is already registered
                if (ServiceLocator.Instance.CanResolve(serviceDef.interfaceType.Type))
                {
                    Debug.LogWarning($"Service {serviceDef.interfaceType.Type.Name} is already registered");
                    continue;
                }
                
                var instance = RegisterService(serviceDef);

                if (instance is IInitiable initiableService)
                {
                    initiableServices.Add(initiableService);
                }
            }

            await InitializeServices(initiableServices, initializationTasks);
        }

        private static async Task InitializeServices(List<IInitiable> initiableServices, List<Task> initializationTasks)
        {
            // Call InitializeAsync on all IInitiable services
            foreach (var initiableService in initiableServices)
            {
                var initTask = initiableService.InitializeAsync();
                initializationTasks.Add(initTask);
            }

            // Wait for all initialization tasks to complete
            await Task.WhenAll(initializationTasks);
        }


        private object RegisterService(ServiceDefinition serviceDef)
        {
            Type interfaceType = serviceDef.interfaceType.Type;
            Type implementationType = serviceDef.implementationType.Type;

            if (interfaceType == null || implementationType == null)
            {
                Debug.LogError($"Invalid service types for {serviceDef.serviceName}");
                return null;
            }

            object instance = null;

            bool isMonoBehaviour = typeof(MonoBehaviour).IsAssignableFrom(implementationType);

            if (isMonoBehaviour)
            {
                // Handle MonoBehaviour service registration
                Func<object> factory = () =>
                {
                    GameObject serviceObject = null;
                    MonoBehaviour monoInstance = null;

                    if (serviceDef.monoBehaviourPrefab != null)
                    {
                        serviceObject = Instantiate(serviceDef.monoBehaviourPrefab.gameObject);
                        monoInstance = serviceObject.GetComponent(implementationType) as MonoBehaviour;
                    }
                    else
                    {
                        serviceObject = new GameObject($"{implementationType.Name}Service");
                        monoInstance = serviceObject.AddComponent(implementationType) as MonoBehaviour;
                    }

                    if (serviceDef.configuration != null)
                    {
                        ConfigureService(monoInstance, serviceDef.configuration);
                    }

                    return monoInstance;
                };

                ServiceLocator.Instance.Register(interfaceType, implementationType, serviceDef.lifetime, factory);
                instance = ServiceLocator.Instance.GetService(interfaceType);
            }
            else
            {
                // Handle pure C# service registration
                Func<object> factory = () =>
                {
                    var objInstance = Activator.CreateInstance(implementationType);

                    if (serviceDef.configuration != null)
                    {
                        ConfigureService(objInstance, serviceDef.configuration);
                    }

                    return objInstance;
                };

                ServiceLocator.Instance.Register(interfaceType, implementationType, serviceDef.lifetime, factory);
                instance = ServiceLocator.Instance.GetService(interfaceType);
            }

            return instance;
        }

        private void ConfigureService(object instance, ScriptableObject configuration)
        {
            Type configType = configuration.GetType();
            Type configurableType = typeof(IConfigurable<>).MakeGenericType(configType);

            if (configurableType.IsAssignableFrom(instance.GetType()))
            {
                var configureMethod = instance.GetType().GetMethod("Configure", new[] { configType });
                configureMethod?.Invoke(instance, new[] { configuration });
            }
            else
            {
                Debug.LogWarning(
                    $"Service {instance.GetType().Name} does not implement IConfigurable<{configType.Name}>");
            }
        }
    }
}