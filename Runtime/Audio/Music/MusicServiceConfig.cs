using UnityEngine;
using UnityEngine.Audio;

namespace Nexus.Audio
{
    [CreateAssetMenu(fileName = "MusicServiceConfig", menuName = "Nexus/Audio/Music Service Config")]
    public class MusicServiceConfig : ScriptableObject
    {
        [Header("Audio Settings")]
        [Range(0f, 1f)]
        public float defaultVolume = 0.7f;
        
        [Range(0.1f, 5f)]
        public float defaultFadeDuration = 2f;
        
        [Range(0.1f, 2f)]
        public float quickFadeDuration = 0.3f;

        [Header("Mixer Settings")]
        public AudioMixerGroup mixerGroup;
        public string volumeParameter = "MusicVolume";
        
        [Header("Audio Sources")]
        public int audioSourceCount = 2;
        
        [Header("Default Content")]
        public MusicPlaylist defaultPlaylist;

        [Header("Playback Settings")]
        public bool rememberLastTrack = true;
        public bool autoResumeOnFocus = true;
        public bool pauseOnFocusLost = true;
        public int maxHistorySize = 10;
        public bool logDebugMessages = false;
        

        [Header("Debug Settings")]
        [Tooltip("Enable detailed debug logging")]
        public bool enableDebugLogging = true;
    }
}