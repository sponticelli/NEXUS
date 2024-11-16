using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Core.Rx.Unity
{
    public class ObservableMonoBehaviour : MonoBehaviour
    {
        private readonly List<IDisposable> disposables = new List<IDisposable>();

        protected void AddDisposable(IDisposable disposable)
        {
            disposables.Add(disposable);
        }

        protected virtual void OnDestroy()
        {
            foreach (var disposable in disposables)
            {
                disposable?.Dispose();
            }
            disposables.Clear();
        }
    }
}