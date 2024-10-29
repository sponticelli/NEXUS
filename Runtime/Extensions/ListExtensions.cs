using System.Collections.Generic;

namespace Nexus.Extensions
{
    public static class ListExtensions
    {
        public static void AddList<T>(this List<T> list, T item)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
            }
        }

        public static void RemoveList<T>(this List<T> list, T item)
        {
            if (list.Contains(item))
            {
                list.Remove(item);
            }
        }

        public static void ShuffleList<T>(this List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        public static void SwapList<T>(this List<T> list, int indexA, int indexB)
        {
            if (indexA >= 0 && indexA < list.Count && indexB >= 0 && indexB < list.Count)
            {
                (list[indexA], list[indexB]) = (list[indexB], list[indexA]);
            }
        }

        public static T GetRandom<T>(this List<T> list)
        {
            if (list == null || list.Count == 0) return default;
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        public static T PopRandom<T>(this List<T> list)
        {
            if (list == null || list.Count == 0) return default;
            int index = UnityEngine.Random.Range(0, list.Count);
            T item = list[index];
            list.RemoveAt(index);
            return item;
        }

        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            return list == null || list.Count == 0;
        }
    }
}