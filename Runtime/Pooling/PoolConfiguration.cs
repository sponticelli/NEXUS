using System;
using UnityEngine;

namespace Nexus.Pooling
{
    [Serializable]
    public class PoolConfiguration
    {
        [Tooltip("Unique identifier for this pool")]
        public string id = "PoolID";
        
        [Tooltip("Prefab to pool")]
        public GameObject prefab;
        
        [Tooltip("Initial number of instances to create")]
        [Min(0)]
        public int initialSize = 10;
        
        [Tooltip("Maximum number of instances allowed")]
        [Min(1)]
        public int maxSize = 100;
        
        [Tooltip("Whether the pool should create new instances when empty")]
        public bool autoExpand = true;
        
        [Tooltip("Time in seconds before automatically returning to pool (-1 for never)")]
        public float recycleTimeout = -1f;
    }
}