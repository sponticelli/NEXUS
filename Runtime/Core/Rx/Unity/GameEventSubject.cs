using UnityEngine;

namespace Nexus.Core.Rx.Unity
{
    public class GameEventSubject<T> : Subject<T>
    {
        private readonly string debugName;

        public GameEventSubject(string debugName = null)
        {
            this.debugName = debugName;
        }

        public override void OnNext(T value)
        {
            if (debugName != null)
            {
                Debug.Log($"[{debugName}] Event: {value}");
            }
            base.OnNext(value);
        }
    }
}