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
        [SerializeField]
        private List<MusicTrackInfo> tracks = new List<MusicTrackInfo>();

        


        [Tooltip("Should the tracks be shuffled when the playlist starts?")]
        [SerializeField]
        private bool shuffleOnStart = false;

        [Tooltip("Should the playlist loop when it reaches the end?")]
        [SerializeField]
        private bool loopPlaylist = true;

        [Tooltip("Should the playlist automatically start playing when set?")]
        [SerializeField]
        private bool autoPlayOnSet = true;

        // Public properties
        public IReadOnlyList<MusicTrackInfo> Tracks => tracks.AsReadOnly();
        public bool ShuffleOnStart => shuffleOnStart;
        public bool LoopPlaylist => loopPlaylist;
        public bool AutoPlayOnSet => autoPlayOnSet;
        public int TrackCount => tracks.Count;

        // Returns a track by its ID
        public MusicTrackInfo GetTrackById(string id)
        {
            return tracks.FirstOrDefault(t => t.Id == id);
        }

        // Returns a track by its index
        public MusicTrackInfo GetTrackByIndex(int index)
        {
            return index >= 0 && index < tracks.Count ? tracks[index] : null;
        }

        // Returns tracks matching specific tags
        public IEnumerable<MusicTrackInfo> GetTracksByTags(params string[] tags)
        {
            return tracks.Where(t => tags.All(tag => t.Tags.Contains(tag)));
        }
        
        public int FindTrackIndex(string displayName)
        {
            return tracks.FindIndex(t => t.DisplayName == displayName);
        }
        
        public int FindTrackIndexById(string id)
        {
            return tracks.FindIndex(t => t.Id == id);
        }

        // Find track by ID
        public MusicTrackInfo FindTrackById(string id)
        {
            return tracks.FirstOrDefault(t => t.Id == id);
        }

        // Get track index with validation
        public int GetTrackIndex(MusicTrackInfo track)
        {
            if (track == null) return -1;
            return tracks.IndexOf(track);
        }

        // Returns a shuffled copy of the tracks list
        public IReadOnlyList<MusicTrackInfo> GetShuffledTracks()
        {
            var shuffled = new List<MusicTrackInfo>(tracks);
            shuffled.ShuffleList();
            return shuffled;
        }

        // Returns the total duration of all tracks
        public float GetTotalDuration()
        {
            return tracks.Sum(t => t.Clip != null ? t.Clip.length : 0f);
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
                if (string.IsNullOrEmpty(track.Id))
                {
                    track.Id = System.Guid.NewGuid().ToString();
                }
                
                // Check for duplicate IDs
                if (usedIds.Contains(track.Id))
                {
                    Debug.LogError($"Duplicate track ID found in playlist {name}: {track.Id}");
                    track.Id = System.Guid.NewGuid().ToString();
                }
                usedIds.Add(track.Id);
                
                // Generate display name if missing
                if (string.IsNullOrEmpty(track.DisplayName) && track.Clip != null)
                {
                    track.DisplayName = track.Clip.name;
                }
                
                // Validate volume multiplier
                track.VolumeMultiplier = Mathf.Clamp01(track.VolumeMultiplier);
                
                // Check for missing audio clips
                if (track.Clip == null)
                {
                    Debug.LogWarning($"Missing audio clip for track {track.DisplayName} in playlist {name}");
                }
            }
        }

        // Editor utility method to add a track
        public void AddTrack(AudioClip clip, string displayName = null, float volumeMultiplier = 1f)
        {
            var track = new MusicTrackInfo(Guid.NewGuid().ToString(), displayName ?? clip.name, clip, volumeMultiplier);
            
            tracks.Add(track);
            ValidatePlaylist();
        }

        // Editor utility method to remove a track
        public void RemoveTrack(string id)
        {
            tracks.RemoveAll(t => t.Id == id);
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