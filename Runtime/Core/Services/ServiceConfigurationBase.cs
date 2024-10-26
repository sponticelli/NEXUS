using UnityEngine;

namespace Nexus.Core.Services
{
    /// <summary>
    /// Base class for service configurations
    /// </summary>
    public abstract class ServiceConfigurationBase : ScriptableObject
    {
        [SerializeField] private bool isDebugConfig;
        
        public bool IsDebugConfig => isDebugConfig;
        
        // Optional validation method that configurations can implement
        public virtual bool Validate(out string error)
        {
            error = null;
            return true;
        }
    }
}