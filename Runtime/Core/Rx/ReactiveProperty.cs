using System;
using System.Collections.Generic;

namespace Nexus.Core.Rx
{
    /// <summary>
    /// A wrapper around a value that notifies observers when the value changes. Good for state management.
    /// - Maintains current value
    /// - vNotifies only on actual value changes
    /// - Immediate notification of current value to new subscribers
    /// - Thread-safe notification
    /// </summary>
    /// <usage>
    /// var health = new ReactiveProperty<int>(100);
    /// health.Subscribe(newHealth => UpdateHealthUI(newHealth));
    /// health.Value = 90;
    /// </usage> 
    public class ReactiveProperty<T> : IObservable<T>
    {
        private T value;
        private readonly List<IObserver<T>> observers = new List<IObserver<T>>();

        public virtual T Value
        {
            get => value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(this.value, value)) return;
                this.value = value;
                NotifyObservers();
            }
        }

        public ReactiveProperty(T initialValue)
        {
            value = initialValue;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
                // Immediately notify new observer of current value
                observer.OnNext(value);
            }
            return new Subscription(() => observers.Remove(observer));
        }

        private void NotifyObservers()
        {
            foreach (var observer in observers.ToArray()) // Create copy to avoid modification during iteration
            {
                observer.OnNext(value);
            }
        }
    }
}