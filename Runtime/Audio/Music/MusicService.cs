using System;
using System.Collections;
using System.Threading.Tasks;
using Nexus.Core.ServiceLocation;
using Nexus.Core.Services;
using UnityEngine;

namespace Nexus.Audio
{
    [ServiceImplementation]
    public class MusicService : MonoBehaviour, IMusicService, IConfigurable<MusicServiceConfig>
    {
        private MusicServiceConfig config;
        private AudioSource[] audioSources;
        private MusicPlaylist currentPlaylist;
        private int currentTrackIndex = -1;
        private int activeSourceIndex = 0;
        private Coroutine fadeCoroutine;
        private float volume;
        private bool isPlaying;
        private bool isPaused;
        private TaskCompletionSource<bool> initializationTcs;

        public float Volume => volume;
        public bool IsPlaying => isPlaying;
        public bool IsPaused => isPaused;
        public bool IsInitialized { get; private set; }
        
        public string CurrentTrackName => 
            currentPlaylist?.Tracks != null && currentTrackIndex >= 0 && currentTrackIndex < currentPlaylist.Tracks.Count 
                ? currentPlaylist.Tracks[currentTrackIndex].displayName 
                : string.Empty;

        public event Action<string> OnTrackChanged;
        public event Action<float> OnVolumeChanged;
        public event Action<bool> OnPlaybackStateChanged;

        public void Configure(MusicServiceConfig configuration)
        {
            config = configuration;
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            initializationTcs = new TaskCompletionSource<bool>();

            try
            {
                // Create and configure audio sources
                audioSources = new AudioSource[config.audioSourceCount];
                for (int i = 0; i < config.audioSourceCount; i++)
                {
                    var audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.loop = true;
                    audioSources[i] = audioSource;
                }

                // Set initial volume
                SetVolume(config.defaultVolume);

                // Load default playlist if configured
                if (config.defaultPlaylist != null)
                {
                    SetPlaylist(config.defaultPlaylist);
                }

                IsInitialized = true;
                initializationTcs.SetResult(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize MusicService: {ex}");
                initializationTcs.SetException(ex);
                throw;
            }
        }

        public Task WaitForInitialization()
        {
            return initializationTcs?.Task ?? Task.CompletedTask;
        }

        public void SetPlaylist(MusicPlaylist playlist)
        {
            if (!IsInitialized) return;
            
            currentPlaylist = playlist;
            currentTrackIndex = -1;
        }

        public void PlayTrack(string trackName)
        {
            if (!IsInitialized || currentPlaylist?.Tracks == null) return;

            // Find the first track that matches the display name
            for (int i = 0; i < currentPlaylist.Tracks.Count; i++)
            {
                if (currentPlaylist.Tracks[i].displayName == trackName)
                {
                    PlayTrack(i);
                    return;
                }
            }

            Debug.LogWarning($"Track '{trackName}' not found in the current playlist.");
        }

        public void PlayTrack(int trackIndex)
        {
            if (!IsInitialized || currentPlaylist?.Tracks == null) return;

            if (trackIndex >= 0 && trackIndex < currentPlaylist.Tracks.Count)
            {
                currentTrackIndex = trackIndex;
                PlayTrack(currentPlaylist.Tracks[trackIndex].clip);
                OnTrackChanged?.Invoke(CurrentTrackName);
            }
            else
            {
                Debug.LogWarning($"Track index {trackIndex} is out of range.");
            }
        }

        public void PlayTrack(AudioClip clip)
        {
            if (!IsInitialized || audioSources == null) return;

            int nextSourceIndex = (activeSourceIndex + 1) % audioSources.Length;
            
            audioSources[nextSourceIndex].clip = clip;
            audioSources[nextSourceIndex].volume = 0;
            audioSources[nextSourceIndex].Play();

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            fadeCoroutine = StartCoroutine(CrossfadeAudioSources(activeSourceIndex, nextSourceIndex));
            activeSourceIndex = nextSourceIndex;
            
            isPlaying = true;
            isPaused = false;
            OnPlaybackStateChanged?.Invoke(true);
        }

        public void PlayNext()
        {
            if (!IsInitialized || currentPlaylist?.Tracks == null || currentPlaylist.Tracks.Count == 0) return;

            currentTrackIndex = (currentTrackIndex + 1) % currentPlaylist.Tracks.Count;
            PlayTrack(currentPlaylist.Tracks[currentTrackIndex].clip);
            OnTrackChanged?.Invoke(CurrentTrackName);
        }

        public void PlayPrevious()
        {
            if (!IsInitialized || currentPlaylist?.Tracks == null || currentPlaylist.Tracks.Count == 0) return;

            currentTrackIndex = (currentTrackIndex - 1 + currentPlaylist.Tracks.Count) % currentPlaylist.Tracks.Count;
            PlayTrack(currentPlaylist.Tracks[currentTrackIndex].clip);
            OnTrackChanged?.Invoke(CurrentTrackName);
        }

        public void Stop()
        {
            if (!IsInitialized) return;

            foreach (var source in audioSources)
            {
                source.Stop();
            }
            
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            isPlaying = false;
            isPaused = false;
            OnPlaybackStateChanged?.Invoke(false);
        }

        public void Pause()
        {
            if (!IsInitialized || !isPlaying || isPaused) return;

            foreach (var source in audioSources)
            {
                source.Pause();
            }

            isPaused = true;
            OnPlaybackStateChanged?.Invoke(false);
        }

        public void Resume()
        {
            if (!IsInitialized || !isPaused) return;

            foreach (var source in audioSources)
            {
                source.UnPause();
            }

            isPaused = false;
            OnPlaybackStateChanged?.Invoke(true);
        }

        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
            
            if (audioSources != null)
            {
                foreach (var source in audioSources)
                {
                    if (source != null)
                    {
                        source.volume = volume;
                    }
                }
            }
            
            OnVolumeChanged?.Invoke(volume);
        }

        private IEnumerator CrossfadeAudioSources(int fromIndex, int toIndex)
        {
            float elapsedTime = 0;
            float startVolume = audioSources[fromIndex].volume;
            
            while (elapsedTime < config.crossfadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / config.crossfadeDuration;

                audioSources[fromIndex].volume = Mathf.Lerp(startVolume, 0, t);
                audioSources[toIndex].volume = Mathf.Lerp(0, volume, t);

                yield return null;
            }

            audioSources[fromIndex].Stop();
            audioSources[toIndex].volume = volume;
            fadeCoroutine = null;
        }

        private void OnDisable()
        {
            Stop();
        }
    }
}