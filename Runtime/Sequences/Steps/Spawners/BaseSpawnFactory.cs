using UnityEngine;

namespace Nexus.Sequences
{
    public abstract class BaseSpawnFactory : MonoBehaviour, ISpawnFactory
    {
        public abstract GameObject CreateSpawnObject(Vector3 position, Quaternion rotation);
    }
}