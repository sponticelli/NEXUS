using System;
using System.Collections.Generic;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Helper class to track services associated with scenes
    /// </summary>
    public static class SceneContext
    {
        private static readonly Dictionary<UnityEngine.SceneManagement.Scene, List<object>> sceneServices
            = new Dictionary<UnityEngine.SceneManagement.Scene, List<object>>();

        public static void RegisterSceneService(UnityEngine.SceneManagement.Scene scene, object service)
        {
            if (!sceneServices.ContainsKey(scene))
            {
                sceneServices[scene] = new List<object>();
            }

            sceneServices[scene].Add(service);
        }

        public static void CleanupSceneServices(UnityEngine.SceneManagement.Scene scene)
        {
            if (sceneServices.TryGetValue(scene, out var services))
            {
                foreach (var service in services)
                {
                    if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                sceneServices.Remove(scene);
            }
        }
    }
}