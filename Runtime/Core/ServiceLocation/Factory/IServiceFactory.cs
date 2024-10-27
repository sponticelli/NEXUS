using System;

namespace Nexus.Core.ServiceLocation
{
    public interface IServiceFactory
    {
        object CreateInstance(Type type, bool isMonoBehaviour, object configuration);
    }
}