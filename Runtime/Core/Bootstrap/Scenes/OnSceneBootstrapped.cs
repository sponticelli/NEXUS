using UnityEngine;

namespace Nexus.Core.Bootstrap.Scenes
{
    public class OnSceneBootstrapped : MonoBehaviour
    {
        [SerializeField] private SceneBootstrapper bootstrapper;
        
        [SerializeField] private UnityEngine.Events.UnityEvent onBootstrapped;
        
        private void Start()
        {
            // If bootstrap is null search for it in the scene
            if (bootstrapper == null)
            {
                bootstrapper = FindFirstObjectByType<SceneBootstrapper>();
            }
            
            if (bootstrapper.IsInitialized)
            {
                onBootstrapped.Invoke();
            }
            else
            {
                bootstrapper.WaitForInitialization().ContinueWith(_ => onBootstrapped.Invoke());
            }
        }
        
    }
}