using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Extension methods for service error handling
    /// </summary>
    public static class ServiceErrorHandlingExtensions
    {
        public static async Task WithErrorHandling(this Task serviceTask, string operationName)
        {
            try
            {
                await serviceTask;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"Operation '{operationName}' was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during '{operationName}': {ex}");
                throw new ServiceException($"Service operation '{operationName}' failed", ServiceState.Failed, ex);
            }
        }

        public static async Task<T> WithErrorHandling<T>(this Task<T> serviceTask, string operationName)
        {
            try
            {
                return await serviceTask;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"Operation '{operationName}' was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during '{operationName}': {ex}");
                throw new ServiceException($"Service operation '{operationName}' failed", ServiceState.Failed, ex);
            }
        }
    }
}