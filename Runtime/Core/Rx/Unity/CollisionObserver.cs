using System;
using UnityEngine;

namespace Nexus.Core.Rx.Unity
{
    /// <summary>
    /// Helper component for observing collision events
    /// </summary>
    internal class CollisionObserver : MonoBehaviour
    {
        public event Action<Collision> OnCollisionEnterEvent;

        private void OnCollisionEnter(Collision collision)
        {
            OnCollisionEnterEvent?.Invoke(collision);
        }
    }
}