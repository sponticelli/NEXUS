using System;
using System.Collections.Generic;
using Nexus.Core.Services;
using UnityEngine;

namespace Nexus.Registries
{
    [ServiceInterface]
    public interface IComponentRegistry
    {
        event Action<Type, MonoBehaviour> OnRegistered;
        event Action<Type, MonoBehaviour> OnDeRegistered;
        void Register(MonoBehaviour obj);
        void Deregister(MonoBehaviour obj);
        bool IsRegistered(MonoBehaviour obj);
        T Get<T>(Predicate<T> predicate = null) where T : MonoBehaviour;
        List<T> GetAll<T>(Predicate<T> predicate = null) where T : MonoBehaviour;
        void SubscribeToRegister<T>(Action<T> callback) where T : MonoBehaviour;
        void UnsubscribeFromRegister<T>(Action<T> callback) where T : MonoBehaviour;
        void SubscribeToDeRegister<T>(Action<T> callback) where T : MonoBehaviour;
        void UnsubscribeFromDeRegister<T>(Action<T> callback) where T : MonoBehaviour;
        void Clear();
    }
}