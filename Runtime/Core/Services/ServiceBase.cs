using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Bootstrap;
using UnityEngine;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Base class for all services in the system
    /// </summary>
    public abstract class ServiceBase : IServiceLifecycle, IInitializable, IDisposable
    {
        private readonly object stateLock = new object();
        private ServiceState state = ServiceState.Uninitialized;
        private CancellationTokenSource shutdownCts;
        private TaskCompletionSource<bool> initializationComplete;
        private readonly List<IDisposable> managedResources = new List<IDisposable>();

        public ServiceState State
        {
            get
            {
                lock (stateLock)
                {
                    return state;
                }
            }
            private set
            {
                lock (stateLock)
                {
                    state = value;
                }
            }
        }

        protected bool IsInitialized => State == ServiceState.Running;
        protected CancellationToken ShutdownToken => shutdownCts?.Token ?? CancellationToken.None;

        public async Task Initialize(CancellationToken cancellationToken = default)
        {
            if (State != ServiceState.Uninitialized)
            {
                throw new InvalidOperationException($"Cannot initialize service in state: {State}");
            }

            try
            {
                State = ServiceState.Initializing;
                shutdownCts = new CancellationTokenSource();
                initializationComplete = new TaskCompletionSource<bool>();

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, shutdownCts.Token);

                await OnInitializing(linkedCts.Token);
                State = ServiceState.Running;
                initializationComplete.TrySetResult(true);
            }
            catch (Exception ex)
            {
                State = ServiceState.Failed;
                initializationComplete?.TrySetException(ex);
                throw;
            }
        }

        Task IInitializable.Initialize() => Initialize();

        public async Task Shutdown()
        {
            if (State != ServiceState.Running)
            {
                return;
            }

            try
            {
                State = ServiceState.ShuttingDown;
                shutdownCts?.Cancel();
                await OnShutdown();
            }
            finally
            {
                State = ServiceState.Disposed;
                DisposeResources();
            }
        }

        public void Dispose()
        {
            if (State == ServiceState.Disposed)
            {
                return;
            }

            try
            {
                Shutdown().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during service disposal: {ex}");
            }

            GC.SuppressFinalize(this);
        }

        protected virtual Task OnInitializing(CancellationToken cancellationToken) => Task.CompletedTask;
        protected virtual Task OnShutdown() => Task.CompletedTask;

        protected void RegisterManagedResource(IDisposable resource)
        {
            if (resource == null) throw new ArgumentNullException(nameof(resource));
            managedResources.Add(resource);
        }

        private void DisposeResources()
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