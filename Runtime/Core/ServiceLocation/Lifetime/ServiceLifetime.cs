using System;

namespace Nexus.Core.ServiceLocation
{
    public interface IServiceResolver
    {
        T GetService<T>() where T : class;
        object ResolveType(Type serviceType);
        bool CanResolve(Type serviceType);
    }


    // Attribute to mark constructor for dependency injection

    public enum ServiceLifetime
    {
        Singleton,
        SceneScoped,
        Transient
    }
}