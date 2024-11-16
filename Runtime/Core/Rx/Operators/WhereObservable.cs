using System;

namespace Nexus.Core.Rx.Operators
{
    /// <summary>
    /// Filters the elements of an observable sequence based on a predicate.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WhereObservable<T> : IObservable<T>
    {
        private readonly IObservable<T> source;
        private readonly Func<T, bool> predicate;

        public WhereObservable(IObservable<T> source, Func<T, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return source.Subscribe(new WhereObserver<T>(observer, predicate));
        }
    }
}