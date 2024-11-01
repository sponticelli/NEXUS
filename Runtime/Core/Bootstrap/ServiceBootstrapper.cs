using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexus.Core.ServiceLocation;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.Events;

namespace Nexus.Core.Bootstrap
{
    public class ServiceBootstrapper : MonoBehaviour
    {
        public enum InitializeOn
        {
            Awake,
            Start,
            Enable,
            Manual
        }

        [SerializeField] private InitializeOn initializeOn = InitializeOn.Manual;
        [SerializeField] private ServiceRegistryAsset serviceRegistryAsset;
        [SerializeField] protected UnityEvent onInitialized;
        [SerializeField] protected BootstrapProgressEvent onBootstrapProgress;

        private bool isInitialized = false;
        private TaskCompletionSource<bool> initializationTcs = new TaskCompletionSource<bool>();
        private BootstrapStage currentStage = BootstrapStage.NotStarted;

        public bool IsInitialized => isInitialized;
        
        private void Awake()
        {
            Debug.Log($"ServiceBootstrapper Awake - Initialize On: {initializeOn}");
            if (initializeOn == InitializeOn.Awake)
            {
                _ = Initialize();
            }
        }

        private void Start()
        {
            Debug.Log($"ServiceBootstrapper Start - Initialize On: {initializeOn}");
            if (initializeOn == InitializeOn.Start)
            {
                _ = Initialize();
            }
        }

        private void OnEnable()
        {
            Debug.Log($"ServiceBootstrapper OnEnable - Initialize On: {initializeOn}");
            if (initializeOn == InitializeOn.Enable)
            {
                _ = Initialize();
            }
        }

        public async Task Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning("Service bootstrapper already initialized");
                return;
            }

            Debug.Log("Initializing service bootstrapper");

            if (serviceRegistryAsset == null)
            {
                ReportProgress(BootstrapStage.Failed, 0, 0, error: new InvalidOperationException("No ServiceRegistryAsset assigned"));
                Debug.LogError("No ServiceRegistryAsset assigned to ServiceBootstrapper!");
                return;
            }

