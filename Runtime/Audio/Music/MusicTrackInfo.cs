using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Audio
{
    /// <summary>
    /// Represents track information for the music system.
    /// Can be used both for configuration and runtime state.
    /// </summary>
    [Serializable]
    public class MusicTrackInfo
    {
        #region Serialized Fields
        [SerializeField]
        private string id;
        
        [SerializeField]
        private string displayName;
        
        [SerializeField]
        private AudioClip clip;
        
        [SerializeField, Range(0f, 1f)]
        private float volumeMultiplier = 1f;
        
        [SerializeField, TextArea(1, 3)]
        private string description;
        
        [SerializeField]
        private List<string> tags = new List<string>();
        #endregion

        #region Runtime State
        private float startTime;
        private float duration;
        #endregion

        #region Properties
        public string Id
        {
            get => id;
#if UNITY_EDITOR
            set => id = value;
#endif
        }

        public string DisplayName
        {
            get => displayName;
#if UNITY_EDITOR
            set => displayName = value;
#endif
        }

        public AudioClip Clip => clip;
        public float VolumeMultiplier
        {
            get => volumeMultiplier;
#if UNITY_EDITOR
            set => volumeMultiplier = value;
#endif
        }

        public string Description => description;
        public IReadOnlyList<string> Tags => tags.AsReadOnly();
        public float StartTime => startTime;
        public float Duration => duration;
        #endregion

        #region Constructors
        // For serialization
        public MusicTrackInfo() { }

        // For runtime creation
        public MusicTrackInfo(string id, string displayName, AudioClip clip, float volumeMultiplier = 1f, 
            string description = null, List<string> tags = null)
        {
            this.id = id;
            this.displayName = displayName;
            this.clip = clip;
            this.volumeMultiplier = volumeMultiplier;
            this.description = description;
            this.tags = tags ?? new List<string>();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Creates a runtime copy with additional state information
        /// </summary>
        public MusicTrackInfo CreateRuntimeInfo(float startTime)
        {
            return new MusicTrackInfo
            {
                id = this.id,
                displayName = this.displayName,
                clip = this.clip,
                volumeMultiplier = this.volumeMultiplier,
                description = this.description,
                tags = new List<string>(this.tags),
                startTime = startTime,
                duration = this.clip?.length ?? 0f
            };
        }

        /// <summary>
        /// Validates the track information
        /// </summary>
        public bool Validate(out string error)
        {
            if (string.IsNullOrEmpty(id))
            {
                error = "Track ID cannot be empty";
                return false;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                error = "Display name cannot be empty";
                return false;
            }

            if (clip == null)
            {
                error = "Audio clip cannot be null";
                return false;
            }

            error = null;
            return true;
        }
        #endregion
    }
}