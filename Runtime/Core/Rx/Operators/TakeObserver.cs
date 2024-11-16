using System;

namespace Nexus.Core.Rx.Operators
{
    /// <summary>
    /// Handles counting and completion
    /// </summary>
    internal class TakeObserver<T> : IObserver<T>
    {
        private readonly IObserver<T> observer;
        private readonly int count;
        private int taken;
        private bool completed;

        public TakeObserver(IObserver<T> observer, int count)
        {
            this.observer = observer;
            this.count = count;
            this.taken = 0;
            this.completed = false;
        }

        public void OnNext(T value)
        {
            if (completed) return;

            if (taken < count)
            {
                taken++;
                observer.OnNext(value);

                if (taken >= count)
                {
                    completed = true;
                    observer.OnCompleted();
                }
            }
        }

        public void OnError(Exception error)
        {
            if (!completed)
            {
                completed = true;
                observer.OnError(error);
            }
        }

        public void OnCompleted()
        {
            if (!completed)
            {
                completed = true;
                observer.OnCompleted();
            }
        }
    }
}