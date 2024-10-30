using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Audio
{
    [System.Serializable]
    public class SoundEntry
    {
        [SerializeField]
        private string id;
        public string Id
        {
            get => id;
#if UNITY_EDITOR
            set => id = value;
#endif
        }

        [SerializeField]
        private string displayName;
        public string DisplayName => displayName;

        [SerializeField]
        private AudioClip clip;
        public AudioClip Clip => clip;

        [SerializeField]
        private SoundType type;
        public SoundType Type => type;

        [SerializeField]
        [Range(0f, 1f)]
        private float defaultVolume = 1f;
        public float DefaultVolume => defaultVolume;
        
        [SerializeField]
        [Range(-3f, 3f)]
        private float pitchMin = 1f;
        public float PitchMin => pitchMin;

        [SerializeField]
        [Range(-3f, 3f)]
        private float pitchMax = 1f;
        public float PitchMax => pitchMax;

        [SerializeField]
        private bool randomizePitch;
        public bool RandomizePitch => randomizePitch;

        [SerializeField]
        private bool spatialize;
        public bool Spatialize => spatialize;

        [SerializeField]
        [Range(0f, 500f)]
        private float maxDistance = 100f;
        public float MaxDistance => maxDistance;

        [SerializeField]
        [Range(0f, 50f)]
        private float minDistance = 1f;
        public float MinDistance => minDistance;

        [SerializeField]
        private List<string> tags = new List<string>();
        public IReadOnlyList<string> Tags => tags.AsReadOnly();
        
        
    }
}