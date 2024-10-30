using System;
using UnityEngine;

namespace Nexus.Audio
{
    public interface ISoundHandle : IDisposable
    {
        bool IsPlaying { get; }
        bool IsPaused { get; }
        bool IsFading { get; }
        float Volume { get; }
        void Stop(bool fade = true);
        void Pause(bool fade = true);
        void Resume(bool fade = true);
        void SetVolume(float volume, bool fade = true);
        void SetPosition(Vector3 position);
    }
}