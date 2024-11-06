using UnityEngine;

namespace Paths.TwoD
{
    public class BezierCurve : IPath
    {
        private readonly Vector2[] controlPoints;
        private readonly int resolution;
        private readonly float[] lengthLookup;
        private readonly float totalLength;

        public BezierCurve(Vector2[] controlPoints, int resolution = 100)
        {
            this.controlPoints = controlPoints;
            this.resolution = resolution;
            this.lengthLookup = new float[resolution + 1];
        
            // Precalculate length lookup table
            float accumLength = 0f;
            Vector2 prevPoint = Evaluate(0);
            lengthLookup[0] = 0f;
        
            for (int i = 1; i <= resolution; i++)
            {
                float t = i / (float)resolution;
                Vector2 currentPoint = Evaluate(t);
                accumLength += Vector2.Distance(prevPoint, currentPoint);
                lengthLookup[i] = accumLength;
                prevPoint = currentPoint;
            }
        
            totalLength = accumLength;
        }

        public Vector2 Evaluate(float t)
        {
            t = Mathf.Clamp01(t);
            return EvaluateBezier(controlPoints, t);
        }

        private Vector2 EvaluateBezier(Vector2[] points, float t)
        {
            if (points.Length == 1)
                return points[0];

            Vector2[] newPoints = new Vector2[points.Length - 1];
            for (int i = 0; i < points.Length - 1; i++)
            {
                newPoints[i] = Vector2.Lerp(points[i], points[i + 1], t);
            }

            return EvaluateBezier(newPoints, t);
        }

        public float GetLength() => totalLength;

        public float GetParameterAtDistance(float distance)
        {
            distance = Mathf.Clamp(distance, 0f, totalLength);
        
            // Binary search in the lookup table
            int low = 0;
            int high = resolution;
        
            while (low <= high)
            {
                int mid = (low + high) / 2;
                if (lengthLookup[mid] < distance)
                    low = mid + 1;
                else if (lengthLookup[mid] > distance)
                    high = mid - 1;
                else
                    return mid / (float)resolution;
            }
        
            // Interpolate between closest points
            int index = low;
            float startDist = lengthLookup[index - 1];
            float endDist = lengthLookup[index];
            float t = (distance - startDist) / (endDist - startDist);
            return ((index - 1) + t) / resolution;
        }
    }
}