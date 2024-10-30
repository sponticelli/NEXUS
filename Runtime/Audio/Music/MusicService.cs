using System;
using System.Collections;
using System.Collections.Generic;
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
        private Coroutine playbackMonitorCoroutine;
        private float volume;
        private bool isPlaying;
        private bool isPaused;
        private bool isFading;
        private TaskCompletionSource<bool> initializationTcs;
        private Stack<int> trackHistory;
        private string lastPlayedTrackId;

        // Properties
        public float Volume => volume;
        public bool IsPlaying => isPlaying;
        public bool IsPaused => isPaused;
        public bool IsFading => isFading;
        public bool IsInitialized { get; private set; }

        public string CurrentTrackName =>
            currentPlaylist?.Tracks != null && currentTrackIndex >= 0
                ? currentPlaylist.Tracks[currentTrackIndex].displayName
                : string.Empty;

        public float CurrentTrackTime =>
            audioSources != null && activeSourceIndex >= 0 ? audioSources[activeSourceIndex].time : 0f;

        public float CurrentTrackLength =>
            audioSources != null && activeSourceIndex >= 0 ? audioSources[activeSourceIndex].clip?.length ?? 0f : 0f;

        public float CurrentTrackProgress =>
            CurrentTrackLength > 0 ? CurrentTrackTime / CurrentTrackLength : 0f;

        // Events
        public event Action<MusicTrackInfo> OnTrackStarted;
        public event Action<MusicTrackInfo> OnTrackEnded;
        public event Action<float> OnVolumeChanged;
        public event Action<bool> OnPlaybackStateChanged;

        public void Configure(MusicServiceConfig configuration)
        {
            config = configuration;
            Debug.Log(
                $"[MusicService] Configure called with config: {configuration?.name}, defaultPlaylist: {configuration?.defaultPlaylist?.name}");
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            try
            {
                initializationTcs = new TaskCompletionSource<bool>();
                LogDebug("Initializing MusicService...");

                // Initialize collections
                trackHistory = new Stack<int>(config.maxHistorySize);

                // Create and configure audio sources
                audioSources = new AudioSource[config.audioSourceCount];
                for (int i = 0; i < config.audioSourceCount; i++)
                {
                    var go = new GameObject($"AudioSource{i}");
                    go.transform.SetParent(transform);
                    audioSources[i] = go.AddComponent<AudioSource>(); // Store the reference
                    ConfigureAudioSource(audioSources[i]);
                    Debug.Log($"Created AudioSource{i}: {audioSources[i] != null}");
                }

                // Set initial volume
                volume = config.defaultVolume;

                // Mark as initialized before setting playlist
                IsInitialized = true;

                // Now it's safe to set playlist
                if (config.defaultPlaylist != null)
                {
                    SetPlaylist(config.defaultPlaylist, config.defaultPlaylist.AutoPlayOnSet);

                    // Restore last played track if enabled
                    if (config.rememberLastTrack)
                    {
                        RestoreLastPlayedTrack();
                    }
                }

                // Start monitoring playback for track completion
                playbackMonitorCoroutine = StartCoroutine(MonitorPlayback());

                // Subscribe to application focus events
                Application.focusChanged += HandleApplicationFocus;

                Debug.Log($"MusicService initialized with {audioSources.Length} audio sources");
                foreach (var source in audioSources)
                {
                    Debug.Log($"AudioSource status: {(source != null ? "valid" : "null")}");
                }

                initializationTcs.SetResult(true);
            }
            catch (Exception ex)
            {
                var error = $"Failed to initialize MusicService: {ex}";
                Debug.LogError(error);
                initializationTcs.SetException(new Exception(error));
                throw;
            }
        }

        private void ConfigureAudioSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = true;
            source.outputAudioMixerGroup = config.mixerGroup;
            source.priority = 0; // Highest priority
        }

        public Task WaitForInitialization()
        {
            return initializationTcs?.Task ?? Task.CompletedTask;
        }

        public void SetPlaylist(MusicPlaylist playlist, bool autoPlay = false)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Attempting to set playlist before initialization");
                return;
            }

            Debug.Log($"Setting playlist: {playlist?.name}, autoPlay: {autoPlay}");
            currentPlaylist = playlist;
            currentTrackIndex = -1;
            trackHistory?.Clear(); // Add null check

            if (autoPlay && playlist?.Tracks.Count > 0)
            {
                PlayTrack(0);
            }
        }

        public void PlayTrack(string trackName, bool fadeIn = true)
        {
            if (!IsInitialized || currentPlaylist?.Tracks == null) return;

            var trackIndex = currentPlaylist.FindTrackIndex(trackName);
            if (trackIndex >= 0)
            {
                PlayTrack(trackIndex, fadeIn);
            }
            else
            {
                Debug.LogWarning($"Track '{trackName}' not found in the current playlist.");
            }
        }

        public void PlayTrack(int trackIndex, bool fadeIn = true)
        {
            try
            {
                Debug.Log($"Starting PlayTrack: {trackIndex}");

                if (!IsInitialized || currentPlaylist?.Tracks == null)
                {
                    Debug.LogWarning("MusicService not initialized or playlist is null.");
                    return;
                }

                if (trackIndex >= 0 && trackIndex < currentPlaylist.Tracks.Count)
                {
                    var track = currentPlaylist.Tracks[trackIndex];
                    Debug.Log($"Found track: {track.displayName}, clip: {(track.clip != null ? "valid" : "null")}");

                    if (currentTrackIndex >= 0)
                    {
                        Debug.Log("Saving to history");
                        while (trackHistory.Count >= config.maxHistorySize)
                        {
                            trackHistory.Pop();
                        }

                        trackHistory.Push(currentTrackIndex);
                    }

                    currentTrackIndex = trackIndex;
                    lastPlayedTrackId = track.id;

                    Debug.Log("Calling PlayTrackInternal");
                    PlayTrackInternal(track, fadeIn);

                    Debug.Log("Saving last played track");
                    if (config.rememberLastTrack)
                    {
                        PlayerPrefs.SetString("LastPlayedTrackId", lastPlayedTrackId);
                        PlayerPrefs.Save();
                    }

                    Debug.Log($"PlayTrack completed for: {track.displayName}");
                }
                else
                {
                    Debug.LogWarning($"Track index {trackIndex} is out of range.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in PlayTrack: {ex}");
            }
        }

        public void PlayTrack(AudioClip clip, bool fadeIn = true)
        {
            if (!IsInitialized || clip == null) return;

            var trackInfo = new MusicTrackInfo
            {
                DisplayName = clip.name,
                Clip = clip,
                VolumeMultiplier = 1f,
                StartTime = Time.time,
                Duration = clip.length
            };

            PlayTrackInternal(trackInfo, fadeIn);
        }

        private void PlayTrackInternal(MusicPlaylist.TrackInfo track, bool fadeIn)
        {
            Debug.Log($"PlayTrackInternal starting for {track.displayName}");

            var trackInfo = new MusicTrackInfo
            {
                Id = track.id,
                DisplayName = track.displayName,
                Clip = track.clip,
                VolumeMultiplier = track.volumeMultiplier,
                Description = track.description,
                Tags = track.tags?.AsReadOnly(),
                StartTime = Time.time,
                Duration = track.clip?.length ?? 0f
            };

            PlayTrackInternal(trackInfo, fadeIn);
        }

        private void PlayTrackInternal(MusicTrackInfo trackInfo, bool fadeIn)
        {
            if (!IsInitialized)
            {
                Debug.LogError("Attempting to play track before service is initialized");
                return;
            }

            Debug.Log(
                $"PlayTrackInternal executing for {trackInfo?.DisplayName}, audioSources: {(audioSources != null ? audioSources.Length.ToString() : "null")}");

            try
            {
                if (trackInfo?.Clip == null)
                {
                    Debug.LogError("Attempted to play null track or clip");
                    return;
                }

                if (audioSources == null)
                {
                    Debug.LogError("AudioSources array is null");
                    return;
                }

                int nextSourceIndex = activeSourceIndex < 0 ? 0 : (activeSourceIndex + 1) % audioSources.Length;
                var nextSource = audioSources[nextSourceIndex];

                if (nextSource == null)
                {
                    Debug.LogError($"AudioSource at index {nextSourceIndex} is null");
                    return;
                }

                Debug.Log($"Setting up next source at index {nextSourceIndex}");
                nextSource.clip = trackInfo.Clip;
                nextSource.volume = fadeIn ? 0f : volume * trackInfo.VolumeMultiplier;

                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                    fadeCoroutine = null;
                }

                Debug.Log("Starting playback");
                nextSource.Play();

                if (fadeIn)
                {
                    fadeCoroutine = StartCoroutine(CrossfadeAudioSources(
                        activeSourceIndex,
                        nextSourceIndex,
                        config.defaultFadeDuration,
                        trackInfo.VolumeMultiplier
                    ));
                }
                else if (activeSourceIndex >= 0 && activeSourceIndex < audioSources.Length)
                {
                    audioSources[activeSourceIndex]?.Stop();
                }

                activeSourceIndex = nextSourceIndex;
                isPlaying = true;
                isPaused = false;

                OnTrackStarted?.Invoke(trackInfo);
                OnPlaybackStateChanged?.Invoke(true);

                Debug.Log($"Successfully started playback of {trackInfo.DisplayName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in PlayTrackInternal: {ex}");
                throw;
            }
        }

        public void PlayNext(bool fadeTransition = true)
        {
            if (!IsInitialized || currentPlaylist?.Tracks == null || currentPlaylist.Tracks.Count == 0) return;

            int nextIndex = (currentTrackIndex + 1) % currentPlaylist.Tracks.Count;
            if (nextIndex == 0 && !currentPlaylist.LoopPlaylist)
            {
                Stop(fadeTransition);
                return;
            }

            PlayTrack(nextIndex, fadeTransition);
        }

        public void PlayPrevious(bool fadeTransition = true)
        {
            if (!IsInitialized || currentPlaylist?.Tracks == null) return;

            // If we have history, use it
            if (trackHistory.Count > 0)
            {
                int previousIndex = trackHistory.Pop();
                PlayTrack(previousIndex, fadeTransition);
                return;
            }

            // Otherwise, go to previous track in playlist
            int previousTrackIndex =
                (currentTrackIndex - 1 + currentPlaylist.Tracks.Count) % currentPlaylist.Tracks.Count;
            PlayTrack(previousTrackIndex, fadeTransition);
        }

        public void Stop(bool fadeOut = true)
        {
            if (!IsInitialized) return;

            if (fadeOut && isPlaying)
            {
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }

                fadeCoroutine = StartCoroutine(FadeOut(config.defaultFadeDuration));
            }
            else
            {
                StopImmediate();
            }
        }

        private void StopImmediate()
        {
            if (audioSources == null) return; // Add null check

            foreach (var source in audioSources)
            {
                if (source != null) // Add null check
                {
                    source.Stop();
                }
            }

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            isPlaying = false;
            isPaused = false;
            isFading = false;

            OnPlaybackStateChanged?.Invoke(false);
        }

        public void Pause(bool fadeOut = true)
        {
            if (!IsInitialized || !isPlaying || isPaused) return;

            if (fadeOut)
            {
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }

                fadeCoroutine = StartCoroutine(FadeOut(config.quickFadeDuration, true));
            }
            else
            {
                PauseImmediate();
            }
        }

        private void PauseImmediate()
        {
            foreach (var source in audioSources)
            {
                source.Pause();
            }

            isPaused = true;
            OnPlaybackStateChanged?.Invoke(false);
        }

        public void Resume(bool fadeIn = true)
        {
            if (!IsInitialized || !isPaused) return;

            foreach (var source in audioSources)
            {
                source.UnPause();
            }

            isPaused = false;

            if (fadeIn)
            {
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }

                var currentTrack = currentPlaylist?.Tracks[currentTrackIndex];
                float volumeMultiplier = currentTrack?.volumeMultiplier ?? 1f;

                fadeCoroutine = StartCoroutine(FadeIn(config.quickFadeDuration, volumeMultiplier));
            }
            else
            {
                OnPlaybackStateChanged?.Invoke(true);
            }
        }

        public void SetVolume(float newVolume, bool fade = true)
        {
            newVolume = Mathf.Clamp01(newVolume);

            if (fade && gameObject.activeInHierarchy)
            {
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }

                fadeCoroutine = StartCoroutine(FadeVolume(volume, newVolume, config.quickFadeDuration));
            }
            else
            {
                volume = newVolume;
                UpdateVolume();
            }

            // Update mixer if configured
            if (config.mixerGroup != null && !string.IsNullOrEmpty(config.volumeParameter))
            {
                float dbValue = newVolume > 0 ? 20f * Mathf.Log10(newVolume) : -80f;
                config.mixerGroup.audioMixer.SetFloat(config.volumeParameter, dbValue);
            }

            OnVolumeChanged?.Invoke(volume);
        }

        public void SeekTo(float time)
        {
            if (!IsInitialized || activeSourceIndex < 0) return;

            var source = audioSources[activeSourceIndex];
            if (source.clip != null)
            {
                time = Mathf.Clamp(time, 0f, source.clip.length);
                source.time = time;
            }
        }

        public bool TryGetTrackInfo(out MusicTrackInfo trackInfo)
        {
            trackInfo = null;

            if (!IsInitialized || currentPlaylist == null || currentTrackIndex < 0)
                return false;

            var track = currentPlaylist.Tracks[currentTrackIndex];
            trackInfo = new MusicTrackInfo
            {
                Id = track.id,
                DisplayName = track.displayName,
                Clip = track.clip,
                VolumeMultiplier = track.volumeMultiplier,
                Description = track.description,
                Tags = track.tags?.AsReadOnly(),
                StartTime = Time.time - CurrentTrackTime,
                Duration = track.clip?.length ?? 0f
            };

            return true;
        }

        private IEnumerator CrossfadeAudioSources(int fromIndex, int toIndex, float duration,
            float targetVolumeMultiplier)
        {
            isFading = true;
            float elapsedTime = 0;

            var fromSource = fromIndex >= 0 ? audioSources[fromIndex] : null;
            var toSource = audioSources[toIndex];

            float startVolume = fromSource?.volume ?? 0f;
            float targetVolume = volume * targetVolumeMultiplier;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;

                // Smoothstep for more natural fading
                float smoothT = t * t * (3f - 2f * t);

                if (fromSource != null)
                    fromSource.volume = Mathf.Lerp(startVolume, 0f, smoothT);

                toSource.volume = Mathf.Lerp(0f, targetVolume, smoothT);

                yield return null;
            }

            if (fromSource != null)
            {
                fromSource.Stop();
                fromSource.volume = 0f;
            }

            toSource.volume = targetVolume;

            fadeCoroutine = null;
            isFading = false;
        }

        private IEnumerator FadeOut(float duration, bool pause = false)
        {
            isFading = true;
            float elapsedTime = 0;
            float startVolume = audioSources[activeSourceIndex].volume;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;

                float smoothT = t * t * (3f - 2f * t);
                audioSources[activeSourceIndex].volume = Mathf.Lerp(startVolume, 0f, smoothT);

                yield return null;
            }

            if (pause)
                PauseImmediate();
            else
                StopImmediate();

            fadeCoroutine = null;
            isFading = false;
        }

        private IEnumerator FadeIn(float duration, float volumeMultiplier)
        {
            isFading = true;
            float elapsedTime = 0;
            float targetVolume = volume * volumeMultiplier;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;

                float smoothT = t * t * (3f - 2f * t);
                audioSources[activeSourceIndex].volume = Mathf.Lerp(0f, targetVolume, smoothT);

                yield return null;
            }

            audioSources[activeSourceIndex].volume = targetVolume;
            fadeCoroutine = null;
            isFading = false;
            OnPlaybackStateChanged?.Invoke(true);
        }

        private IEnumerator FadeVolume(float fromVolume, float toVolume, float duration)
        {
            isFading = true;
            float elapsedTime = 0;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;

                volume = Mathf.Lerp(fromVolume, toVolume, t);
                UpdateVolume();

                yield return null;
            }

            volume = toVolume;
            UpdateVolume();

            fadeCoroutine = null;
            isFading = false;
        }

        private void UpdateVolume()
        {
            if (currentPlaylist?.Tracks == null || currentTrackIndex < 0) return;

            float trackMultiplier = currentPlaylist.Tracks[currentTrackIndex].volumeMultiplier;

            foreach (var source in audioSources)
            {
                if (source.isPlaying)
                {
                    source.volume = volume * trackMultiplier;
                }
            }
        }

        private IEnumerator MonitorPlayback()
        {
            while (true)
            {
                if (isPlaying && !isPaused && !isFading && activeSourceIndex >= 0)
                {
                    var source = audioSources[activeSourceIndex];
                    if (!source.isPlaying ||
                        (source.clip != null && source.time >= source.clip.length - 0.1f))
                    {
                        if (TryGetTrackInfo(out var trackInfo))
                        {
                            OnTrackEnded?.Invoke(trackInfo);
                        }

                        PlayNext();
                    }
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private void RestoreLastPlayedTrack()
        {
            if (!config.rememberLastTrack) return;

            string savedTrackId = PlayerPrefs.GetString("LastPlayedTrackId", null);
            if (!string.IsNullOrEmpty(savedTrackId))
            {
                var track = currentPlaylist.GetTrackById(savedTrackId);
                if (track != null)
                {
                    var index = currentPlaylist.GetTrackIndex(track);
                    if (index >= 0)
                    {
                        PlayTrack(index, true);
                    }
                }
            }
        }

        private void HandleApplicationFocus(bool hasFocus)
        {
            if (!IsInitialized) return;

            if (!hasFocus && config.pauseOnFocusLost)
            {
                Pause(true);
            }
            else if (hasFocus && config.autoResumeOnFocus && isPaused)
            {
                Resume(true);
            }
        }

        private void LogDebug(string message)
        {
            if (config.logDebugMessages)
            {
                Debug.Log($"[MusicService] {message}");
            }
        }

        private void OnDisable()
        {
            Stop(false);
            Application.focusChanged -= HandleApplicationFocus;
        }

        private void OnDestroy()
        {
            if (playbackMonitorCoroutine != null)
            {
                StopCoroutine(playbackMonitorCoroutine);
            }
        }
    }
}