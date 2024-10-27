using System;
using System.Linq;

namespace Nexus.Core.ServiceLocation
{
    public class DependencyInjector : IDependencyInjector
    {
        private readonly IServiceResolver resolver;

        public DependencyInjector(IServiceResolver resolver)
        {
            this.resolver = resolver;
        }

        public Type[] GetDependencies(Type type)
        {
            var constructor = type.GetConstructors()
                                  .FirstOrDefault(c =>
                                      c.GetCustomAttributes(typeof(ServiceConstructorAttribute), true).Any())
                              ?? type.GetConstructors()
                                  .OrderByDescending(c => c.GetParameters().Length)
                                  .FirstOrDefault();

            return constructor?.GetParameters()
                .Select(p => p.ParameterType)
                .ToArray() ?? Array.Empty<Type>();
        }

        public void InjectProperties(object instance)
        {
            var properties = instance.GetType()
                .GetProperties()
                .Where(p => p.CanWrite && resolver.CanResolve(p.PropertyType));

            foreach (var property in properties)
            {
                var service = resolver.ResolveType(property.PropertyType);
                property.SetValue(instance, service);
            }
        }
    }
}