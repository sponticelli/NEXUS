using System;
using System.Collections.Generic;

namespace Nexus.Core.Rx
{
    /// <summary>
    /// A general purpose reactive stream. Good for events and messages.
    /// - Can both emit and receive values
    /// - Supports error and completion states
    /// - Multiple subscribers
    /// - No value caching (unlike ReactiveProperty)
    /// </summary>
    /// <usage>
    /// var damageEvents = new Subject<int>();
    /// damageEvents.Subscribe(damage => ApplyDamage(damage));
    /// damageEvents.OnNext(10); // Emit damage event
    /// </usage>
    public class Subject<T> : IObservable<T>, IObserver<T>
    {
        private readonly List<IObserver<T>> observers = new List<IObserver<T>>();
        private bool isCompleted;
        private Exception error;

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (isCompleted)
            {
                observer.OnCompleted();
                return new Subscription(() => { });
            }

            if (error != null)
            {
                observer.OnError(error);
                return new Subscription(() => { });
            }

            observers.Add(observer);
            return new Subscription(() => observers.Remove(observer));
        }

        public virtual void OnNext(T value)
        {
            if (isCompleted || error != null) return;

            foreach (var observer in observers.ToArray())
            {
                observer.OnNext(value);
            }
        }

        public void OnError(Exception error)
        {
            if (isCompleted || this.error != null) return;

            this.error = error;
            foreach (var observer in observers.ToArray())
            {
                observer.OnError(error);
            }
            observers.Clear();
        }

        public void OnCompleted()
        {
            if (isCompleted || error != null) return;

            isCompleted = true;
            foreach (var observer in observers.ToArray())
            {
                observer.OnCompleted();
            }
            observers.Clear();
        }
    }
}