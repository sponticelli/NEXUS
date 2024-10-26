using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Helper class for resource management
    /// </summary>
    public class ResourceManager : SingletonServiceBase
    {
        private readonly Dictionary<string, WeakReference> cachedResources = new Dictionary<string, WeakReference>();
        private readonly object cacheLock = new object();

        public async Task<T> LoadResource<T>(string path, IProgress<float> progress = null, CancellationToken cancellationToken = default) where T : UnityEngine.Object
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check cache first
            lock (cacheLock)
            {
                if (cachedResources.TryGetValue(path, out var weakRef) && weakRef.IsAlive)
                {
                    return weakRef.Target as T;
                }
            }

            // Simulate resource loading with progress
            var resourceRequest = Resources.LoadAsync<T>(path);
            while (!resourceRequest.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(resourceRequest.progress);
                await Task.Yield();
            }

            var resource = resourceRequest.asset as T;
            if (resource == null)
            {
                throw new InvalidOperationException($"Failed to load resource at path: {path}");
            }

            // Cache the loaded resource
            lock (cacheLock)
            {
                cachedResources[path] = new WeakReference(resource);
            }

            progress?.Report(1.0f);
            return resource;
        }

        protected override async Task OnInitializing(CancellationToken cancellationToken)
        {
            await base.OnInitializing(cancellationToken);
            // Initialize resource system
        }

        protected override async Task OnShutdown()
        {
            // Clear cache
            lock (cacheLock)
            {
                cachedResources.Clear();
            }

            await base.OnShutdown();
        }
    }
}