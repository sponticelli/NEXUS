using System;

namespace Nexus.Core.Rx.Operators
{
    public class SelectObservable<TSource, TResult> : IObservable<TResult>
    {
        private readonly IObservable<TSource> source;
        private readonly Func<TSource, TResult> selector;

        public SelectObservable(IObservable<TSource> source, Func<TSource, TResult> selector)
        {
            this.source = source;
            this.selector = selector;
        }

        public IDisposable Subscribe(IObserver<TResult> observer)
        {
            return source.Subscribe(new SelectObserver<TSource, TResult>(observer, selector));
        }
    }
}