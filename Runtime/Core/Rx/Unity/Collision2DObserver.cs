using System;
using UnityEngine;

namespace Nexus.Core.Rx.Unity
{
    /// <summary>
    /// Helper component for observing collision events
    /// </summary>
    internal class Collision2DObserver : MonoBehaviour
    {
        public event Action<Collision2D> OnCollision2DEnterEvent;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            OnCollision2DEnterEvent?.Invoke(collision);
        }
    }
}