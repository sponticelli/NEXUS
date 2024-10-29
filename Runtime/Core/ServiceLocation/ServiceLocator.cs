using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus.Core.ServiceLocation
{
    public class ServiceLocator : MonoBehaviour, IServiceResolver
    {
        private static ServiceLocator instance;
        private IServiceRegistry registry;
        private IServiceLifetimeManager lifetimeManager;
        private IServiceFactory serviceFactory;
        private IDependencyInjector dependencyInjector;

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

            InitializeComponents();
            SubscribeToSceneEvents();
        }

        private void InitializeComponents()
        {
            dependencyInjector = new DependencyInjector(this);
            serviceFactory = new ServiceFactory(this, dependencyInjector);
            lifetimeManager = new ServiceLifetimeManager();
            registry = new ServiceRegistryManager(serviceFactory);
        }

        private void SubscribeToSceneEvents()
        {
            SceneManager.sceneLoaded += lifetimeManager.OnSceneLoaded;
            SceneManager.sceneUnloaded += lifetimeManager.OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= lifetimeManager.OnSceneLoaded;
            SceneManager.sceneUnloaded -= lifetimeManager.OnSceneUnloaded;
            lifetimeManager.CleanupServices();
        }

        // IServiceResolver implementation.
        public T GetService<T>() where T : class
        {
            return (T)ResolveType(typeof(T));
        }
        
        public object GetService(Type serviceType)
        {
            return ResolveType(serviceType);
        }

        public object ResolveType(Type serviceType)
        {
            if (!CanResolve(serviceType))
            {
                throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered.");
            }

            var registration = registry.GetRegistration(serviceType);
            return lifetimeManager.GetOrCreateInstance(registration, serviceType);
        }

        public bool CanResolve(Type serviceType)
        {
            return registry.IsRegistered(serviceType);
        }

        // Additional methods to expose registry for registration
        public void Register<TInterface, TImplementation>(ServiceLifetime lifetime)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            registry.Register<TInterface, TImplementation>(lifetime);
        }
        
        public void Register(Type interfaceType, Type implementationType, ServiceLifetime lifetime, Func<object> factory = null)
        {
            registry.Register(interfaceType, implementationType, lifetime, factory);
        }

        public void RegisterWithConfig<TInterface, TImplementation, TConfig>(
            ServiceLifetime lifetime,
            TConfig config)
            where TInterface : class
            where TImplementation : class, TInterface
            where TConfig : class
        {
            registry.RegisterWithConfig<TInterface, TImplementation, TConfig>(lifetime, config);
        }

        public void RegisterInstance<TInterface>(TInterface instance,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TInterface : class
        {
            registry.RegisterInstance<TInterface>(instance, lifetime);
        }
        
        public override string ToString()
        {
            return registry.ToString();
        }
    }
}