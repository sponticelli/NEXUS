using System;
using System.Collections;
using System.Collections.Generic;
using Nexus.Core.Rx.Unity;
using UnityEngine;

namespace Nexus.Core.Rx.Operators
{
    /// <summary>
    /// Helper class for buffer timing
    /// </summary>
    internal class BufferTimer<T> : IDisposable
    {
        private readonly List<T> buffer;
        private readonly object gate;
        private readonly IObserver<List<T>> observer;
        private readonly float interval;
        private readonly bool useUnscaledTime;
        private readonly ObjectPool<List<T>> pool;
        private bool disposed;
        private float lastEmitTime;

        public BufferTimer(
            List<T> buffer,
            object gate,
            IObserver<List<T>> observer,
            float interval,
            bool useUnscaledTime,
            ObjectPool<List<T>> pool)
        {
            this.buffer = buffer;
            this.gate = gate;
            this.observer = observer;
            this.interval = interval;
            this.useUnscaledTime = useUnscaledTime;
            this.pool = pool;
            this.lastEmitTime = GetCurrentTime();

            RxUnityRunner.Instance.StartCoroutine(TimerRoutine());
        }

        private float GetCurrentTime() => 
            useUnscaledTime ? Time.unscaledTime : Time.time;

        private IEnumerator TimerRoutine()
        {
            while (!disposed)
            {
                yield return new WaitForSeconds(interval);

                EmitBuffer();
            }
        }

        private void EmitBuffer()
        {
            if (disposed) return;

            lock (gate)
            {
                if (buffer.Count > 0)
                {
                    var newBuffer = pool.Rent();
                    newBuffer.AddRange(buffer);
                    observer.OnNext(newBuffer);
                    pool.Return(newBuffer);
                    buffer.Clear();
                }
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                EmitBuffer();
            }
        }
    }
}