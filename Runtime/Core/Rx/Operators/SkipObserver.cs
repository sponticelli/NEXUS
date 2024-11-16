using System;

namespace Nexus.Core.Rx.Operators
{
    internal class SkipObserver<T> : IObserver<T>
    {
        private readonly IObserver<T> observer;
        private readonly int count;
        private int skipped;

        public SkipObserver(IObserver<T> observer, int count)
        {
            this.observer = observer;
            this.count = count;
            this.skipped = 0;
        }

        public void OnNext(T value)
        {
            if (skipped < count)
            {
                skipped++;
            }
            else
            {
                observer.OnNext(value);
            }
        }

        public void OnError(Exception error)
        {
            observer.OnError(error);
        }

        public void OnCompleted()
        {
            observer.OnCompleted();
        }
    }
}