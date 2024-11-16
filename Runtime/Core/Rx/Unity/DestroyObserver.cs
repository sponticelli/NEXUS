using System;
using UnityEngine;

namespace Nexus.Core.Rx.Unity
{
    /// <summary>
    /// Helper component for observing Unity lifecycle events
    /// </summary>
    internal class DestroyObserver : MonoBehaviour
    {
        public event Action OnDestroyEvent;

        private void OnDestroy()
        {
            OnDestroyEvent?.Invoke();
        }
    }
}