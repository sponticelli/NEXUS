using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nexus.Audio
{
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Nexus/Audio/Sound/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [SerializeField]
        private List<SoundEntry> sounds = new List<SoundEntry>();

        public IReadOnlyList<SoundEntry> Sounds => sounds.AsReadOnly();

        public SoundEntry GetSound(string id)
        {
            return sounds.FirstOrDefault(s => s.Id == id);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateLibrary();
        }

        private void ValidateLibrary()
        {
            // Generate IDs for entries that don't have one
            foreach (var sound in sounds.Where(s => string.IsNullOrEmpty(s.Id)))
            {
                sound.Id = System.Guid.NewGuid().ToString();
            }

            // Check for duplicates
            var duplicates = sounds.GroupBy(s => s.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicate in duplicates)
            {
                Debug.LogError($"Duplicate sound ID found in library: {duplicate}");
            }
        }
#endif
    }
}