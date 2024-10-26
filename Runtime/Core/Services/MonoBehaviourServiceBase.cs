using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Base class for services that require MonoBehaviour functionality
    /// </summary>
    public abstract class MonoBehaviourServiceBase : MonoBehaviour, IServiceLifecycle
    {
        private ServiceState currentState = ServiceState.Uninitialized;
        private CancellationTokenSource shutdownCts;
        private readonly List<IDisposable> managedResources = new List<IDisposable>();

        public ServiceState State => currentState;

        protected CancellationToken ShutdownToken => shutdownCts?.Token ?? CancellationToken.None;

        public async Task Initialize(CancellationToken cancellationToken = default)
        {
            if (currentState != ServiceState.Uninitialized)
            {
                throw new InvalidOperationException($"Cannot initialize service in state: {currentState}");
            }

            try
            {
                currentState = ServiceState.Initializing;
                shutdownCts = new CancellationTokenSource();

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, shutdownCts.Token);
                await OnInitialize(linkedCts.Token);
                
                currentState = ServiceState.Running;
            }
            catch (Exception)
            {
                currentState = ServiceState.Failed;
                throw;
            }
        }

        public async Task Shutdown()
        {
            if (currentState != ServiceState.Running)
                return;

            try
            {
                currentState = ServiceState.ShuttingDown;
                shutdownCts?.Cancel();
                await OnShutdown();
                CleanupResources();
                currentState = ServiceState.Disposed;
            }
            catch (Exception ex)
            {
                currentState = ServiceState.Failed;
                Debug.LogError($"Error during service shutdown: {ex}");
                throw;
            }
        }

        protected virtual void OnDestroy()
        {
            if (currentState == ServiceState.Running)
            {
                Shutdown().GetAwaiter().GetResult();
            }
            CleanupResources();
        }

        protected virtual Task OnInitialize(CancellationToken cancellationToken) => Task.CompletedTask;
        protected virtual Task OnShutdown() => Task.CompletedTask;

        protected void RegisterManagedResource(IDisposable resource)
        {
            managedResources.Add(resource);
        }

        private void CleanupResources()
        {
            foreach (var resource in managedResources)
            {
                try
                {
                    resource.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error disposing resource: {ex}");
                }
            }

            managedResources.Clear();
            shutdownCts?.Dispose();
            shutdownCts = null;
        }
    }
}