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
            var serviceDefinitions = serviceRegistryAsset.services;

            // Build the dependency graph
            var dependencyGraph = BuildDependencyGraph(serviceDefinitions);

            // Perform topological sort
            List<Type> sortedServiceTypes;
            try
            {
                sortedServiceTypes = TopologicalSort(dependencyGraph);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to sort services: {ex.Message}");
                return;
            }

            // Map implementation types to service definitions
            var serviceDefMap = serviceDefinitions.ToDictionary(def => def.implementationType.Type);

            List<Task> initializationTasks = new List<Task>();

            // Register and initialize services in order
            foreach (var implementationType in sortedServiceTypes)
            {
                if (serviceDefMap.TryGetValue(implementationType, out var serviceDef))
                {
                    if (ServiceLocator.Instance.CanResolve(serviceDef.interfaceType.Type))
                    {
                        Debug.LogWarning($"Service {serviceDef.serviceName} already registered");
                        continue;
                    }
                    
                    var instance = RegisterService(serviceDef);

                    if (instance is IInitiable initiableService)
                    {
                        var initTask = initiableService.InitializeAsync();
                        initializationTasks.Add(initTask);
                    }
                }
                else
                {
                    Debug.LogError($"Service definition not found for type {implementationType.FullName}");
                }
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
                var dependencyAttributes = implementationType.GetCustomAttributes(typeof(ServiceDependencyAttribute), true);
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

        private bool TopologicalSortUtil(Type node, Dictionary<Type, bool> visited, List<Type> sortedList, Dictionary<Type, List<Type>> dependencyGraph)
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