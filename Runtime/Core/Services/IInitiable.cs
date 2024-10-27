using System.Threading.Tasks;

namespace Nexus.Core.Services
{
    public interface IInitiable
    {
        Task InitializeAsync();
        
        bool IsInitialized { get; }
        Task WaitForInitialization();
    }
}