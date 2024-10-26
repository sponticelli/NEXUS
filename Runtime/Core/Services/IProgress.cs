namespace Nexus.Core.Services
{
    /// <summary>
    /// Interface for tracking async operation progress
    /// </summary>
    public interface IProgress<in T>
    {
        void Report(T value);
    }
}