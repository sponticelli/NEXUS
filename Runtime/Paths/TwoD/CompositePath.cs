using UnityEngine;

namespace Paths.TwoD
{
    public class CompositePath : IPath
    {
        private readonly IPath[] paths;
        private readonly float totalLength;
        private readonly float[] pathStarts;

        public CompositePath(IPath[] paths)
        {
            this.paths = paths;
            this.pathStarts = new float[paths.Length];
        
            float accumLength = 0f;
            for (int i = 0; i < paths.Length; i++)
            {
                pathStarts[i] = accumLength;
                accumLength += paths[i].GetLength();
            }
            totalLength = accumLength;
        }

        public Vector2 Evaluate(float t)
        {
            float distance = t * totalLength;
            int pathIndex = 0;
        
            for (int i = 0; i < paths.Length; i++)
            {
                if (distance < pathStarts[i] + paths[i].GetLength())
                {
                    pathIndex = i;
                    break;
                }
            }

            float localDistance = distance - pathStarts[pathIndex];
            float localT = localDistance / paths[pathIndex].GetLength();
            return paths[pathIndex].Evaluate(localT);
        }

        public float GetLength() => totalLength;

        public float GetParameterAtDistance(float distance)
        {
            return distance / totalLength;
        }
    }
}