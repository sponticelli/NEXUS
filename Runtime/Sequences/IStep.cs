using System;

namespace Nexus.Sequences
{
    public interface IStep
    {
        bool IsInitialized { get; }
        bool IsComplete { get; }
        bool IsFinished { get; }

        void InitStep();
        void StartStep();
        void UpdateStep();
        void CleanupStep();

        event Action OnComplete;
        event Action OnFinished;
    }
}