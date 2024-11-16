using System;

namespace Nexus.Core.Rx.Operators
{
    /// <summary>
    /// Filters the elements of an observable sequence based on a predicate.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WhereObserver<T> : IObserver<T>
    {
        private readonly IObserver<T> observer;
        private readonly Func<T, bool> predicate;

        public WhereObserver(IObserver<T> observer, Func<T, bool> predicate)
        {
            this.observer = observer;
            this.predicate = predicate;
        }

        public void OnNext(T value)
        {
            try
            {
                if (predicate(value))
                    observer.OnNext(value);
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
        }

        public void OnError(Exception error) => observer.OnError(error);
        public void OnCompleted() => observer.OnCompleted();
    }
}