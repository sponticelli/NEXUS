namespace Nexus.Core
{
    public static class ExecutionOrder
    {
        // Execution order
        public const int ServiceLocator = -1000;
        public const int ServiceBootstrapper =  ServiceLocator + 100;
        
        public const int SceneRegister = ServiceBootstrapper + 200;
    }
}