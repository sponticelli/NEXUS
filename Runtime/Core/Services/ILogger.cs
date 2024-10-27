namespace Nexus.Core.Services
{
    [ServiceInterface]
    public interface ILogger
    {
        void Log(string message);
    }
}