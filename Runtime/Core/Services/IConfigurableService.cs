namespace Nexus.Core.Services
{
    /// <summary>
    /// Interface for services that require configuration
    /// </summary>
    public interface IConfigurableService<TConfig> where TConfig : ServiceConfigurationBase
    {
        TConfig Configuration { get; }
    }
}