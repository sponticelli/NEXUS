using UnityEngine;

namespace Paths.TwoD
{
    public class LinePath : IPath
    {
        private Vector2 start;
        private Vector2 end;

        public LinePath(Vector2 start, Vector2 end)
        {
            this.start = start;
            this.end = end;
        }
        
        
        public Vector2 Evaluate(float t)
        {
            return Vector2.Lerp(start, end, t);
        }

        public float GetLength()
        {
            return Vector2.Distance(start, end);
        }

        public float GetParameterAtDistance(float distance)
        {
            return distance / GetLength();
        }
    }
}