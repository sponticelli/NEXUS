using UnityEngine;
using UnityEngine.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexus.Core.ServiceLocation;
using Nexus.Core.Services;

namespace Nexus.Audio
{
    [ServiceImplementation]
    public class SoundService : MonoBehaviour, ISoundService, IConfigurable<SoundServiceConfig>
    {
        private SoundServiceConfig config;
        private readonly List<AudioSource> sourcePool = new List<AudioSource>();
        private readonly List<SoundHandle> activeHandles = new List<SoundHandle>();
        private float masterVolume;
        private float uiVolume;
        private float sfxVolume;
        private float environmentVolume;
        private bool isInitialized;
        private TaskCompletionSource<bool> initializationTcs;
        private Dictionary<string, SoundEntry> soundCache;

        public float FadeDuration => config.defaultFadeDuration;
        public float MasterVolume => masterVolume;
        public float UIVolume => uiVolume;
        public float SFXVolume => sfxVolume;
        public float EnvironmentVolume => environmentVolume;
        public bool IsInitialized => isInitialized;

        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnUIVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;
        public event Action<float> OnEnvironmentVolumeChanged;
        
        public SoundLibrary CurrentLibrary => config?.soundLibrary;

        public void Configure(SoundServiceConfig configuration)
        {
            config = configuration;
            LogDebug("[SoundService] Configured with settings");
        }

        public async Task InitializeAsync()
        {
            if (isInitialized) return;

            try
            {
                initializationTcs = new TaskCompletionSource<bool>();
                LogDebug("Initializing sound service...");

                // Initialize source pool
                for (int i = 0; i < config.initialSources; i++)
                {
                    CreatePooledSource();
                }

                // Initialize sound cache
                InitializeSoundCache();

                // Set initial volumes
                masterVolume = config.defaultMasterVolume;
                uiVolume = config.defaultUIVolume;
                sfxVolume = config.defaultSFXVolume;
                environmentVolume = config.defaultEnvironmentVolume;

                await Task.Yield();

                // Apply volumes to mixer
                UpdateMixerVolumes();

                isInitialized = true;
                initializationTcs.SetResult(true);
                LogDebug("Sound service initialized successfully");
            }
            catch (Exception ex)
            {
                var error = $"Failed to initialize SoundService: {ex}";
                Debug.LogError(error);
                initializationTcs?.SetException(new Exception(error));
                throw;
            }
        }

        private void InitializeSoundCache()
        {
            soundCache = new Dictionary<string, SoundEntry>();

            if (config.soundLibrary == null)
            {
                Debug.LogWarning("No sound library assigned to SoundService");
                return;
            }

            var duplicateIds = config.soundLibrary.Sounds
                .GroupBy(s => s.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateIds.Any())
            {
                Debug.LogWarning($"Found duplicate sound IDs in library {config.soundLibrary.name}: {string.Join(", ", duplicateIds)}");
            }

            foreach (var sound in config.soundLibrary.Sounds)
            {
                if (string.IsNullOrEmpty(sound.Id))
                {
                    Debug.LogError($"Sound entry '{sound.DisplayName}' has no ID");
                    continue;
                }

                if (sound.Clip == null)
                {
                    Debug.LogError($"Sound entry '{sound.Id}' has no clip assigned");
                    continue;
                }

                if (soundCache.ContainsKey(sound.Id))
                {
                    Debug.LogWarning($"Duplicate sound ID '{sound.Id}' - using last definition");
                }

                soundCache[sound.Id] = sound;
            }

            LogDebug($"Initialized sound cache with {soundCache.Count} sounds");
        }


        public Task WaitForInitialization()
        {
            return initializationTcs?.Task ?? Task.CompletedTask;
        }
        

        public bool SetLibrary(SoundLibrary library, bool mergeDuplicates = false)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("Cannot change library before service is initialized");
                return false;
            }

            if (library == null)
            {
                Debug.LogError("Cannot set null sound library");
                return false;
            }

            // Store current state
            var previousLibrary = config.soundLibrary;
            var previousCache = new Dictionary<string, SoundEntry>(soundCache);

