using System;

namespace Nexus.Core.Rx.Operators
{
    /// <summary>
    /// combines the latest values from two observables.
    /// Takes two source observables and a result selector
    /// Creates a shared state object to track values
    /// Subscribes to both sources
    /// Manages cleanup through CompositeDisposable
    /// </summary>
    /// <usage>
    ///     // Combining player stats
    ///     var damage = sword.CombineLatest(
    ///         strengthBuff,
    ///         (weapon, buff) => weapon * buff
    ///     );
    /// 
    ///     // UI updates
    ///     var displayText = playerName.CombineLatest(
    ///         score,
    ///         (name, points) => $"{name}: {points}"
    ///     );
    /// 
    ///     // Game mechanics
    ///     var isAlive = health.CombineLatest(
    ///         poison,
    ///         (h, p) => h > p
    ///     );
    /// </usage>
    public class CombineLatestObservable<T1, T2, TResult> : IObservable<TResult>
    {
        private readonly IObservable<T1> first;
        private readonly IObservable<T2> second;
        private readonly Func<T1, T2, TResult> resultSelector;

        public CombineLatestObservable(
            IObservable<T1> first,
            IObservable<T2> second,
            Func<T1, T2, TResult> resultSelector)
        {
            this.first = first;
            this.second = second;
            this.resultSelector = resultSelector;
        }

        public IDisposable Subscribe(IObserver<TResult> observer)
        {
            var state = new CombineLatestState<T1, T2, TResult>(observer, resultSelector);
        
            var firstSubscription = first.Subscribe(
                value => state.OnNext1(value),
                error => state.OnError(error),
                () => state.OnCompleted1()
            );

            var secondSubscription = second.Subscribe(
                value => state.OnNext2(value),
                error => state.OnError(error),
                () => state.OnCompleted2()
            );

            // Return a composite disposable for cleanup
            return new CompositeDisposable(firstSubscription, secondSubscription, state);
        }
    }
}