using System;
using UnityEngine;

namespace Nexus.Core.Rx.Unity
{
    /// <summary>
    /// Helper component for observing collision events
    /// </summary>
    internal class Collision2DObserver : MonoBehaviour
    {
        public event Action<Collision2D> OnCollisionEnterEvent;

        private void OnCollisionEnter(Collision2D collision)
        {
            OnCollisionEnterEvent?.Invoke(collision);
        }
    }
}