using System;

namespace Nexus.Core.Rx.Operators
{
    /// <summary>
    /// Represents an observable that throttles the source observable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ThrottleObservable<T> : IObservable<T>
    {
        private readonly IObservable<T> source;
        private readonly float seconds;

        public ThrottleObservable(IObservable<T> source, float seconds)
        {
            this.source = source;
            this.seconds = seconds;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return source.Subscribe(new ThrottleObserver<T>(observer, seconds));
        }
    }
}