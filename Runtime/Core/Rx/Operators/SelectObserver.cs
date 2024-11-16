using System;

namespace Nexus.Core.Rx.Operators
{
    public class SelectObserver<TSource, TResult> : IObserver<TSource>
    {
        private readonly IObserver<TResult> observer;
        private readonly Func<TSource, TResult> selector;

        public SelectObserver(IObserver<TResult> observer, Func<TSource, TResult> selector)
        {
            this.observer = observer;
            this.selector = selector;
        }

        public void OnNext(TSource value)
        {
            try
            {
                observer.OnNext(selector(value));
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