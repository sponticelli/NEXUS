namespace Nexus.Core.Bootstrap.Scenes
{
    public enum ServiceImplementationType
    {
        Default,    // Use the normal service implementation
        Debug,      // Use debug ScriptableObject implementation
        Custom      // Use custom implementation provided in inspector
    }
}