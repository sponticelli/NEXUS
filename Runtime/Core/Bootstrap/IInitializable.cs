using System.Threading.Tasks;

namespace Nexus.Core.Bootstrap
{
    /// <summary>
    /// Interface for services that require initialization
    /// </summary>
    public interface IInitializable
    {
        Task Initialize();
    }
}