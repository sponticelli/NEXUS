using Nexus.Core;
using Nexus.Core.ServiceLocation;
using UnityEngine;

namespace Nexus.Registries
{
    [DefaultExecutionOrder(ExecutionOrder.SceneRegister)]
    public class ComponentRegister<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField] private T _object;
        [SerializeField] private bool _findComponentIfNull = true;
        [SerializeField] private bool _deregisterOnDestroy = true;

        private IComponentRegistry _registry;
        private bool _isRegistered;

        private void Awake()
        {
            _registry = ServiceLocator.Instance.GetService<IComponentRegistry>();
            
            if (_object == null && _findComponentIfNull)
                _object = GetComponent<T>();

            if (_object == null)
                Debug.LogError($"No {typeof(T).Name} component assigned or found on {gameObject.name}", this);
        }

        private void Start()
        {
            if (_object != null && !_isRegistered)
            {
                _registry.Register(_object);
                _isRegistered = true;
            }
        }

        private void OnDestroy()
        {
            if (_deregisterOnDestroy && _registry != null && _object != null && _isRegistered)
            {
                _registry.Deregister(_object);
                _isRegistered = false;
            }
        }

        private void OnValidate()
        {
            if (_object == null && _findComponentIfNull)
                _object = GetComponent<T>();
        }
    }
}