using System;
using Nexus.Core.Services;
using UnityEngine;

namespace Nexus.Audio
{
    [ServiceInterface]
    public interface IMusicService : IInitiable
    {
        float Volume { get; }
        bool IsPlaying { get; }
        bool IsPaused { get; }
        bool IsFading { get; }
        string CurrentTrackName { get; }
        float CurrentTrackTime { get; }
        float CurrentTrackLength { get; }
        float CurrentTrackProgress { get; } // 0-1 range
        
        void SetPlaylist(MusicPlaylist playlist, bool autoPlay = false);
        void PlayTrack(string trackId, bool fadeIn = true);
        void PlayTrack(int trackIndex, bool fadeIn = true);
        void PlayTrack(AudioClip clip, bool fadeIn = true);
        void PlayNext(bool fadeTransition = true);
        void PlayPrevious(bool fadeTransition = true);
        void Stop(bool fadeOut = true);
        void Pause(bool fadeOut = true);
        void Resume(bool fadeIn = true);
        void SetVolume(float volume, bool fade = true);
        void SeekTo(float time);
        bool TryGetTrackInfo(out MusicTrackInfo trackInfo);
        
        event Action<MusicTrackInfo> OnTrackStarted;
        event Action<MusicTrackInfo> OnTrackEnded;
        event Action<float> OnVolumeChanged;
        event Action<bool> OnPlaybackStateChanged;
    }
}