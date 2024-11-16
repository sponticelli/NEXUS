using System;
using System.Collections.Concurrent;

namespace Nexus.Core.Rx.Operators
{
    /// <summary>
    /// Object pooling for frequently created objects
    /// </summary>
    internal class ObjectPool<T> where T : class
    {
        private readonly Func<T> factory;
        private readonly Action<T> reset;
        private readonly ConcurrentBag<T> pool;
        private readonly int maxSize;

        public ObjectPool(Func<T> factory, Action<T> reset = null, int maxSize = 50)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.reset = reset ?? (_ => { });
            this.maxSize = maxSize;
            this.pool = new ConcurrentBag<T>();
        }

        public T Rent()
        {
            return pool.TryTake(out var item) ? item : factory();
        }

        public void Return(T item)
        {
            if (item == null) return;

            if (pool.Count < maxSize)
            {
                reset(item);
                pool.Add(item);
            }
        }
    }
}