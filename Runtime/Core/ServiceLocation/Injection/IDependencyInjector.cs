using System;

namespace Nexus.Core.ServiceLocation
{
    public interface IDependencyInjector
    {
        Type[] GetDependencies(Type type);
        void InjectProperties(object instance);
    }
}