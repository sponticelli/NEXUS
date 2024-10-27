using System;

namespace Nexus.Core.ServiceLocation
{
    public interface IServiceRegistry
    {
        void Register<TInterface, TImplementation>(ServiceLifetime lifetime)
            where TInterface : class
            where TImplementation : class, TInterface;

        void RegisterWithConfig<TInterface, TImplementation, TConfig>(
            ServiceLifetime lifetime,
            TConfig config)
            where TInterface : class
            where TImplementation : class, TInterface
            where TConfig : class;

        void RegisterInstance<TInterface>(
            TInterface instance,
            ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TInterface : class;

        bool IsRegistered<T>() where T : class;
        bool IsRegistered(Type type); // Added method
        ServiceRegistry GetRegistration(Type type);
    }
}