using UnityEngine;

namespace Nexus.Extensions
{
    public static class ColorExtensions
    {
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        public static Color Lighten(this Color color, float amount)
        {
            return Color.Lerp(color, Color.white, amount);
        }

        public static Color Darken(this Color color, float amount)
        {
            return Color.Lerp(color, Color.black, amount);
        }

        public static Color Invert(this Color color)
        {
            return new Color(1f - color.r, 1f - color.g, 1f - color.b, color.a);
        }

        public static string ToHex(this Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
        }
    }
}