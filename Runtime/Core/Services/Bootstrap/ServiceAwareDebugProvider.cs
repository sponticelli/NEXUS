using System;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Bootstrap.Scenes;
using UnityEngine;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Example debug service provider that integrates with the service lifecycle
    /// </summary>
    public abstract class ServiceAwareDebugProvider : DebugServiceProvider, IServiceLifecycle
    {
        private ServiceState currentState = ServiceState.Uninitialized;
        
        public ServiceState State => currentState;

        public async Task Initialize(CancellationToken cancellationToken = default)
        {
            if (currentState != ServiceState.Uninitialized)
                return;

            currentState = ServiceState.Initializing;
            
            try
            {
                await OnInitialize(cancellationToken);
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

            currentState = ServiceState.ShuttingDown;
            
            try
            {
                await OnShutdown();
                currentState = ServiceState.Disposed;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during debug service shutdown: {ex}");
                currentState = ServiceState.Failed;
                throw;
            }
        }

        protected virtual Task OnInitialize(CancellationToken cancellationToken) => Task.CompletedTask;
        protected virtual Task OnShutdown() => Task.CompletedTask;
    }
}