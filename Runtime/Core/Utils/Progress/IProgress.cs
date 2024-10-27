namespace Nexus.Core.Utils.Progress
{
    /// <summary>
    /// Interface for tracking async operation progress
    /// </summary>
    public interface IProgress<in T>
    {
        void Report(T value);
    }
}