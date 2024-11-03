using System.Collections;
using UnityEngine;

namespace Nexus.Extensions
{
    public static class AudioSourceExtensions
    {
        public static void PlayClip(this AudioSource source, AudioClip clip)
        {
            source.clip = clip;
            source.Play();
        }

        public static void SetVolume(this AudioSource source, float volume)
        {
            source.volume = Mathf.Clamp01(volume);
        }

        public static float GetVolume(this AudioSource source)
        {
            return source.volume;
        }

        public static void Set3DProperties(this AudioSource source, float minDistance, float maxDistance, float spatialBlend)
        {
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.spatialBlend = Mathf.Clamp01(spatialBlend);
        }

        public static IEnumerator FadeOut(this AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float timer = 0;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0, timer / duration);
                yield return null;
            }

            source.Stop();
            source.volume = startVolume;
        }

        public static IEnumerator FadeIn(this AudioSource source, float duration)
        {
            source.volume = 0;
            source.Play();

            float targetVolume = 1f;
            float timer = 0;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                source.volume = Mathf.Lerp(0, targetVolume, timer / duration);
                yield return null;
            }
        }
    }
    
}