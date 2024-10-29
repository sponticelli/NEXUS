using UnityEngine;

namespace Nexus.Extensions
{
    public static class Vector2Extensions
    {
        public static Vector2 WithX(this Vector2 vector, float x)
        {
            return new Vector2(x, vector.y);
        }

        public static Vector2 WithY(this Vector2 vector, float y)
        {
            return new Vector2(vector.x, y);
        }

        public static Vector3 ToVector3(this Vector2 vector)
        {
            return new Vector3(vector.x, vector.y, 0);
        }

        public static Vector2 Random(this Vector2 vector, float min, float max)
        {
            return new Vector2(
                UnityEngine.Random.Range(min, max),
                UnityEngine.Random.Range(min, max)
            );
        }
    }
}