using System;

namespace Nexus.Core.Rx.Operators
{
    /// <summary>
    /// Tracks latest values from both observables
    /// Manages flags for received values and completion
    /// Thread-safe through lock mechanism
    /// Handles error propagation
    /// Manages proper completion logic
    /// </summary>
    internal class CombineLatestState<T1, T2, TResult> : IDisposable
    {
        private readonly object gate = new object();
        private readonly IObserver<TResult> observer;
        private readonly Func<T1, T2, TResult> resultSelector;

        private bool hasFirst;
        private bool hasSecond;
        private T1 latestFirst;
        private T2 latestSecond;
        private bool completed1;
        private bool completed2;
        private bool disposed;

        public CombineLatestState(
            IObserver<TResult> observer,
            Func<T1, T2, TResult> resultSelector)
        {
            this.observer = observer;
            this.resultSelector = resultSelector;
        }

        public void OnNext1(T1 value)
        {
            lock (gate)
            {
                if (disposed) return;

                hasFirst = true;
                latestFirst = value;

                if (hasSecond)
                {
                    EmitResult();
                }
            }
        }

        public void OnNext2(T2 value)
        {
            lock (gate)
            {
                if (disposed) return;

                hasSecond = true;
                latestSecond = value;

                if (hasFirst)
                {
                    EmitResult();
                }
            }
        }

        public void OnError(Exception error)
        {
            lock (gate)
            {
                if (!disposed)
                {
                    disposed = true;
                    observer.OnError(error);
                }
            }
        }

        public void OnCompleted1()
        {
            lock (gate)
            {
                if (disposed) return;
            
                completed1 = true;
                CheckCompleted();
            }
        }

        public void OnCompleted2()
        {
            lock (gate)
            {
                if (disposed) return;
            
                completed2 = true;
                CheckCompleted();
            }
        }

        private void EmitResult()
        {
            try
            {
                var result = resultSelector(latestFirst, latestSecond);
                observer.OnNext(result);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        private void CheckCompleted()
        {
            // If either observable completes without producing a value,
            // and we haven't got values from both observables yet,
            // we complete without producing any values
            if ((completed1 && !hasFirst) || (completed2 && !hasSecond))
            {
                if (!disposed)
                {
                    disposed = true;
                    observer.OnCompleted();
                }
                return;
            }

            // If both observables complete after we've received values,
            // we complete after emitting the last combined value
            if (completed1 && completed2)
            {
                if (!disposed)
                {
                    disposed = true;
                    observer.OnCompleted();
                }
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                disposed = true;
            }
        }
    }
}