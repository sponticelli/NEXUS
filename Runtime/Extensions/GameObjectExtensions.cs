using UnityEngine;
using System;
using System.Linq;

namespace Nexus.Extensions
{
    public static class GameObjectExtensions 
    {
        public static T GetOrAddComponent<T>(this GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();
            if (component == null)
            {
                component = obj.AddComponent<T>();
            }
            return component;
        }

        public static bool IsGrounded(this GameObject obj, float rayDistance = 0.1f, LayerMask? layerMask = null)
        {
            var mask = layerMask ?? Physics.DefaultRaycastLayers;
            return Physics.Raycast(obj.transform.position, Vector3.down, rayDistance, mask);
        }

        public static bool IsVisibleToCamera(this GameObject obj, Camera camera)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null) return false;
            
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
        }

        public static GameObject GetClosest(this GameObject obj, GameObject[] others)
        {
            if (others == null || others.Length == 0) return null;

            return others
                .Where(x => x != null && x != obj)
                .OrderBy(x => Vector3.Distance(obj.transform.position, x.transform.position))
                .FirstOrDefault();
        }

        public static void SetLayer(this GameObject obj, string layerName)
        {
            obj.layer = LayerMask.NameToLayer(layerName);
        }

        public static bool HasLayer(this GameObject obj, string layerName)
        {
            return obj.layer == LayerMask.NameToLayer(layerName);
        }

        public static void SetTag(this GameObject obj, string tag)
        {
            obj.tag = tag;
        }

        public static bool HasTag(this GameObject obj, string tag)
        {
            return obj.CompareTag(tag);
        }
    }
}