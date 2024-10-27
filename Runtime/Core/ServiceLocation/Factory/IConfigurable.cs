namespace Nexus.Core.ServiceLocation
{
    public interface IConfigurable<in TConfig>
    {
        void Configure(TConfig configuration);
    }
}