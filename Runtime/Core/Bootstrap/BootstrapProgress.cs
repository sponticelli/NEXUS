using System;

namespace Nexus.Core.Bootstrap
{
    /// <summary>
    /// Contains information about the current bootstrap progress
    /// </summary>
    public class BootstrapProgress
    {
        public BootstrapStage Stage { get; }
        public int CurrentService { get; }
        public int TotalServices { get; }
        public string CurrentServiceName { get; }
        public float Progress => TotalServices > 0 ? (float)CurrentService / TotalServices : 0f;
        public Exception Error { get; }

        public BootstrapProgress(BootstrapStage stage, int current, int total, string serviceName = null, Exception error = null)
        {
            Stage = stage;
            CurrentService = current;
            TotalServices = total;
            CurrentServiceName = serviceName;
            Error = error;
        }
    }
}