using System;

namespace Nexus.Core.Rx.Operators
{
    /// <summary>
    /// Takes values from the source observable until the other observable emits a value.
    /// - Thread-safe implementation
    /// - Proper cleanup of subscriptions
    /// - Handles edge cases for completion and errors
    /// </summary>
    public class TakeUntilObservable<TSource, TOther> : IObservable<TSource>
    {
        private readonly IObservable<TSource> source;
        private readonly IObservable<TOther> other;

        public TakeUntilObservable(IObservable<TSource> source, IObservable<TOther> other)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.other = other ?? throw new ArgumentNullException(nameof(other));
        }

        public IDisposable Subscribe(IObserver<TSource> observer)
        {
            var gate = new object();
            var disposables = new CompositeDisposable();
            var stopped = false;

            // Subscribe to the source observable
            disposables.Add(source.Subscribe(
                value =>
                {
                    lock (gate)
                    {
                        if (!stopped)
                        {
                            try
                            {
                                observer.OnNext(value);
                            }
                            catch (Exception ex)
                            {
                                observer.OnError(new RxException("TakeUntil.Source", value, ex));
                            }
                        }
                    }
                },
                error =>
                {
                    lock (gate)
                    {
                        if (!stopped)
                        {
                            stopped = true;
                            observer.OnError(error);
                            disposables.Dispose();
                        }
                    }
                },
                () =>
                {
                    lock (gate)
                    {
                        if (!stopped)
                        {
                            stopped = true;
                            observer.OnCompleted();
                            disposables.Dispose();
                        }
                    }
                }));

            // Subscribe to the other observable that will stop the sequence
            disposables.Add(other.Subscribe(
                value =>
                {
                    lock (gate)
                    {
                        if (!stopped)
                        {
                            stopped = true;
                            observer.OnCompleted();
                            disposables.Dispose();
                        }
                    }
                },
                error =>
                {
                    lock (gate)
                    {
                        if (!stopped)
                        {
                            stopped = true;
                            observer.OnError(error);
                            disposables.Dispose();
                        }
                    }
                },
                () =>
                {
                    // Do nothing when the other sequence completes
                    // The source sequence should continue emitting
                }));

            return disposables;
        }
    }
}