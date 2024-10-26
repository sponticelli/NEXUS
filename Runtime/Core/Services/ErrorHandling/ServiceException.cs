using System;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Custom exception for service-related errors
    /// </summary>
    public class ServiceException : Exception
    {
        public ServiceState ServiceState { get; }

        public ServiceException(string message, ServiceState state, Exception innerException = null)
            : base(message, innerException)
        {
            ServiceState = state;
        }
    }
}