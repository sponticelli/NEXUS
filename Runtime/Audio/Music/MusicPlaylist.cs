using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Nexus.Extensions;

namespace Nexus.Audio
{
    [CreateAssetMenu(fileName = "MusicPlaylist", menuName = "Nexus/Audio/Music Playlist")]
    public class MusicPlaylist : ScriptableObject
    {
        [Serializable]
        public class TrackInfo
        {
            [Tooltip("Unique identifier for the track")]
            public string id;
            
            [Tooltip("Display name of the track")]
            public string displayName;
            
            [Tooltip("The audio clip to play")]
            public AudioClip clip;
            
            [Tooltip("Volume multiplier for this specific track (0-1)")]
            [Range(0f, 1f)]
            public float volumeMultiplier = 1f;
            
            [Tooltip("Optional track description or credits")]
            [TextArea(1, 3)]
            public string description;
            
            [Tooltip("Tags for filtering and organization")]
            public List<string> tags = new List<string>();
        }

        [SerializeField]
        private List<TrackInfo> tracks = new List<TrackInfo>();

        [Tooltip("Should the tracks be shuffled when the playlist starts?")]
        [SerializeField]
        private bool shuffleOnStart = false;

        [Tooltip("Should the playlist loop when it reaches the end?")]
        [SerializeField]
        private bool loopPlaylist = true;

        // Public properties
        public IReadOnlyList<TrackInfo> Tracks => tracks;
        public bool ShuffleOnStart => shuffleOnStart;
        public bool LoopPlaylist => loopPlaylist;
        public int TrackCount => tracks.Count;

        // Returns a track by its ID
        public TrackInfo GetTrackById(string id)
        {
            return tracks.FirstOrDefault(t => t.id == id);
        }

        // Returns a track by its index
        public TrackInfo GetTrackByIndex(int index)
        {
            return index >= 0 && index < tracks.Count ? tracks[index] : null;
        }

        // Returns tracks matching specific tags
        public IEnumerable<TrackInfo> GetTracksByTags(params string[] tags)
        {
            return tracks.Where(t => tags.All(tag => t.tags.Contains(tag)));
        }

        // Returns a shuffled copy of the tracks list
        public IReadOnlyList<TrackInfo> GetShuffledTracks()
        {
            var shuffled = new List<TrackInfo>(tracks);
            shuffled.ShuffleList();
            return shuffled;
        }

        // Returns the total duration of all tracks
        public float GetTotalDuration()
        {
            return tracks.Sum(t => t.clip != null ? t.clip.length : 0f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidatePlaylist();
        }

        private void ValidatePlaylist()
        {
            // Validate tracks
            var usedIds = new HashSet<string>();
            
            for (int i = 0; i < tracks.Count; i++)
            {
                var track = tracks[i];
                
                // Generate ID if missing
                if (string.IsNullOrEmpty(track.id))
                {
                    track.id = System.Guid.NewGuid().ToString();
                }
                
                // Check for duplicate IDs
                if (usedIds.Contains(track.id))
                {
                    Debug.LogError($"Duplicate track ID found in playlist {name}: {track.id}");
                    track.id = System.Guid.NewGuid().ToString();
                }
                usedIds.Add(track.id);
                
                // Generate display name if missing
                if (string.IsNullOrEmpty(track.displayName) && track.clip != null)
                {
                    track.displayName = track.clip.name;
                }
                
                // Validate volume multiplier
                track.volumeMultiplier = Mathf.Clamp01(track.volumeMultiplier);
                
                // Check for missing audio clips
                if (track.clip == null)
                {
                    Debug.LogWarning($"Missing audio clip for track {track.displayName} in playlist {name}");
                }
            }
        }

        // Editor utility method to add a track
        public void AddTrack(AudioClip clip, string displayName = null, float volumeMultiplier = 1f)
        {
            var track = new TrackInfo
            {
                id = System.Guid.NewGuid().ToString(),
                displayName = displayName ?? clip.name,
                clip = clip,
                volumeMultiplier = volumeMultiplier
            };
            
            tracks.Add(track);
            ValidatePlaylist();
        }

        // Editor utility method to remove a track
        public void RemoveTrack(string id)
        {
            tracks.RemoveAll(t => t.id == id);
        }

        // Editor utility method to reorder tracks
        public void ReorderTrack(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= tracks.Count || 
                newIndex < 0 || newIndex >= tracks.Count)
                return;

            var track = tracks[oldIndex];
            tracks.RemoveAt(oldIndex);
            tracks.Insert(newIndex, track);
        }
#endif
    }
}