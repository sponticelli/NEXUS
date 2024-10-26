using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Provides error handling and recovery capabilities for services
    /// </summary>
    public class ServiceErrorHandler
    {
        private readonly IServiceLifecycle service;
        private readonly int maxRetries;
        private readonly TimeSpan retryDelay;

        public ServiceErrorHandler(IServiceLifecycle service, int maxRetries = 3, TimeSpan? retryDelay = null)
        {
            this.service = service;
            this.maxRetries = maxRetries;
            this.retryDelay = retryDelay ?? TimeSpan.FromSeconds(1);
        }

        public async Task<bool> TryRecoverAsync()
        {
            if (service.State != ServiceState.Failed)
            {
                return true;
            }

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    Debug.Log($"Attempting service recovery ({i + 1}/{maxRetries})...");
                    await service.Initialize();
                    Debug.Log("Service recovery successful!");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Recovery attempt {i + 1} failed: {ex}");
                    
                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(retryDelay);
                    }
                }
            }

            Debug.LogError($"Service recovery failed after {maxRetries} attempts");
            return false;
        }
    }
}