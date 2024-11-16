using System;
using UnityEngine;

namespace Nexus.Core.Rx
{
    /// <summary>
    /// A simple observer that takes delegates for each of the observer methods
    /// </summary>
    public class AnonymousObserver<T> : IObserver<T>
    {
        private readonly Action<T> onNext;
        private readonly Action<Exception> onError;
        private readonly Action onCompleted;

        public AnonymousObserver(
            Action<T> onNext, 
            Action<Exception> onError = null, 
            Action onCompleted = null)
        {
            this.onNext = onNext ?? throw new ArgumentNullException(nameof(onNext));
            this.onError = onError ?? (ex => Debug.LogException(ex));
            this.onCompleted = onCompleted ?? (() => { });
        }

        public void OnNext(T value) => onNext(value);
        public void OnError(Exception error) => onError(error);
        public void OnCompleted() => onCompleted();
    }
}