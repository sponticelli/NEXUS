using System;

namespace Nexus.Core.ServiceLocation
{
    public class ServiceRegistry
    {
        public Type ImplementationType { get; set; }
        public ServiceLifetime Lifetime { get; set; }
        public object SingletonInstance { get; set; }
        public bool IsMonoBehaviour { get; set; }
        public Func<object> Factory { get; set; }
        public object Configuration { get; set; }
        public Type[] Dependencies { get; set; }
    }
}