namespace Nexus.Core.Services
{
    /// <summary>
    /// Base class for Scoped services
    /// </summary>
    public abstract class ScopedServiceBase : ServiceBase, IScopedService
    {
        protected ScopedServiceBase()
        {
            // Scoped services can be created multiple times and don't need to be registered with GameInitializer
        }
    }
}