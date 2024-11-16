using System;

namespace Nexus.Core.Rx
{
    public interface IObservable<T>
    {
        IDisposable Subscribe(IObserver<T> observer);
    }


}