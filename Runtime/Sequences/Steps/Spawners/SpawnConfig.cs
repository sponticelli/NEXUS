using UnityEngine;

namespace Nexus.Sequences
{
    /// <summary>
    /// Configuration for spawn behavior
    /// </summary>
    [System.Serializable]
    public class SpawnConfig
    {
        [Header("Spawn Settings")]
        [Tooltip("How objects should be spawned across points")]
        public SpawnStrategy spawnStrategy = SpawnStrategy.Sequential;

        [Tooltip("Total number of objects to spawn")]
        public int spawnCount = 10;

        [Header("Timing")]
        [Tooltip("Time between spawns")]
        public float spawnInterval = 1f;

        [Tooltip("Random variation in spawn interval")]
        public float intervalVariation = 0.1f;

        [Header("Positioning")]
        [Tooltip("Random position offset from spawn point")]
        public Vector3 positionVariation = Vector3.zero;

        [Tooltip("Random rotation variation in degrees")]
        public Vector3 rotationVariation = Vector3.zero;

        [Tooltip("Random scale variation")]
        public Vector3 scaleVariation = Vector3.zero;

        [Header("Effects")]
        [Tooltip("Particle system to play on spawn")]
        public ParticleSystem spawnEffect;

        [Tooltip("Audio clip to play on spawn")]
        public AudioClip spawnSound;
    }
}