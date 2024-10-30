using System;
using System.Collections;
using UnityEngine;

namespace Nexus.Audio
{
    public class SoundHandle : ISoundHandle
    {
        private readonly AudioSource source;
        private readonly SoundService service;
        private Coroutine fadeCoroutine;
        
        public SoundType Type { get; }
        public bool IsPlaying => source != null && source.isPlaying;
        public bool IsPaused { get; private set; }
        public bool IsFading => fadeCoroutine != null;
        public float Volume => source?.volume ?? 0f;

        public SoundHandle(AudioSource source, SoundType type, SoundService service)
        {
            this.source = source;
            Type = type;
            this.service = service;
        }

        public void Stop(bool fade = true)
        {
            if (!IsPlaying) return;
            
            if (fade)
            {
                StartFade(0f, () => StopImmediate());
            }
            else
            {
                StopImmediate();
            }
        }

        private void StopImmediate()
        {
            source.Stop();
            source.clip = null;
            service.RemoveHandle(this);
        }

        public void Pause(bool fade = true)
        {
            if (!IsPlaying || IsPaused) return;
            
            if (fade)
            {
                StartFade(0f, () => PauseImmediate());
            }
            else
            {
                PauseImmediate();
            }
        }

        private void PauseImmediate()
        {
            source.Pause();
            IsPaused = true;
        }

        public void Resume(bool fade = true)
        {
            if (!IsPaused) return;

            source.UnPause();
            IsPaused = false;

            if (fade)
            {
                StartFade(1f);
            }
            else
            {
                source.volume = 1f;
            }
        }

        public void SetVolume(float volume, bool fade = true)
        {
            volume = Mathf.Clamp01(volume);
            if (fade)
            {
                StartFade(volume);
            }
            else
            {
                source.volume = volume;
            }
        }

        public void SetPosition(Vector3 position)
        {
            if (source != null)
            {
                source.transform.position = position;
            }
        }

        private void StartFade(float targetVolume, Action onComplete = null)
        {
            if (fadeCoroutine != null)
            {
                service.StopCoroutine(fadeCoroutine);
            }

            fadeCoroutine = service.StartCoroutine(FadeCoroutine(targetVolume, onComplete));
        }

        private IEnumerator FadeCoroutine(float targetVolume, Action onComplete = null)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < service.FadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / service.FadeDuration;
                source.volume = Mathf.Lerp(startVolume, targetVolume, t);
                yield return null;
            }

            source.volume = targetVolume;
            fadeCoroutine = null;
            onComplete?.Invoke();
        }

        public void Dispose()
        {
            Stop(false);
        }
    }
}