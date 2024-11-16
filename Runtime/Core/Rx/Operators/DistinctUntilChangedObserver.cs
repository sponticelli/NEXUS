using System;
using System.Collections.Generic;

namespace Nexus.Core.Rx.Operators
{
    internal class DistinctUntilChangedObserver<T> : IObserver<T>
    {
        private readonly IObserver<T> observer;
        private readonly IEqualityComparer<T> comparer;
        private bool hasValue;
        private T lastValue;

        public DistinctUntilChangedObserver(
            IObserver<T> observer,
            IEqualityComparer<T> comparer)
        {
            this.observer = observer;
            this.comparer = comparer;
        }

        public void OnNext(T value)
        {
            var shouldEmit = false;

            if (!hasValue)
            {
                hasValue = true;
                lastValue = value;
                shouldEmit = true;
            }
            else
            {
                try
                {
                    if (!comparer.Equals(lastValue, value))
                    {
                        lastValue = value;
                        shouldEmit = true;
                    }
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                    return;
                }
            }

            if (shouldEmit)
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