using System;
using System.Collections;
using Nexus.Core.Rx.Unity;
using UnityEngine;

namespace Nexus.Core.Rx.Operators
{
    /// <summary>
    /// Observable that delays emissions and completions by a specified amount of time
    /// </summary>
    /// <usage>
    /// // Basic delay
    ///     source
    ///         .Delay(2f)
    ///         .Subscribe(value => HandleDelayedValue(value));
    /// 
    /// // Game mechanics
    ///     damageEvents
    ///         .Delay(1.5f)
    ///         .Subscribe(damage => ApplyDelayedDamage(damage));
    /// 
    /// // UI feedback
    ///     buttonClicks
    ///         .Delay(0.5f)
    ///         .Subscribe(_ => EnableButton());
    /// <using>
    public class DelayObservable<T> : IObservable<T>
    {
        private readonly IObservable<T> source;
        private readonly float delaySeconds;

        public DelayObservable(IObservable<T> source, float seconds)
        {
            this.source = source;
            this.delaySeconds = seconds;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var runner = RxUnityRunner.Instance;
            var sourceSubscription = source.Subscribe(
                value =>
                {
                    var coroutine = runner.StartCoroutine(DelayedEmission(value, observer));
                },
                error =>
                {
                    observer.OnError(error);
                },
                () =>
                {
                    var coroutine = runner.StartCoroutine(DelayedCompletion(observer));
                }
            );

            return sourceSubscription;
        }

        private IEnumerator DelayedEmission(T value, IObserver<T> observer)
        {
            yield return new WaitForSeconds(delaySeconds);
            observer.OnNext(value);
        }

        private IEnumerator DelayedCompletion(IObserver<T> observer)
        {
            yield return new WaitForSeconds(delaySeconds);
            observer.OnCompleted();
        }
    }
}