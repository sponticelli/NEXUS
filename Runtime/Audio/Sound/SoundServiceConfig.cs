using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace Nexus.Audio
{
    [CreateAssetMenu(fileName = "SoundServiceConfig", menuName = "Nexus/Audio/Sound/Sound Service Config")]
    public class SoundServiceConfig : ScriptableObject
    {
        [Header("Sound Library")]
        public SoundLibrary soundLibrary;
        
        [Header("Audio Mixer Groups")]
        public AudioMixerGroup masterGroup;
        public AudioMixerGroup uiGroup;
        public AudioMixerGroup sfxGroup;
        public AudioMixerGroup environmentGroup;
        
        [Header("Mixer Parameters")]
        public string masterVolumeParameter = "MasterVolume";
        public string uiVolumeParameter = "UIVolume";
        public string sfxVolumeParameter = "SFXVolume";
        public string environmentVolumeParameter = "EnvironmentVolume";
        
        [Header("Default Settings")]
        [Range(0f, 1f)]
        public float defaultMasterVolume = 1f;
        [Range(0f, 1f)]
        public float defaultUIVolume = 1f;
        [Range(0f, 1f)]
        public float defaultSFXVolume = 1f;
        [Range(0f, 1f)]
        public float defaultEnvironmentVolume = 1f;
        
        [FormerlySerializedAs("poolSize")] [Header("Audio Sources")]
        public int initialSources = 20;
        public int maxSources = 50;
        public bool autoExpand = true;
        
        [Header("Fade Settings")]
        [Range(0.1f, 2f)]
        public float defaultFadeDuration = 0.5f;
        
        [Header("3D Sound Settings")]
        [Range(0f, 500f)]
        public float maxDistance = 100f;
        [Range(0f, 50f)]
        public float minDistance = 1f;
        public AnimationCurve rolloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Debug Settings")] 
        public bool logDebugMessages = true;
        
        private void OnValidate()
        {
            if (soundLibrary == null)
            {
                Debug.LogWarning("No sound library assigned to SoundServiceConfig");
            }
        }
    }
}