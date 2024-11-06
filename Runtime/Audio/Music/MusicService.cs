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
                ? currentPlaylist.Tracks[currentTrackIndex].DisplayName
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

                await Task.Yield();
                
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
            if (source == null)
            {
                Debug.LogError("Attempted to configure null AudioSource");
                return;
            }

            source.playOnAwake = false;
            source.loop = true; // Ensure looping is explicitly set
            source.outputAudioMixerGroup = config.mixerGroup;
            source.priority = 0; // Highest priority
            source.spatialBlend = 0f; // Pure TwoD
            source.reverbZoneMix = 0f; // No reverb
            source.dopplerLevel = 0f; // No doppler effect
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = 500f;

            // Validate configuration
            Debug.Log(
                $"Configured AudioSource - Loop: {source.loop}, Priority: {source.priority}, Volume: {source.volume}");
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

        public void PlayTrack(string trackId, bool fadeIn = true)
        {
            if (!IsInitialized || currentPlaylist?.Tracks == null) return;

            var trackIndex = currentPlaylist.FindTrackIndexById(trackId);
            if (trackIndex >= 0)
            {
                PlayTrack(trackIndex, fadeIn);
            }
            else
            {
                Debug.LogWarning($"Track with ID '{trackId}' not found in the current playlist.");
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
                    Debug.Log($"Found track: {track.DisplayName}, clip: {(track.Clip != null ? "valid" : "null")}");

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
                    lastPlayedTrackId = track.Id;

                    Debug.Log("Calling PlayTrackInternal");
                    PlayTrackInternal(track, fadeIn);

                    Debug.Log("Saving last played track");
                    if (config.rememberLastTrack)
                    {
                        PlayerPrefs.SetString("LastPlayedTrackId", lastPlayedTrackId);
                        PlayerPrefs.Save();
                    }

                    Debug.Log($"PlayTrack completed for: {track.DisplayName}");
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

            var trackInfo = new MusicTrackInfo(Guid.NewGuid().ToString(), clip.name, clip).CreateRuntimeInfo(Time.time);
            PlayTrackInternal(trackInfo, fadeIn);
        }

        private void PlayTrackInternal(MusicTrackInfo trackInfo, bool fadeIn)
        {
            if (!IsInitialized)
            {
                Debug.LogError("Attempting to play track before service is initialized");
                return;
            }

            Debug.Log($"=== Starting PlayTrackInternal ===");
            Debug.Log($"Track: {trackInfo?.DisplayName}, Fade: {fadeIn}");

            try
            {
                if (trackInfo?.Clip == null)
                {
                    Debug.LogError("Attempted to play null track or clip");
                    return;
                }

                // Calculate next source index
                int nextSourceIndex = activeSourceIndex < 0 ? 0 : (activeSourceIndex + 1) % audioSources.Length;
                var nextSource = audioSources[nextSourceIndex];

                if (nextSource == null)
                {
                    Debug.LogError($"AudioSource at index {nextSourceIndex} is null");
                    return;
                }

                // Stop any existing fade
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                    fadeCoroutine = null;
                }

                // Configure the next source
                nextSource.clip = trackInfo.Clip;
                nextSource.loop = true; // Ensure loop is set
                float targetVolume = volume * trackInfo.VolumeMultiplier;

                if (!fadeIn)
                {
                    nextSource.volume = targetVolume;
                    Debug.Log($"Set immediate volume: {targetVolume}");
                }
                else
                {
                    nextSource.volume = 0f;
                    Debug.Log("Set initial volume to 0 for fade");
                }

                // Start playback
                nextSource.Play();
                Debug.Log($"Started playback on source {nextSourceIndex}");

                // Verify playback started
                if (!nextSource.isPlaying)
                {
                    Debug.LogError("Failed to start playback!");
                    return;
                }

                // Handle fade if needed
                if (fadeIn)
                {
                    fadeCoroutine = StartCoroutine(CrossfadeAudioSources(
                        activeSourceIndex,
                        nextSourceIndex,
                        config.defaultFadeDuration,
                        trackInfo.VolumeMultiplier
                    ));
                }
                else if (activeSourceIndex >= 0)
                {
                    var oldSource = audioSources[activeSourceIndex];
                    if (oldSource != null)
                    {
                        oldSource.Stop();
                        oldSource.volume = 0f;
                    }
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
                float volumeMultiplier = currentTrack?.VolumeMultiplier ?? 1f;

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
            trackInfo = track.CreateRuntimeInfo(CurrentTrackTime);

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

            // Set initial volume for new source to 0
            toSource.volume = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);

                // Smoothstep for more natural fading
                float smoothT = t * t * (3f - 2f * t);

                if (fromSource != null && fromSource.isPlaying)
                {
                    fromSource.volume = Mathf.Lerp(startVolume, 0f, smoothT);
                }

                toSource.volume = Mathf.Lerp(0f, targetVolume, smoothT);

                yield return null;
            }

            // Ensure final volumes are set exactly
            if (fromSource != null)
            {
                fromSource.Stop();
                fromSource.volume = 0f;
            }

            toSource.volume = targetVolume;

            Debug.Log($"Crossfade complete. Final volume: {toSource.volume}, Target volume: {targetVolume}");

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
                float t = Mathf.Clamp01(elapsedTime / duration);

                float smoothT = t * t * (3f - 2f * t);
                audioSources[activeSourceIndex].volume = Mathf.Lerp(startVolume, 0f, smoothT);

                yield return null;
            }

            // Ensure final volume is exactly 0
            audioSources[activeSourceIndex].volume = 0f;

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

            // Set initial volume to 0
            audioSources[activeSourceIndex].volume = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);

                float smoothT = t * t * (3f - 2f * t);
                audioSources[activeSourceIndex].volume = Mathf.Lerp(0f, targetVolume, smoothT);

                yield return null;
            }

            // Ensure final volume is set exactly
            audioSources[activeSourceIndex].volume = targetVolume;

            Debug.Log(
                $"Fade in complete. Final volume: {audioSources[activeSourceIndex].volume}, Target volume: {targetVolume}");

            fadeCoroutine = null;
            isFading = false;
            OnPlaybackStateChanged?.Invoke(true);
        }

        private IEnumerator FadeVolume(float fromVolume, float toVolume, float duration)
        {
            isFading = true;
            float elapsedTime = 0;

            // Get the current track's volume multiplier
            float volumeMultiplier = 1f;
            if (currentPlaylist?.Tracks != null && currentTrackIndex >= 0)
            {
                volumeMultiplier = currentPlaylist.Tracks[currentTrackIndex].VolumeMultiplier;
            }

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);

                volume = Mathf.Lerp(fromVolume, toVolume, t);

                // Apply the volume with multiplier to the active source
                if (activeSourceIndex >= 0 && activeSourceIndex < audioSources.Length)
                {
                    audioSources[activeSourceIndex].volume = volume * volumeMultiplier;
                }

                yield return null;
            }

            // Ensure final volume is set exactly
            volume = toVolume;
            if (activeSourceIndex >= 0 && activeSourceIndex < audioSources.Length)
            {
                audioSources[activeSourceIndex].volume = volume * volumeMultiplier;
            }

            fadeCoroutine = null;
            isFading = false;
        }

        private void UpdateVolume()
        {
            if (currentPlaylist?.Tracks == null || currentTrackIndex < 0 || activeSourceIndex < 0) return;

            float trackMultiplier = currentPlaylist.Tracks[currentTrackIndex].VolumeMultiplier;
            float targetVolume = volume * trackMultiplier;

            var source = audioSources[activeSourceIndex];
            if (source != null && source.isPlaying)
            {
                source.volume = targetVolume;
                Debug.Log($"Updated volume to {targetVolume} (base: {volume}, multiplier: {trackMultiplier})");
            }
        }

        private IEnumerator MonitorPlayback()
        {
            Debug.Log("Starting playback monitor");
            var checkInterval = 0.1f; // Check every 100ms for more responsive monitoring
            var endThreshold = 0.1f; // Time before end to consider for track completion

            while (true)
            {
                if (isPlaying && !isPaused && !isFading && activeSourceIndex >= 0)
                {
                    var source = audioSources[activeSourceIndex];

                    if (source != null)
                    {
                        // Ensure loop setting persists
                        if (!source.loop)
                        {
                            Debug.LogWarning("Loop setting was disabled - re-enabling");
                            source.loop = true;
                        }

                        // Log current state
                        if (config.logDebugMessages)
                        {
                            LogPlaybackState(source);
                        }

                        // Check if source actually playing
                        if (!source.isPlaying)
                        {
                            Debug.LogWarning(
                                $"Source stopped playing unexpectedly - Track: {CurrentTrackName}, Time: {source.time:F2}");

                            // Try to recover playback
                            if (source.clip != null)
                            {
                                Debug.Log("Attempting to resume playback");
                                source.Play();
                                source.time = 0; // Start from beginning
                                yield return new WaitForSeconds(checkInterval);

                                if (!source.isPlaying)
                                {
                                    Debug.LogError("Failed to resume playback - trying next track");
                                    HandleTrackCompletion();
                                }
                            }
                        }
                        else
                        {
                            // Validate volume
                            if (Math.Abs(source.volume) < float.Epsilon)
                            {
                                Debug.LogWarning("Volume is zero - restoring volume");
                                UpdateVolume();
                            }

                            // Only check for track completion if not looping
                            if (!currentPlaylist.LoopPlaylist && source.time >= (source.clip.length - endThreshold))
                            {
                                Debug.Log("Track completed naturally");
                                HandleTrackCompletion();
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"Null audio source at index {activeSourceIndex}");
                    }
                }

                yield return new WaitForSeconds(checkInterval);
            }
        }

        private void HandleTrackCompletion()
        {
            if (TryGetTrackInfo(out var trackInfo))
            {
                OnTrackEnded?.Invoke(trackInfo);
            }

            if (currentPlaylist?.LoopPlaylist == true ||
                (currentTrackIndex + 1 < currentPlaylist?.TrackCount))
            {
                PlayNext(true);
            }
            else
            {
                Stop(true);
            }
        }

        private void LogPlaybackState(AudioSource source)
        {
            Debug.Log($"Playback State - Track: {CurrentTrackName}" +
                      $"\nPlaying: {source.isPlaying}, Time: {source.time:F2}/{source.clip?.length:F2}" +
                      $"\nVolume: {source.volume:F3}, Loop: {source.loop}" +
                      $"\nMuted: {source.mute}, Enabled: {source.enabled}");
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