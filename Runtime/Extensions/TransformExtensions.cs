using UnityEngine;

namespace Nexus.Extensions
{
    public static class TransformExtensions
    {
        public static void ResetTransform(this Transform transform)
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        public static void AddChild(this Transform transform, Transform child)
        {
            child.SetParent(transform);
        }

        public static void ClearChildren(this Transform transform)
        {
            foreach (Transform child in transform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }
    }
}