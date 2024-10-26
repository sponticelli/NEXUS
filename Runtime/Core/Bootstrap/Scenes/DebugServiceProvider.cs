using System.Threading.Tasks;
using UnityEngine;

namespace Nexus.Core.Bootstrap.Scenes
{
    public abstract class DebugServiceProvider : ScriptableObject
    {
        [SerializeField] protected bool logServiceCalls = true;
        [SerializeField] protected bool simulateDelay;
        [SerializeField] protected float simulatedDelay = 0.5f;

        protected async Task LogServiceCall([System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
        {
            if (logServiceCalls)
            {
                UnityEngine.Debug.Log($"[Debug Service] {GetType().Name}.{methodName} called");
            }

            if (simulateDelay)
            {
                await Task.Delay((int)(simulatedDelay * 1000));
            }
        }
    }
}