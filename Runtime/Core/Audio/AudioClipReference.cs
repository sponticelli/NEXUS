using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Nexus.Core.Audio
{
    /// <summary>
    /// Audio clip reference that can be directly assigned in the inspector
    /// </summary>
    [Serializable]
    public class AudioClipReference
    {
        public string id;
        public AudioClip clip;
        [Tooltip("Optional mixer group for this specific clip")]
        public AudioMixerGroup mixerGroup;
        [Tooltip("Default volume for this clip")]
        [Range(0f, 1f)] public float defaultVolume = 1f;
    }
}