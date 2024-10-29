using UnityEngine;

namespace Nexus.Extensions
{
    public static class Rigidbody2DExtensions
    {
        public static void AddForceTowards(this Rigidbody2D rb, Vector2 target, float force)
        {
            Vector2 direction = (target - rb.position).normalized;
            rb.AddForce(direction * force);
        }

        public static void SetVelocityTowards(this Rigidbody2D rb, Vector2 target, float speed)
        {
            Vector2 direction = (target - rb.position).normalized;
            rb.velocity = direction * speed;
        }

        public static void StopMovement(this Rigidbody2D rb)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}