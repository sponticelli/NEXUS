using System;
using UnityEngine;

namespace Nexus.Core.Rx
{
    public interface IObserver<T>
    {
        void OnNext(T value);
        void OnError(Exception error);
        void OnCompleted();
    }
}