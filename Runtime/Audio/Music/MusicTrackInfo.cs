using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Audio
{
    public class MusicTrackInfo
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public AudioClip Clip { get; set; }
        public float VolumeMultiplier { get; set; }
        public string Description { get; set; }
        public IReadOnlyList<string> Tags { get; set; }
        public float StartTime { get; set; }
        public float Duration { get; set; }
    }
}