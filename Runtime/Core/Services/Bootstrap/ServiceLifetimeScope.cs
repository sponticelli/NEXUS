namespace Nexus.Core.Services
{
    /// <summary>
    /// Defines the lifetime scope of a service
    /// </summary>
    public enum ServiceLifetimeScope
    {
        Singleton,       // Lives for the entire game session
        SceneScoped,    // Lives for the duration of a scene
        Transient       // Created on demand, short-lived
    }
}