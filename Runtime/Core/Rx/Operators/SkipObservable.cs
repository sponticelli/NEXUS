using System;

namespace Nexus.Core.Rx.Operators
{
    public class SkipObservable<T> : IObservable<T>
    {
        private readonly IObservable<T> source;
        private readonly int count;

        public SkipObservable(IObservable<T> source, int count)
        {
            this.source = source;
            this.count = count;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return source.Subscribe(new SkipObserver<T>(observer, count));
        }
    }
}