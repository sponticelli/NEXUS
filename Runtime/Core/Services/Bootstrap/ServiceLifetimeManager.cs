using System;
using Nexus.Core.Bootstrap;
using UnityEngine;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Service lifetime management utilities
    /// </summary>
    public static class ServiceLifetimeManager
    {
        public static void ValidateServiceRegistration(Type serviceType, object implementation)
        {
            // Validate singleton services
            if (implementation is ISingletonService)
            {
                if (GameInitializer.Instance.HasService(serviceType))
                {
                    throw new InvalidOperationException(
                        $"Attempted to register singleton service {serviceType.Name} more than once. " +
                        "Singleton services should only be registered once.");
                }
            }
            
            // Validate that implementations match their lifetime markers
            ValidateLifetimeConsistency(serviceType, implementation);
        }

        private static void ValidateLifetimeConsistency(Type serviceType, object implementation)
        {
            var implType = implementation.GetType();
            
            // Check for conflicting lifetime markers
            bool isSingleton = typeof(ISingletonService).IsAssignableFrom(implType);
            bool isScoped = typeof(IScopedService).IsAssignableFrom(implType);
            bool isTransient = typeof(ITransientService).IsAssignableFrom(implType);

            int lifetimeCount = 0;
            if (isSingleton) lifetimeCount++;
            if (isScoped) lifetimeCount++;
            if (isTransient) lifetimeCount++;

            if (lifetimeCount > 1)
            {
                throw new InvalidOperationException(
                    $"Service {implType.Name} has multiple lifetime markers. " +
                    "A service should only implement one lifetime interface.");
            }

            if (lifetimeCount == 0)
            {
                Debug.LogWarning(
                    $"Service {implType.Name} does not implement any lifetime marker interface. " +
                    "Consider implementing ISingletonService, IScopedService, or ITransientService.");
            }
        }
    }
}