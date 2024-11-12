using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Services;
using UnityEngine;

namespace Nexus.Registries
{
    [ServiceImplementation]
    public class ComponentRegistry : MonoBehaviour, IComponentRegistry
    {
        private readonly object _lock = new object();
        private readonly Dictionary<Type, List<MonoBehaviour>> _registry = new Dictionary<Type, List<MonoBehaviour>>();
        private readonly Dictionary<Type, HashSet<MonoBehaviour>> _registrySet = new Dictionary<Type, HashSet<MonoBehaviour>>();
        private readonly Dictionary<Type, Action<MonoBehaviour>> _typeRegisteredEvents = new Dictionary<Type, Action<MonoBehaviour>>();
        private readonly Dictionary<Type, Action<MonoBehaviour>> _typeDeRegisteredEvents = new Dictionary<Type, Action<MonoBehaviour>>();

        public event Action<Type, MonoBehaviour> OnRegistered = delegate { };
        public event Action<Type, MonoBehaviour> OnDeRegistered = delegate { };

        public void Register(MonoBehaviour obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            lock (_lock)
            {
                var type = obj.GetType();
                
                // Check if already registered using HashSet for O(1) lookup
                if (!_registrySet.ContainsKey(type))
                    _registrySet[type] = new HashSet<MonoBehaviour>();
                
                if (!_registrySet[type].Add(obj))
                    return; // Already registered

                // Add to list for ordered access
                if (!_registry.ContainsKey(type))
                    _registry[type] = new List<MonoBehaviour>();
                
                _registry[type].Add(obj);

                try
                {
                    OnRegistered?.Invoke(type, obj);
                    if (_typeRegisteredEvents.TryGetValue(type, out var handler))
                        handler?.Invoke(obj);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in registration callbacks for {type}: {e}");
                }
            }
        }

        public void Deregister(MonoBehaviour obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            lock (_lock)
            {
                var type = obj.GetType();
                
                if (!_registrySet.ContainsKey(type) || !_registrySet[type].Remove(obj))
                    return; // Wasn't registered

                if (_registry.ContainsKey(type))
                {
                    _registry[type].Remove(obj);
                    if (_registry[type].Count == 0)
                    {
                        _registry.Remove(type);
                        _registrySet.Remove(type);
                    }
                }

                try
                {
                    OnDeRegistered?.Invoke(type, obj);
                    if (_typeDeRegisteredEvents.TryGetValue(type, out var handler))
                        handler?.Invoke(obj);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in deregistration callbacks for {type}: {e}");
                }
            }
        }

        public bool IsRegistered(MonoBehaviour obj)
        {
            if (obj == null)
                return false;

            var type = obj.GetType();
            lock (_lock)
            {
                return _registrySet.ContainsKey(type) && _registrySet[type].Contains(obj);
            }
        }

        public T Get<T>(Predicate<T> predicate = null) where T : MonoBehaviour
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (!_registry.ContainsKey(type) || _registry[type].Count == 0)
                    return null;

                if (predicate == null)
                    return _registry[type][0] as T;

                return _registry[type].Cast<T>().FirstOrDefault(x => predicate(x));
            }
        }

        public List<T> GetAll<T>(Predicate<T> predicate = null) where T : MonoBehaviour
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (!_registry.ContainsKey(type))
                    return new List<T>();

                var results = _registry[type].Cast<T>();
                return predicate == null ? results.ToList() : results.Where(x => predicate(x)).ToList();
            }
        }

        public void SubscribeToRegister<T>(Action<T> callback) where T : MonoBehaviour
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            var type = typeof(T);
            lock (_lock)
            {
                if (!_typeRegisteredEvents.ContainsKey(type))
                    _typeRegisteredEvents[type] = obj => callback((T)obj);
                else
                    _typeRegisteredEvents[type] += obj => callback((T)obj);
            }
        }

        public void UnsubscribeFromRegister<T>(Action<T> callback) where T : MonoBehaviour
        {
            if (callback == null)
                return;

            var type = typeof(T);
            lock (_lock)
            {
                if (_typeRegisteredEvents.ContainsKey(type))
                    _typeRegisteredEvents[type] -= obj => callback((T)obj);
            }
        }

        public void SubscribeToDeRegister<T>(Action<T> callback) where T : MonoBehaviour
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            var type = typeof(T);
            lock (_lock)
            {
                if (!_typeDeRegisteredEvents.ContainsKey(type))
                    _typeDeRegisteredEvents[type] = obj => callback((T)obj);
                else
                    _typeDeRegisteredEvents[type] += obj => callback((T)obj);
            }
        }

        public void UnsubscribeFromDeRegister<T>(Action<T> callback) where T : MonoBehaviour
        {
            if (callback == null)
                return;

            var type = typeof(T);
            lock (_lock)
            {
                if (_typeDeRegisteredEvents.ContainsKey(type))
                    _typeDeRegisteredEvents[type] -= obj => callback((T)obj);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _registry.Clear();
                _registrySet.Clear();
                _typeRegisteredEvents.Clear();
                _typeDeRegisteredEvents.Clear();
            }
        }

        private void OnDestroy()
        {
            Clear();
            OnRegistered = null;
            OnDeRegistered = null;
        }
    }
}