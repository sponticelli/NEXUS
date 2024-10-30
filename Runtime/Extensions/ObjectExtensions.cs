using System.Collections.Generic;

namespace Nexus.Extensions
{
    public static class ObjectExtensions
    {
        public static IEnumerable<T> Yield<T>(this T item)
        {
            if (item != null) yield return item;
        }
    }
}