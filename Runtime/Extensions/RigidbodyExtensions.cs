using UnityEngine;

namespace Nexus.Extensions
{
    public static class RigidbodyExtensions
    {
        public static void AddForceTowards(this Rigidbody rb, Vector3 target, float force)
        {
            Vector3 direction = (target - rb.position).normalized;
            rb.AddForce(direction * force);
        }

        public static void SetVelocityTowards(this Rigidbody rb, Vector3 target, float speed)
        {
            Vector3 direction = (target - rb.position).normalized;
            rb.velocity = direction * speed;
        }

        public static void StopMovement(this Rigidbody rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}