            try
            {
                // Clear existing cache
                soundCache.Clear();

                // Set new library
                config.soundLibrary = library;

                // Initialize with new library
                InitializeSoundCache();

                if (mergeDuplicates && previousLibrary != null)
                {
                    // Add back any sounds from previous library that don't conflict
                    foreach (var kvp in previousCache)
                    {
                        if (!soundCache.ContainsKey(kvp.Key))
                        {
                            soundCache[kvp.Key] = kvp.Value;
                        }
                    }
                }

                LogDebug($"Successfully changed sound library to: {library.name}");
                return true;
            }
            catch (Exception ex)
            {
                // Restore previous state on error
                Debug.LogError($"Failed to change sound library: {ex.Message}");
                config.soundLibrary = previousLibrary;
                soundCache = previousCache;
                return false;
            }
        }

        public int MergeLibrary(SoundLibrary library, bool overwriteExisting = false)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("Cannot merge library before service is initialized");
                return 0;
            }

            if (library == null)
            {
                Debug.LogWarning("Cannot merge null library");
                return 0;
            }

            int addedCount = 0;

            foreach (var sound in library.Sounds)
            {
                if (string.IsNullOrEmpty(sound.Id))
                {
                    Debug.LogWarning($"Skipping sound '{sound.DisplayName}' with no ID");
                    continue;
                }

                if (sound.Clip == null)
                {
                    Debug.LogWarning($"Skipping sound '{sound.Id}' with no clip");
                    continue;
                }

                bool exists = soundCache.ContainsKey(sound.Id);
                if (!exists || overwriteExisting)
                {
                    soundCache[sound.Id] = sound;
                    addedCount++;
                }
            }

            LogDebug($"Merged {addedCount} sounds from library: {library.name}");
            return addedCount;
        }


        public void PlayOneShot(string soundId, Vector3? position = null)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("SoundService not initialized");
                return;
            }

            if (!soundCache.TryGetValue(soundId, out var soundEntry))
            {
                Debug.LogError($"Sound with ID '{soundId}' not found");
                return;
            }

            PlayOneShot(soundEntry.Clip, soundEntry.Type, position, soundEntry.DefaultVolume);
            LogDebug($"Playing one shot sound: {soundEntry.DisplayName} [{soundId}]");
        }

        public ISoundHandle PlayLoop(string soundId, Vector3? position = null)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("SoundService not initialized");
                return null;
            }

            if (!soundCache.TryGetValue(soundId, out var soundEntry))
            {
                Debug.LogError($"Sound with ID '{soundId}' not found");
                return null;
            }

            var source = GetFreeSource();
            if (source == null) return null;

            // Configure the source based on sound entry settings
            ConfigureSourceFromEntry(source, soundEntry, position);

            source.clip = soundEntry.Clip;
            source.loop = true;
            source.volume = soundEntry.DefaultVolume;
            source.Play();

            var handle = new SoundHandle(source, soundEntry.Type, this);
            activeHandles.Add(handle);

            LogDebug($"Started looping sound: {soundEntry.DisplayName} [{soundId}]");
            return handle;
        }
        
        public void PlayOneShot(string soundId, float pitch, Vector3? position = null)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("SoundService not initialized");
                return;
            }

            if (!soundCache.TryGetValue(soundId, out var soundEntry))
            {
                Debug.LogError($"Sound with ID '{soundId}' not found");
                return;
            }

            var source = GetFreeSource();
            if (source == null) return;

            ConfigureSource(source, soundEntry.Type, position);
            source.pitch = pitch;
            source.PlayOneShot(soundEntry.Clip, soundEntry.DefaultVolume);
            LogDebug($"Playing one shot sound: {soundEntry.DisplayName} [{soundId}] with pitch {pitch}");
        }

        public ISoundHandle PlayLoop(string soundId, float pitch, Vector3? position = null)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("SoundService not initialized");
                return null;
            }

            if (!soundCache.TryGetValue(soundId, out var soundEntry))
            {
                Debug.LogError($"Sound with ID '{soundId}' not found");
                return null;
            }

            var source = GetFreeSource();
            if (source == null) return null;

            ConfigureSourceFromEntry(source, soundEntry, position);
            source.pitch = pitch; // Override any random pitch
            source.clip = soundEntry.Clip;
            source.loop = true;
            source.volume = soundEntry.DefaultVolume;
            source.Play();

            var handle = new SoundHandle(source, soundEntry.Type, this);
            activeHandles.Add(handle);

            LogDebug($"Started looping sound: {soundEntry.DisplayName} [{soundId}] with pitch {pitch}");
            return handle;
        }

        private void ConfigureSourceFromEntry(AudioSource source, SoundEntry entry, Vector3? position)
        {
            source.outputAudioMixerGroup = GetMixerGroup(entry.Type);

            // Apply pitch settings
            if (entry.RandomizePitch)
            {
                source.pitch = UnityEngine.Random.Range(entry.PitchMin, entry.PitchMax);
            }
            else
            {
                source.pitch = 1f;
            }

            if (entry.Spatialize || position.HasValue)
            {
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Custom;
                source.maxDistance = entry.MaxDistance;
                source.minDistance = entry.MinDistance;
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, config.rolloffCurve);

                if (position.HasValue)
                {
                    source.transform.position = position.Value;
                }
            }
            else
            {
                source.spatialBlend = 0f;
            }
        }

        public IEnumerable<SoundEntry> GetSoundsByTag(string tag)
        {
            if (string.IsNullOrEmpty(tag) || soundCache == null)
                return Enumerable.Empty<SoundEntry>();

            return soundCache.Values.Where(s => s.Tags.Contains(tag));
        }

        // Helper method to check if a sound ID exists
        public bool HasSound(string soundId)
        {
            return soundCache?.ContainsKey(soundId) ?? false;
        }

        // Helper method to get sound info
        public bool TryGetSoundInfo(string soundId, out SoundEntry soundEntry)
        {
            if (soundCache != null)
            {
                return soundCache.TryGetValue(soundId, out soundEntry);
            }

            soundEntry = null;
            return false;
        }

        public void PlayOneShot(AudioClip clip, SoundType type, Vector3? position = null, float volumeScale = 1f)
        {
            if (!isInitialized || clip == null) return;

            var source = GetFreeSource();
            if (source == null) return;

            ConfigureSource(source, type, position);
            source.PlayOneShot(clip, volumeScale);
            LogDebug($"Playing one shot sound: {clip.name} of type {type}");
        }

        public ISoundHandle PlayLoop(AudioClip clip, SoundType type, Vector3? position = null, float volumeScale = 1f)
        {
            if (!isInitialized || clip == null) return null;

            var source = GetFreeSource();
            if (source == null) return null;

            ConfigureSource(source, type, position);
            source.clip = clip;
            source.loop = true;
            source.volume = volumeScale;
            source.Play();

            var handle = new SoundHandle(source, type, this);
            activeHandles.Add(handle);
            LogDebug($"Started looping sound: {clip.name} of type {type}");
            return handle;
        }

        public void StopAll(SoundType type, bool fade = true)
        {
            foreach (var handle in activeHandles.ToArray())
            {
                if (handle.Type == type)
                {
                    handle.Stop(fade);
                }
            }

            LogDebug($"Stopped all sounds of type {type}");
        }

        public void PauseAll(SoundType type, bool fade = true)
        {
            foreach (var handle in activeHandles)
            {
                if (handle.Type == type)
                {
                    handle.Pause(fade);
                }
            }

            LogDebug($"Paused all sounds of type {type}");
        }

        public void ResumeAll(SoundType type, bool fade = true)
        {
            foreach (var handle in activeHandles)
            {
                if (handle.Type == type)
                {
                    handle.Resume(fade);
                }
            }

            LogDebug($"Resumed all sounds of type {type}");
        }

        public void SetMasterVolume(float volume, bool fade = true)
        {
            SetVolume(ref masterVolume, volume, config.masterVolumeParameter, OnMasterVolumeChanged, fade);
        }

        public void SetUIVolume(float volume, bool fade = true)
        {
            SetVolume(ref uiVolume, volume, config.uiVolumeParameter, OnUIVolumeChanged, fade);
        }

        public void SetSFXVolume(float volume, bool fade = true)
        {
            SetVolume(ref sfxVolume, volume, config.sfxVolumeParameter, OnSFXVolumeChanged, fade);
        }

        public void SetEnvironmentVolume(float volume, bool fade = true)
        {
            SetVolume(ref environmentVolume, volume, config.environmentVolumeParameter, OnEnvironmentVolumeChanged,
                fade);
        }

        public void RemoveHandle(SoundHandle handle)
        {
            activeHandles.Remove(handle);
        }

        private void SetVolume(ref float currentVolume, float newVolume, string parameter, Action<float> callback,
            bool fade)
        {
            newVolume = Mathf.Clamp01(newVolume);
            if (Mathf.Approximately(currentVolume, newVolume)) return;

            currentVolume = newVolume;
            UpdateMixerVolume(parameter, newVolume, fade);
            callback?.Invoke(newVolume);
            LogDebug($"Set {parameter} to {newVolume}");
        }

        private void UpdateMixerVolumes()
        {
            UpdateMixerVolume(config.masterVolumeParameter, masterVolume, false);
            UpdateMixerVolume(config.uiVolumeParameter, uiVolume, false);
            UpdateMixerVolume(config.sfxVolumeParameter, sfxVolume, false);
            UpdateMixerVolume(config.environmentVolumeParameter, environmentVolume, false);
        }

        private void UpdateMixerVolume(string parameter, float volume, bool fade)
        {
            float dbValue = volume > 0 ? 20f * Mathf.Log10(volume) : -80f;
            if (fade)
            {
                StartCoroutine(FadeMixerParameter(parameter, dbValue));
            }
            else
            {
                config.masterGroup.audioMixer.SetFloat(parameter, dbValue);
            }
        }

        private IEnumerator FadeMixerParameter(string parameter, float targetValue)
        {
            float currentValue;
            config.masterGroup.audioMixer.GetFloat(parameter, out currentValue);
            float elapsed = 0f;

            while (elapsed < config.defaultFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / config.defaultFadeDuration;
                float newValue = Mathf.Lerp(currentValue, targetValue, t);
                config.masterGroup.audioMixer.SetFloat(parameter, newValue);
                yield return null;
            }

            config.masterGroup.audioMixer.SetFloat(parameter, targetValue);
        }

        private AudioSource GetFreeSource()
        {
            var source = sourcePool.Find(s => !s.isPlaying);
            if (source) return source;
            
            if (sourcePool.Count < config.maxSources)
            {
                return CreatePooledSource();
            }
            
            return config.autoExpand ? CreatePooledSource() : null;
        }

        private AudioSource CreatePooledSource()
        {
            var go = new GameObject("PooledAudioSource");
            go.transform.SetParent(transform);
            var source = go.AddComponent<AudioSource>();
            ConfigurePooledSource(source);
            sourcePool.Add(source);
            return source;
        }

        private void ConfigurePooledSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.priority = 128;
            source.spatialBlend = 0f; // Start as TwoD by default
        }

        private void ConfigureSource(AudioSource source, SoundType type, Vector3? position)
        {
            source.outputAudioMixerGroup = GetMixerGroup(type);
            source.spatialBlend = position.HasValue ? 1f : 0f;

            if (position.HasValue)
            {
                source.transform.position = position.Value;
                source.rolloffMode = AudioRolloffMode.Custom;
                source.maxDistance = config.maxDistance;
                source.minDistance = config.minDistance;
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, config.rolloffCurve);
            }
        }

        private AudioMixerGroup GetMixerGroup(SoundType type)
        {
            return type switch
            {
                SoundType.UI => config.uiGroup,
                SoundType.SFX => config.sfxGroup,
                SoundType.Environment => config.environmentGroup,
                _ => config.masterGroup
            };
        }

        private void LogDebug(string message)
        {
            if (config.logDebugMessages)
            {
                Debug.Log($"[SoundService] {message}");
            }
        }

        private void OnDestroy()
        {
            foreach (var handle in activeHandles.ToArray())
            {
                handle.Stop(false);
            }

            activeHandles.Clear();
            sourcePool.Clear();
        }
    }
}