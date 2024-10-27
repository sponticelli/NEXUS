using System.Collections.Generic;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.Audio;

namespace Nexus.Core.Audio
{
    [CreateAssetMenu(fileName = "AudioServiceConfig", menuName = "Nexus/Services/Config/Audio Service")]
    public class AudioServiceConfiguration : ServiceConfigurationBase
    {
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup masterGroup;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        
        [Header("Audio Collections")]
        [SerializeField] private AudioCollection musicCollection;
        [SerializeField] private AudioCollection sfxCollection;
        [SerializeField] private List<AudioCollection> additionalCollections;

        [Header("Audio Sources")]
        [SerializeField] private int initialSfxSourcePoolSize = 10;
        [SerializeField] private int maxSfxSourcePoolSize = 20;
        [SerializeField] private AudioSource musicSourcePrefab;
        [SerializeField] private AudioSource sfxSourcePrefab;

        [Header("Default Settings")]
        [SerializeField] private float defaultMusicFadeTime = 1.0f;
        [SerializeField] private float defaultMasterVolume = 1.0f;
        [SerializeField] private float defaultMusicVolume = 0.8f;
        [SerializeField] private float defaultSfxVolume = 1.0f;

        public AudioMixer AudioMixer => audioMixer;
        public AudioMixerGroup MasterGroup => masterGroup;
        public AudioMixerGroup MusicGroup => musicGroup;
        public AudioMixerGroup SfxGroup => sfxGroup;
        public AudioCollection MusicCollection => musicCollection;
        public AudioCollection SfxCollection => sfxCollection;
        public IReadOnlyList<AudioCollection> AdditionalCollections => additionalCollections;
        public int InitialSfxSourcePoolSize => initialSfxSourcePoolSize;
        public int MaxSfxSourcePoolSize => maxSfxSourcePoolSize;
        public AudioSource MusicSourcePrefab => musicSourcePrefab;
        public AudioSource SfxSourcePrefab => sfxSourcePrefab;
        public float DefaultMusicFadeTime => defaultMusicFadeTime;
        public float DefaultMasterVolume => defaultMasterVolume;
        public float DefaultMusicVolume => defaultMusicVolume;
        public float DefaultSfxVolume => defaultSfxVolume;

        public override bool Validate(out string error)
        {
            if (audioMixer == null)
            {
                error = "Audio Mixer is not assigned!";
                return false;
            }

            if (musicCollection == null)
            {
                error = "Music Collection is not assigned!";
                return false;
            }

            if (sfxCollection == null)
            {
                error = "SFX Collection is not assigned!";
                return false;
            }

            error = null;
            return true;
        }

        private void OnEnable()
        {
            // Initialize collections when the config is loaded
            musicCollection?.Initialize();
            sfxCollection?.Initialize();
            additionalCollections?.ForEach(c => c?.Initialize());
        }
    }
}