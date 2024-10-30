// ISoundService.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using Nexus.Core.Services;

namespace Nexus.Audio
{
    [ServiceInterface]
    public interface ISoundService : IInitiable
    {
        float MasterVolume { get; }
        float UIVolume { get; }
        float SFXVolume { get; }
        float EnvironmentVolume { get; }
        
        void SetMasterVolume(float volume, bool fade = true);
        void SetUIVolume(float volume, bool fade = true);
        void SetSFXVolume(float volume, bool fade = true);
        void SetEnvironmentVolume(float volume, bool fade = true);
        
        
        void PlayOneShot(string soundId, Vector3? position = null);
        ISoundHandle PlayLoop(string soundId, Vector3? position = null);
        IEnumerable<SoundEntry> GetSoundsByTag(string tag);
        
        void PlayOneShot(AudioClip clip, SoundType type, Vector3? position = null, float volumeScale = 1f);
        ISoundHandle PlayLoop(AudioClip clip, SoundType type, Vector3? position = null, float volumeScale = 1f);
        
        void PlayOneShot(string soundId, float pitch, Vector3? position = null);
        ISoundHandle PlayLoop(string soundId, float pitch, Vector3? position = null);
        
        
        void RemoveHandle(SoundHandle soundHandle);
        void StopAll(SoundType type, bool fade = true);
        void PauseAll(SoundType type, bool fade = true);
        void ResumeAll(SoundType type, bool fade = true);
        
        event Action<float> OnMasterVolumeChanged;
        event Action<float> OnUIVolumeChanged;
        event Action<float> OnSFXVolumeChanged;
        event Action<float> OnEnvironmentVolumeChanged;
        
        /// <summary>
        /// Gets the currently active sound library
        /// </summary>
        SoundLibrary CurrentLibrary { get; }

        /// <summary>
        /// Changes the active sound library
        /// </summary>
        /// <param name="library">The new library to use</param>
        /// <param name="mergeDuplicates">If true, sounds with duplicate IDs from the new library will replace existing ones</param>
        /// <returns>True if the library was successfully changed</returns>
        bool SetLibrary(SoundLibrary library, bool mergeDuplicates = false);

        /// <summary>
        /// Merges additional sounds from another library
        /// </summary>
        /// <param name="library">The library to merge from</param>
        /// <param name="overwriteExisting">If true, sounds with duplicate IDs will be replaced</param>
        /// <returns>The number of sounds added/updated</returns>
        int MergeLibrary(SoundLibrary library, bool overwriteExisting = false);
        
    }
}