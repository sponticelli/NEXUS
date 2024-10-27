using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Services;
using UnityEngine;

namespace Nexus.Core.Audio
{
    public class ConfigurableAudioService : MonoBehaviourServiceBase, IAudioService, 
        IConfigurableService<AudioServiceConfiguration>, ISingletonService
    {
        [SerializeField] private AudioServiceConfiguration configuration;

        private readonly Queue<AudioSource> availableSfxSources = new Queue<AudioSource>();
        private readonly Dictionary<string, AudioCollection> collectionLookup = new Dictionary<string, AudioCollection>();
        private AudioSource musicSource;
        private AudioClipReference currentMusicClip;

        public AudioServiceConfiguration Configuration
        {
            get => configuration;
            set => configuration = value;
        }

        protected override async Task OnInitialize(CancellationToken cancellationToken)
        {
            // Validate configuration
            if (!configuration.Validate(out string error))
            {
                throw new InvalidOperationException($"Invalid audio service configuration: {error}");
            }

            // Initialize audio collections lookup
            InitializeCollectionLookup();

            // Initialize audio sources
            InitializeAudioSources();

            // Set default volumes
            SetMasterVolume(configuration.DefaultMasterVolume);
            SetMusicVolume(configuration.DefaultMusicVolume);
            SetSfxVolume(configuration.DefaultSfxVolume);

            await base.OnInitialize(cancellationToken);
        }

        private void InitializeCollectionLookup()
        {
            // Add main collections
            if (configuration.MusicCollection != null)
                collectionLookup["music"] = configuration.MusicCollection;
            
            if (configuration.SfxCollection != null)
                collectionLookup["sfx"] = configuration.SfxCollection;

            // Add additional collections
            if (configuration.AdditionalCollections != null)
            {
                foreach (var collection in configuration.AdditionalCollections)
                {
                    if (collection != null)
                    {
                        collectionLookup[collection.name.ToLower()] = collection;
                    }
                }
            }
        }

        private void InitializeAudioSources()
        {
            // Initialize music source
            if (configuration.MusicSourcePrefab != null)
            {
                musicSource = Instantiate(configuration.MusicSourcePrefab, transform);
            }
            else
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }

            musicSource.outputAudioMixerGroup = configuration.MusicGroup;

            // Initialize SFX source pool
            for (int i = 0; i < configuration.InitialSfxSourcePoolSize; i++)
            {
                CreateSfxSource();
            }
        }

        private void CreateSfxSource()
        {
            AudioSource sfxSource;
            if (configuration.SfxSourcePrefab != null)
            {
                sfxSource = Instantiate(configuration.SfxSourcePrefab, transform);
            }
            else
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            sfxSource.outputAudioMixerGroup = configuration.SfxGroup;
            availableSfxSources.Enqueue(sfxSource);
        }

        public async Task PlaySound(string soundId, float volume = 1.0f)
        {
            var clipReference = GetAudioClipReference(soundId);
            if (clipReference?.clip == null) 
            {
                Debug.LogWarning($"Audio clip not found: {soundId}");
                return;
            }

            var source = GetOrCreateSfxSource();
            source.clip = clipReference.clip;
            source.volume = volume * clipReference.defaultVolume * configuration.DefaultSfxVolume;
            
            if (clipReference.mixerGroup != null)
                source.outputAudioMixerGroup = clipReference.mixerGroup;
                
            source.Play();

            await Task.Delay(TimeSpan.FromSeconds(0.1f)); // Small delay to ensure playback starts
            StartCoroutine(ReturnSourceToPool(source, clipReference.clip.length + 0.1f));
        }

        public async Task PlayMusic(string musicId, float fadeInDuration = -1)
        {
            var clipReference = GetAudioClipReference($"music/{musicId}");
            if (clipReference?.clip == null)
            {
                Debug.LogWarning($"Music clip not found: {musicId}");
                return;
            }

            if (fadeInDuration < 0) fadeInDuration = configuration.DefaultMusicFadeTime;

            await StopMusic();

            currentMusicClip = clipReference;
            musicSource.clip = clipReference.clip;
            musicSource.volume = 0f;
            
            if (clipReference.mixerGroup != null)
                musicSource.outputAudioMixerGroup = clipReference.mixerGroup;
                
            musicSource.Play();

            // Fade in
            float elapsed = 0f;
            float targetVolume = clipReference.defaultVolume * configuration.DefaultMusicVolume;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeInDuration);
                await Task.Yield();
            }
        }

        public async Task StopMusic(float fadeOutDuration = -1)
        {
            if (!musicSource.isPlaying) return;

            if (fadeOutDuration < 0) fadeOutDuration = configuration.DefaultMusicFadeTime;

            float startVolume = musicSource.volume;
            float elapsed = 0f;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutDuration);
                await Task.Yield();
            }

            musicSource.Stop();
            musicSource.clip = null;
            currentMusicClip = null;
        }

        private AudioClipReference GetAudioClipReference(string soundId)
        {
            // Check if the sound ID includes a collection prefix (e.g., "sfx/jump" or "music/theme")
            string collectionKey = "sfx"; // Default to SFX collection
            string clipId = soundId;

            int separatorIndex = soundId.IndexOf('/');
            if (separatorIndex >= 0)
            {
                collectionKey = soundId.Substring(0, separatorIndex).ToLower();
                clipId = soundId.Substring(separatorIndex + 1);
            }

            if (collectionLookup.TryGetValue(collectionKey, out var collection))
            {
                collection.TryGetClipReference(clipId, out var clipReference);
                return clipReference;
            }

            return null;
        }

        private AudioSource GetOrCreateSfxSource()
        {
            if (availableSfxSources.Count > 0)
                return availableSfxSources.Dequeue();

            if (transform.childCount < configuration.MaxSfxSourcePoolSize)
            {
                CreateSfxSource();
                return availableSfxSources.Dequeue();
            }

            // If we've reached the max pool size, reuse the oldest source
            var oldestSource = transform.GetChild(0).GetComponent<AudioSource>();
            oldestSource.Stop();
            return oldestSource;
        }

        private System.Collections.IEnumerator ReturnSourceToPool(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (source != null)
            {
                source.clip = null;
                availableSfxSources.Enqueue(source);
            }
        }

        public void SetMasterVolume(float volume)
        {
            if (configuration.AudioMixer != null)
            {
                configuration.AudioMixer.SetFloat("MasterVolume", 
                    Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20f);
            }
        }

        public void SetMusicVolume(float volume)
        {
            if (configuration.AudioMixer != null)
            {
                configuration.AudioMixer.SetFloat("MusicVolume", 
                    Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20f);
            }

            // Update current music volume if playing
            if (currentMusicClip != null && musicSource.isPlaying)
            {
                musicSource.volume = volume * currentMusicClip.defaultVolume;
            }
        }

        public void SetSfxVolume(float volume)
        {
            if (configuration.AudioMixer != null)
            {
                configuration.AudioMixer.SetFloat("SFXVolume", 
                    Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20f);
            }
        }

        protected override async Task OnShutdown()
        {
            await StopMusic(0.5f);
            await base.OnShutdown();
        }
    }
}