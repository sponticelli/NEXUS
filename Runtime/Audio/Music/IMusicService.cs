using System;
using Nexus.Core.Services;
using UnityEngine;

namespace Nexus.Audio
{
    public interface IMusicService : IInitiable
    {
        float Volume { get; }
        bool IsPlaying { get; }
        bool IsPaused { get; }
        string CurrentTrackName { get; }
        
        void SetPlaylist(MusicPlaylist playlist);
        void PlayTrack(string trackName);
        void PlayTrack(int trackIndex);
        void PlayTrack(AudioClip clip);
        void PlayNext();
        void PlayPrevious();
        void Stop();
        void Pause();
        void Resume();
        void SetVolume(float volume);
        
        event Action<string> OnTrackChanged;
        event Action<float> OnVolumeChanged;
        event Action<bool> OnPlaybackStateChanged;
    }
}