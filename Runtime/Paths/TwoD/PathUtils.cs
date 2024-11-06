using UnityEngine;

namespace Paths.TwoD
{
    public static class PathUtils
    {
        public static IPath Chain(params IPath[] paths)
        {
            return new CompositePath(paths);
        }

        public static IPath Offset(this IPath path, Vector2 offset)
        {
            return new TransformedPath(path, (p) => p + offset);
        }

        public static IPath Scale(this IPath path, float scale)
        {
            return new TransformedPath(path, (p) => p * scale);
        }
    }
}