using System;

namespace Nexus.Core.Rx
{
    /// <summary>
    /// Represents a disposable subscription.
    /// </summary>
    public class Subscription : IDisposable
    {
        private Action unsubscribe;

        public Subscription(Action unsubscribe)
        {
            this.unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            unsubscribe?.Invoke();
            unsubscribe = null;
        }
    }
}