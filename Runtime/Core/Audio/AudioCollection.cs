using System.Collections.Generic;
using UnityEngine;

namespace Nexus.Core.Audio
{
    /// <summary>
    /// Collection of audio clips for a specific category
    /// </summary>
    [CreateAssetMenu(fileName = "AudioCollection", menuName = "Nexus/Audio/Audio Collection")]
    public class AudioCollection : ScriptableObject
    {
        public List<AudioClipReference> clips = new List<AudioClipReference>();
        
        private Dictionary<string, AudioClipReference> clipLookup;

        public void Initialize()
        {
            clipLookup = new Dictionary<string, AudioClipReference>();
            foreach (var clipRef in clips)
            {
                if (!string.IsNullOrEmpty(clipRef.id) && clipRef.clip != null)
                {
                    clipLookup[clipRef.id] = clipRef;
                }
            }
        }

        public bool TryGetClipReference(string id, out AudioClipReference clipReference)
        {
            if (clipLookup == null)
            {
                Initialize();
            }
            return clipLookup.TryGetValue(id, out clipReference);
        }
    }
}