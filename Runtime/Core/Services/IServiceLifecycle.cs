using System.Threading;
using System.Threading.Tasks;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Interface for services that need to track their lifecycle state
    /// </summary>
    public interface IServiceLifecycle
    {
        ServiceState State { get; }
        Task Initialize(CancellationToken cancellationToken = default);
        Task Shutdown();
    }
}