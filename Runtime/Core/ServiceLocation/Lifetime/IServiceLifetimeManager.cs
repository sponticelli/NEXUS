using System;
using UnityEngine.SceneManagement;

namespace Nexus.Core.ServiceLocation
{
    public interface IServiceLifetimeManager
    {
        object GetOrCreateInstance(ServiceRegistry registry, Type serviceType);
        void CleanupServices();
        void OnSceneLoaded(Scene scene, LoadSceneMode mode);
        void OnSceneUnloaded(Scene scene);
    }
}