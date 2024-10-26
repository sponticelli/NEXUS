using UnityEngine;

namespace Nexus.Core.Bootstrap.Scenes
{
    [System.Serializable]
    public class ServiceOverride
    {
        public string ServiceName;
        public ServiceImplementationType ImplementationType;
        public ScriptableObject DebugImplementation;
        public MonoBehaviour CustomImplementation;

        // Helper property to make inspector clearer
        public bool IsDebugScriptableObject => ImplementationType == ServiceImplementationType.Debug;
        public bool IsCustomComponent => ImplementationType == ServiceImplementationType.Custom;
    }
}