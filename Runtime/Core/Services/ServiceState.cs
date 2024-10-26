namespace Nexus.Core.Services
{
    /// <summary>
    /// Defines the lifecycle state of a service
    /// </summary>
    public enum ServiceState
    {
        Uninitialized,
        Initializing,
        Running,
        Failed,
        ShuttingDown,
        Disposed
    }
}