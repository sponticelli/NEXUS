using UnityEngine;

namespace Nexus.Extensions
{
    public static class MathExtensions
    {
        public static float LerpAngle(float a, float b, float t)
        {
            float delta = Mathf.Repeat((b - a), 360);
            if (delta > 180)
                delta -= 360;
            return a + delta * Mathf.Clamp01(t);
        }

        public static float SmoothStep(float start, float end, float t)
        {
            t = Mathf.Clamp01(t);
            t = t * t * (3f - 2f * t);
            return Mathf.Lerp(start, end, t);
        }

        public static float Sinerp(float start, float end, float t)
        {
            return Mathf.Lerp(start, end, Mathf.Sin(t * Mathf.PI * 0.5f));
        }

        public static float RoundToNearest(float value, float increment)
        {
            return Mathf.Round(value / increment) * increment;
        }

        public static float ShortestDifference(float current, float target)
        {
            float diff = (target - current) % 360f;
            return diff > 180f ? diff - 360f : diff;
        }

        public static float Distance(Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b);
        }
    }
}