using UnityEngine;

namespace Nexus.Extensions
{
    public static class RectTransformExtensions
    {
        public static void SetLeft(this RectTransform rt, float left)
        {
            rt.offsetMin = new Vector2(left, rt.offsetMin.y);
        }

        public static void SetRight(this RectTransform rt, float right)
        {
            rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
        }

        public static void SetTop(this RectTransform rt, float top)
        {
            rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
        }

        public static void SetBottom(this RectTransform rt, float bottom)
        {
            rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
        }

        public static void SetSize(this RectTransform rt, Vector2 size)
        {
            Vector2 oldSize = rt.rect.size;
            Vector2 deltaSize = size - oldSize;
            rt.offsetMin -= new Vector2(deltaSize.x * rt.pivot.x, deltaSize.y * rt.pivot.y);
            rt.offsetMax += new Vector2(deltaSize.x * (1f - rt.pivot.x), deltaSize.y * (1f - rt.pivot.y));
        }
    }
}