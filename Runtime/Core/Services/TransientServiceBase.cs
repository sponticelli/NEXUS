namespace Nexus.Core.Services
{
    /// <summary>
    /// Base class for transient services that can be created multiple times
    /// </summary>
    public abstract class TransientServiceBase : ServiceBase
    {
        protected TransientServiceBase()
        {
            // Transient services can be created multiple times and don't need to be registered with GameInitializer
        }
    }
}