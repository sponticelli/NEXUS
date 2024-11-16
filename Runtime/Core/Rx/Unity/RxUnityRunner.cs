using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Nexus.Core.Rx.Unity
{
    /// <summary>
    /// Centralized runner for all Rx operations requiring MonoBehaviour functionality
    /// </summary>
    public class RxUnityRunner : MonoBehaviour
    {
        private static RxUnityRunner instance;
        private static readonly object gate = new object();
        private readonly ConcurrentDictionary<string, ConcurrentBag<IDisposable>> sceneSubscriptions 
            = new ConcurrentDictionary<string, ConcurrentBag<IDisposable>>();
        
        public static RxUnityRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (gate)
                    {
                        if (instance == null)
                        {
                            var go = new GameObject("[Rx Unity Runner]");
                            DontDestroyOnLoad(go);
                            instance = go.AddComponent<RxUnityRunner>();
                        }
                    }
                }
                return instance;
            }
        }

        public void RegisterSceneSubscription(string sceneName, IDisposable subscription)
        {
            var subscriptions = sceneSubscriptions.GetOrAdd(sceneName, _ => new ConcurrentBag<IDisposable>());
            subscriptions.Add(subscription);
        }

        public void CleanupSceneSubscriptions(string sceneName)
        {
            if (sceneSubscriptions.TryRemove(sceneName, out var subscriptions))
            {
                foreach (var subscription in subscriptions)
                {
                    subscription?.Dispose();
                }
            }
        }

        private void OnApplicationQuit()
        {
            foreach (var kvp in sceneSubscriptions)
            {
                foreach (var subscription in kvp.Value)
                {
                    subscription?.Dispose();
                }
            }
            sceneSubscriptions.Clear();
            instance = null;
        }
    }
}