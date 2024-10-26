using Nexus.Core.Bootstrap;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Base class for singleton services that should only have one instance
    /// </summary>
    public abstract class SingletonServiceBase : ServiceBase
    {
        protected SingletonServiceBase()
        {
            // Register singleton services with GameInitializer automatically
            GameInitializer.Instance.RegisterService(GetType(), this);
        }
    }
}