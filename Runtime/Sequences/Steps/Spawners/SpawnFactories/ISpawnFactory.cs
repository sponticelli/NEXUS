using UnityEngine;

namespace Nexus.Sequences
{
    /// <summary>
    /// Factory interface for creating spawn objects
    /// </summary>
    public interface ISpawnFactory
    {
        GameObject CreateSpawnObject(Vector3 position, Quaternion rotation);
    }
}