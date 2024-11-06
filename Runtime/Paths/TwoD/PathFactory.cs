using UnityEngine;

namespace Paths.TwoD
{
    public static class PathFactory
    {
        public static IPath CreateStraightPath(Vector2 start, Vector2 end)
        {
            return new LinePath(start, end);
        }
        
        
        public static IPath CreateCircularPath(Vector2 center, float radius, float startAngle = 0f)
        {
            const int segments = 8;
            Vector2[] controlPoints = new Vector2[3 * segments + 1];
        
            for (int i = 0; i <= segments; i++)
            {
                float angle = startAngle + (i * 2f * Mathf.PI / segments);
                float nextAngle = startAngle + ((i + 1) * 2f * Mathf.PI / segments);
            
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Vector2 nextPoint = center + new Vector2(Mathf.Cos(nextAngle), Mathf.Sin(nextAngle)) * radius;
            
                int index = i * 3;
                controlPoints[index] = point;
            
                if (i < segments)
                {
                    float handleLength = radius * 0.552284749831f; // Magic number for circular approximation
                    controlPoints[index + 1] = point + new Vector2(Mathf.Cos(angle + Mathf.PI/2), Mathf.Sin(angle + Mathf.PI/2)) * handleLength;
                    controlPoints[index + 2] = nextPoint + new Vector2(Mathf.Cos(nextAngle - Mathf.PI/2), Mathf.Sin(nextAngle - Mathf.PI/2)) * handleLength;
                }
            }
        
            return new BezierCurve(controlPoints);
        }

        public static IPath CreateAttackDive(Vector2 start, Vector2 end, float height)
        {
            Vector2[] controlPoints = new Vector2[]
            {
                start,
                start + Vector2.down * height * 0.25f,
                end + Vector2.up * height,
                end + Vector2.up * height * 0.5f,
                end
            };
        
            return new BezierCurve(controlPoints);
        }

        public static IPath CreateFormationEntry(Vector2 start, Vector2 end, float swoopHeight)
        {
            Vector2[] controlPoints = new Vector2[]
            {
                start,
                start + Vector2.right * swoopHeight * 0.5f,
                start + Vector2.right * swoopHeight + Vector2.up * swoopHeight * 0.5f,
                end + Vector2.up * swoopHeight,
                end
            };
        
            return new BezierCurve(controlPoints);
        }
    }
}