using System;
using System.Collections.Generic;

namespace Nexus.Core.Rx.Operators
{
    public class CompositeDisposable : IDisposable
    {
        private List<IDisposable> disposables;
        private bool disposed;

        public CompositeDisposable(params IDisposable[] disposables)
        {
            this.disposables = new List<IDisposable>(disposables);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                foreach (var disposable in disposables)
                {
                    disposable?.Dispose();
                }
            }
        }

        public void Add(IDisposable subscribe)
        {
            if (disposed)
            {
                subscribe.Dispose();
            }
            else
            {
                disposables.Add(subscribe);
            }
        }
    }
}