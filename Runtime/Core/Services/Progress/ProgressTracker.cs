using System;
using System.Threading;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Tracks progress of async operations with support for cancellation
    /// </summary>
    public class ProgressTracker<T> : IProgress<T>
    {
        private readonly Action<T> onProgressChanged;
        private readonly CancellationToken cancellationToken;
        private T currentProgress;

        public T CurrentProgress => currentProgress;

        public ProgressTracker(Action<T> onProgressChanged = null, CancellationToken cancellationToken = default)
        {
            this.onProgressChanged = onProgressChanged;
            this.cancellationToken = cancellationToken;
        }

        public void Report(T value)
        {
            cancellationToken.ThrowIfCancellationRequested();
            currentProgress = value;
            onProgressChanged?.Invoke(value);
        }
    }
}