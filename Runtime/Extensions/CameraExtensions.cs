using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nexus.Extensions
{
    public static class CameraExtensions
    {
        public static IEnumerator Shake(this Camera camera, MonoBehaviour coroutineHost, float duration = 0.5f, float magnitude = 0.1f)
        {
            Vector3 originalPosition = camera.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;

                camera.transform.localPosition = originalPosition + new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }

            camera.transform.localPosition = originalPosition;
        }

        public static IEnumerator MoveTo(this Camera camera, Vector3 targetPosition, float speed)
        {
            while (Vector3.Distance(camera.transform.position, targetPosition) > 0.01f)
            {
                camera.transform.position = Vector3.MoveTowards(
                    camera.transform.position,
                    targetPosition,
                    speed * Time.deltaTime
                );
                yield return null;
            }
        }

        public static IEnumerator ZoomTo(this Camera camera, float targetSize, float duration)
        {
            float startSize = camera.orthographicSize;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                camera.orthographicSize = Mathf.Lerp(startSize, targetSize, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            camera.orthographicSize = targetSize;
        }
    }
}