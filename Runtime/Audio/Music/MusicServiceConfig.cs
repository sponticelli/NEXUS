using UnityEngine;

namespace Nexus.Audio
{
    [CreateAssetMenu(fileName = "MusicServiceConfig", menuName = "Nexus/Audio/Music Service Config")]
    public class MusicServiceConfig : ScriptableObject
    {
        [Header("Audio Settings")]
        [Range(0f, 1f)]
        public float defaultVolume = 0.7f;
        
        [Range(0.1f, 5f)]
        public float crossfadeDuration = 2f;
        
        [Header("Audio Sources")]
        public int audioSourceCount = 2;
        
        [Header("Default Content")]
        public MusicPlaylist defaultPlaylist;
    }
}