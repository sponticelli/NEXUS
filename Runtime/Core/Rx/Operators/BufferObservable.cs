using System;
using System.Collections.Generic;
using Nexus.Core.Rx.Unity;

namespace Nexus.Core.Rx.Operators
{
    /// <summary>
    /// Time-based buffering of values
    /// Thread-safe operation
    /// Clean resource disposal
    /// Integration with Unity's coroutine system
    /// </summary>
    /// <usage>
    ///     // Buffer input events for combo system
    ///     inputEvents
    ///         .Buffer(0.5f)
    ///         .Subscribe(inputs => {
    ///             if (IsCombo(inputs))
    ///                 ExecuteCombo();
    ///         });
    /// 
    ///     // Accumulate damage
    ///     damageEvents
    ///         .Buffer(1f)
    ///         .Subscribe(damages => {
    ///             float total = damages.Sum();
    ///             ApplyDamage(total);
    ///         });
    /// </usage>
    public class BufferObservable<T> : IObservable<List<T>>
    {
        private static readonly ObjectPool<List<T>> ListPool = 
            new ObjectPool<List<T>>(
                () => new List<T>(),
                list => list.Clear(),
                maxSize: 20
            );

        private readonly IObservable<T> source;
        private readonly float bufferTimeSeconds;
        private readonly bool useUnscaledTime;

        public BufferObservable(
            IObservable<T> source, 
            float seconds,
            bool useUnscaledTime = false)
        {
            this.source = source;
            this.bufferTimeSeconds = seconds;
            this.useUnscaledTime = useUnscaledTime;
        }

        public IDisposable Subscribe(IObserver<List<T>> observer)
        {
            var buffer = ListPool.Rent();
            var gate = new object();
            var runner = RxUnityRunner.Instance;

            var sourceSubscription = source.Subscribe(
                value =>
                {
                    try
                    {
                        lock (gate)
                        {
                            buffer.Add(value);
                        }
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(new RxException("Buffer.OnNext", value, ex));
                    }
                },
                error => 
                {
                    ListPool.Return(buffer);
                    observer.OnError(error);
                },
                () =>
                {
                    lock (gate)
                    {
                        if (buffer.Count > 0)
                        {
                            var finalBuffer = ListPool.Rent();
                            finalBuffer.AddRange(buffer);
                            observer.OnNext(finalBuffer);
                            ListPool.Return(finalBuffer);
                        }
                        ListPool.Return(buffer);
                        observer.OnCompleted();
                    }
                }
            );

            return new CompositeSubscription()
                .Add(sourceSubscription)
                .Add(new BufferTimer<T>(buffer, gate, observer, bufferTimeSeconds, useUnscaledTime, ListPool));
        }
    }
}