            try 
            {
                await RegisterServices();
                
                if (currentStage != BootstrapStage.Failed)
                {
                    ReportProgress(BootstrapStage.Completed, 0, 0);
                    Debug.Log("Service bootstrapper initialization completed");
                    isInitialized = true;
                    initializationTcs.SetResult(true);
                    onInitialized?.Invoke();
                }
            }
            catch (Exception ex)
            {
                ReportProgress(BootstrapStage.Failed, 0, 0, error: ex);
                initializationTcs.SetException(ex);
                
                Debug.LogError($"Service bootstrapper initialization failed: {ex}");
                throw;
            }
        }
        
        public async Task WaitForInitialization()
        {
            if (!isInitialized)
            {
                await initializationTcs.Task;
            }
        }

        protected async Task RegisterServices()
        {
            ReportProgress(BootstrapStage.Registration, 0, 0);
            
            var serviceDefinitions = serviceRegistryAsset.GetServices();
            if (serviceDefinitions == null || serviceDefinitions.Count == 0)
            {
                ReportProgress(BootstrapStage.Failed, 0, 0, error: new InvalidOperationException("No services found in registry"));
                Debug.LogWarning("No services found in registry");
                return;
            }

            Debug.Log($"Found {serviceDefinitions.Count} services to register");

            try
            {
                // Build and sort dependency graph
                var dependencyGraph = BuildDependencyGraph(serviceDefinitions);
                var sortedServiceTypes = TopologicalSort(dependencyGraph);
                var serviceDefMap = serviceDefinitions.ToDictionary(def => def.implementationType.Type);
                
                // Track initialization tasks
                List<Task> initializationTasks = new List<Task>();
                int processedCount = 0;
                
                // Registration phase
                ReportProgress(BootstrapStage.Registration, 0, sortedServiceTypes.Count);
                foreach (var implementationType in sortedServiceTypes)
                {
                    if (serviceDefMap.TryGetValue(implementationType, out var serviceDef))
                    {
                        try 
                        {
                            processedCount++;
                            ReportProgress(BootstrapStage.Registration, processedCount, sortedServiceTypes.Count, serviceDef.serviceName);
                            
                            if (!ServiceLocator.Instance.CanResolve(serviceDef.interfaceType.Type))
                            {
                                var instance = RegisterService(serviceDef);
                                
                                if (instance == null)
                                {
                                    throw new InvalidOperationException($"Failed to create instance for {serviceDef.serviceName}");
                                }
                                
                                // Queue initialization if needed
                                if (instance is IInitiable initiable)
                                {
                                    initializationTasks.Add(initiable.InitializeAsync());
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ReportProgress(BootstrapStage.Failed, processedCount, sortedServiceTypes.Count, serviceDef.serviceName, ex);
                            Debug.LogError($"Failed to register service {serviceDef.serviceName}: {ex}");
                            throw;
                        }
                    }
                }

                // Initialization phase
                if (initializationTasks.Count > 0)
                {
                    ReportProgress(BootstrapStage.Initialization, 0, initializationTasks.Count);
                    
                    for (int i = 0; i < initializationTasks.Count; i++)
                    {
                        try
                        {
                            await initializationTasks[i];
                            ReportProgress(BootstrapStage.Initialization, i + 1, initializationTasks.Count);
                        }
                        catch (Exception ex)
                        {
                            ReportProgress(BootstrapStage.Failed, i + 1, initializationTasks.Count, error: ex);
                            Debug.LogError($"Failed to initialize service: {ex}");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReportProgress(BootstrapStage.Failed, 0, serviceDefinitions.Count, error: ex);
                Debug.LogError($"Service registration failed: {ex}");
                throw;
            }
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

            Debug.Log($"Registering service {serviceDef.serviceName} with lifetime {serviceDef.lifetime}");

            object instance = null;
            bool isMonoBehaviour = typeof(MonoBehaviour).IsAssignableFrom(implementationType);

            try
            {
                if (isMonoBehaviour)
                {
                    // Handle MonoBehaviour service registration
                    Func<object> factory = () =>
                    {
                        // Get or create the appropriate container based on service lifetime
                        Transform parent = GetServiceContainer(serviceDef.lifetime);

                        GameObject serviceObject;
                        MonoBehaviour monoInstance;

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

                        // Parent the service based on its lifetime
                        serviceObject.transform.SetParent(parent);

                        if (serviceDef.configuration != null)
                        {
                            ConfigureService(monoInstance, serviceDef.configuration);
                        }

                        return monoInstance;
                    };

                    // Register with the correct lifetime
                    ServiceLocator.Instance.Register(interfaceType, implementationType, serviceDef.lifetime, factory);
                    instance = ServiceLocator.Instance.GetService(interfaceType);
                }
                else
                {
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
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to register service {serviceDef.serviceName}: {ex}");
                return null;
            }

            return instance;
        }

        private Transform GetServiceContainer(ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    return ServiceLocator.Instance.transform;

                case ServiceLifetime.SceneScoped:
                    var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    var containerName = "SceneServices";

                    // Find existing container in current scene
                    var rootObjects = currentScene.GetRootGameObjects();
                    var existingContainer = rootObjects
                        .FirstOrDefault(go => go.name == containerName);

                    if (existingContainer != null)
                    {
                        return existingContainer.transform;
                    }

                    // Create new container in current scene
                    var container = new GameObject(containerName);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(container, currentScene);
                    return container.transform;

                case ServiceLifetime.Transient:
                    // Transient services should be created at scene root
                    return null;

                default:
                    throw new ArgumentException($"Unsupported service lifetime: {lifetime}");
            }
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

        private Dictionary<Type, List<Type>> BuildDependencyGraph(List<ServiceDefinition> serviceDefinitions)
        {
            var dependencyGraph = new Dictionary<Type, List<Type>>();

            foreach (var serviceDef in serviceDefinitions)
            {
                Type implementationType = serviceDef.implementationType.Type;

                if (implementationType == null)
                {
                    Debug.LogError($"Invalid implementation type for {serviceDef.serviceName}");
                    continue;
                }

                var dependencies = new List<Type>();

                // Get dependencies from ServiceDependencyAttribute
                var dependencyAttributes =
                    implementationType.GetCustomAttributes(typeof(ServiceDependencyAttribute), true);
                foreach (ServiceDependencyAttribute attr in dependencyAttributes)
                {
                    dependencies.Add(attr.DependencyType);
                }

                dependencyGraph[implementationType] = dependencies;
            }

            return dependencyGraph;
        }

        private List<Type> TopologicalSort(Dictionary<Type, List<Type>> dependencyGraph)
        {
            var sortedList = new List<Type>();
            var visited = new Dictionary<Type, bool>();

            foreach (var node in dependencyGraph.Keys)
            {
                if (!visited.ContainsKey(node))
                {
                    if (!TopologicalSortUtil(node, visited, sortedList, dependencyGraph))
                    {
                        throw new Exception("Circular dependency detected in services.");
                    }
                }
            }

            return sortedList;
        }
        
        private void ReportProgress(BootstrapStage stage, int current, int total, string serviceName = null, Exception error = null)
        {
            currentStage = stage;
            var progress = new BootstrapProgress(stage, current, total, serviceName, error);
            onBootstrapProgress?.Invoke(progress);
            
            // Log progress if it's not spam
            if (stage == BootstrapStage.Failed || stage == BootstrapStage.Completed || 
                (current % 5 == 0 || current == total))
            {
                Debug.Log($"Bootstrap Progress: {stage} - {current}/{total} {(serviceName != null ? $"- {serviceName}" : "")}");
            }
        }

        private bool TopologicalSortUtil(Type node, Dictionary<Type, bool> visited, List<Type> sortedList,
            Dictionary<Type, List<Type>> dependencyGraph)
        {
            visited[node] = true;

            if (dependencyGraph.TryGetValue(node, out var dependencies))
            {
                foreach (var dep in dependencies)
                {
                    if (!visited.TryGetValue(dep, out var inProcess))
                    {
                        if (!TopologicalSortUtil(dep, visited, sortedList, dependencyGraph))
                        {
                            return false;
                        }
                    }
                    else if (inProcess)
                    {
                        // Circular dependency detected
                        return false;
                    }
                }
            }

            visited[node] = false; // Mark as processed
            if (!sortedList.Contains(node))
            {
                sortedList.Add(node);
            }

            return true;
        }
    }
}