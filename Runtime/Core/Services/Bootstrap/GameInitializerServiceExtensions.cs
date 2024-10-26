using System;
using System.Collections.Generic;
using Nexus.Core.Bootstrap;
using UnityEngine;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Extends GameInitializer with service lifecycle management capabilities
    /// </summary>
    public static class GameInitializerServiceExtensions
    {
        private static readonly Dictionary<Type, ServiceLifetimeScope> serviceScopes = new Dictionary<Type, ServiceLifetimeScope>();

        // reset the service scopes when the Application stops
        static GameInitializerServiceExtensions()
        {
            Application.quitting += ResetServiceScopes;
        }
        private static void ResetServiceScopes()
        {
            serviceScopes.Clear();
        }
        
        public static void RegisterServiceWithLifetime<T>(this GameInitializer initializer, T service, ServiceLifetimeScope scope) where T : class
        {
            var serviceType = typeof(T);
            serviceScopes[serviceType] = scope;
            initializer.RegisterService(service);
        }

        public static ServiceLifetimeScope GetServiceLifetime(this GameInitializer initializer, Type serviceType)
        {
            return serviceScopes.TryGetValue(serviceType, out var scope) ? scope : ServiceLifetimeScope.Singleton;
        }
    }
}