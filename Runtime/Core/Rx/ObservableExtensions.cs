using System;
using Nexus.Core.Rx.Operators;

namespace Nexus.Core.Rx
{
    public static class ObservableExtensions
    {
        public static IDisposable Subscribe<T>(this IObservable<T> observable, 
            Action<T> onNext, 
            Action<Exception> onError = null, 
            Action onCompleted = null)
        {
            return observable.Subscribe(new AnonymousObserver<T>(onNext, onError, onCompleted));
        }
    }
    
    public static class Observable
    {
        /// <summary>
        /// Creates an observable sequence from a subscribe method implementation.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
        /// <param name="subscribe">Implementation of the resulting observable sequence's subscribe method.</param>
        /// <returns>Observable sequence with the specified implementation for the Subscribe method.</returns>
        public static IObservable<T> Create<T>(Func<IObserver<T>, IDisposable> subscribe)
        {
            return new CreateObservable<T>(subscribe);
        }

        private class CreateObservable<T> : IObservable<T>
        {
            private readonly Func<IObserver<T>, IDisposable> subscribe;

            public CreateObservable(Func<IObserver<T>, IDisposable> subscribe)
            {
                this.subscribe = subscribe ?? throw new ArgumentNullException(nameof(subscribe));
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                try
                {
                    return subscribe(observer ?? throw new ArgumentNullException(nameof(observer)));
                }
                catch (Exception ex)
                {
                    observer.OnError(new RxException("Observable.Create", null, ex));
                    return new Subscription(() => { });
                }
            }
        }
    }
}