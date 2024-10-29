using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nexus.Extensions
{
    public static class CollisionExtensions
    {
        public static bool HasTag(this Collision collision, string tag)
        {
            return collision.gameObject.CompareTag(tag);
        }

        public static bool HasLayer(this Collision collision, string layerName)
        {
            return collision.gameObject.layer == LayerMask.NameToLayer(layerName);
        }

        public static List<GameObject> GetAllColliders(this Collision collision)
        {
            return collision.contacts.Select(contact => contact.otherCollider.gameObject).ToList();
        }

        public static List<GameObject> GetCollidersWithTag(this Collision collision, string tag)
        {
            return collision.contacts
                .Select(contact => contact.otherCollider.gameObject)
                .Where(obj => obj.CompareTag(tag))
                .ToList();
        }

        public static List<GameObject> GetCollidersWithLayer(this Collision collision, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return collision.contacts
                .Select(contact => contact.otherCollider.gameObject)
                .Where(obj => obj.layer == layer)
                .ToList();
        }

        public static Vector3[] GetContactPoints(this Collision collision)
        {
            return collision.contacts.Select(contact => contact.point).ToArray();
        }
    }
}