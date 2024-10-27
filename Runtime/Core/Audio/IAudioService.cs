using System.Threading.Tasks;

namespace Nexus.Core.Audio
{
    public interface IAudioService
    {
        Task PlaySound(string soundId, float volume = 1.0f);
        Task PlayMusic(string musicId, float fadeInDuration = -1);
        Task StopMusic(float fadeOutDuration = -1);
        void SetMasterVolume(float volume);
        void SetMusicVolume(float volume);
        void SetSfxVolume(float volume);
    }
}