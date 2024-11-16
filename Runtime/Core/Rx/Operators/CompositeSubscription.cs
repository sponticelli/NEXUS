using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Core.Rx.Operators
{
    /// <summary>
    /// CompositeSubscription with fluent API and thread safety
    /// 
    /// </summary>
    public class CompositeSubscription : IDisposable
    {
        private readonly HashSet<IDisposable> subscriptions = new HashSet<IDisposable>();
        private readonly object gate = new object();
        private bool disposed;

        public CompositeSubscription Add(IDisposable subscription)
        {
            if (subscription == null) return this;

            bool shouldDispose = false;
            lock (gate)
            {
                if (!disposed)
                {
                    subscriptions.Add(subscription);
                }
                else
                {
                    shouldDispose = true;
                }
            }

            if (shouldDispose)
            {
                subscription.Dispose();
            }

            return this;
        }

        public void Remove(IDisposable subscription)
        {
            lock (gate)
            {
                if (!disposed)
                {
                    subscriptions.Remove(subscription);
                }
            }
        }

        public void Dispose()
        {
            IDisposable[] toDispose = null;

            lock (gate)
            {
                if (!disposed)
                {
                    disposed = true;
                    toDispose = subscriptions.ToArray();
                    subscriptions.Clear();
                }
            }

            if (toDispose != null)
            {
                foreach (var subscription in toDispose)
                {
                    subscription?.Dispose();
                }
            }
        }
    }
}