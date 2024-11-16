using System;
using UnityEngine;

namespace Nexus.Core.Rx.Operators
{
    /// <summary>
    /// Represents an observer that throttles the source observer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ThrottleObserver<T> : IObserver<T>
    {
        private readonly IObserver<T> observer;
        private readonly float interval;
        private float lastEmitTime;

        public ThrottleObserver(IObserver<T> observer, float interval)
        {
            this.observer = observer;
            this.interval = interval;
            this.lastEmitTime = -interval; // Allow first emission
        }

        public void OnNext(T value)
        {
            var currentTime = Time.time;
            if (currentTime - lastEmitTime >= interval)
            {
                observer.OnNext(value);
                lastEmitTime = currentTime;
            }
        }

        public void OnError(Exception error) => observer.OnError(error);
        public void OnCompleted() => observer.OnCompleted();
    }
}