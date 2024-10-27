using System;

namespace Nexus.Core.Services
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ServiceDependencyAttribute : Attribute
    {
        public Type DependencyType { get; }

        public ServiceDependencyAttribute(Type dependencyType)
        {
            DependencyType = dependencyType;
        }
    }
}