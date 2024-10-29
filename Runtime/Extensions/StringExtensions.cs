using System;

namespace Nexus.Extensions
{
    public static class StringExtensions
    {
        public static string ToCamelCase(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            
            string[] words = str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }
            return string.Join("", words);
        }

        public static string ToKMB(this int num)
        {
            return num switch
            {
                >= 1000000000 => (num / 1000000000f).ToString("0.#") + "B",
                >= 1000000 => (num / 1000000f).ToString("0.#") + "M",
                >= 1000 => (num / 1000f).ToString("0.#") + "K",
                _ => num.ToString()
            };
        }
    }
}