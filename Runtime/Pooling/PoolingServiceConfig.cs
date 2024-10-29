using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nexus.Pooling
{
    [CreateAssetMenu(fileName = "PoolingServiceConfig", menuName = "Nexus/Pooling/Pooling Service Config")]
    public class PoolingServiceConfig : ScriptableObject
    {
        [SerializeField]
        private List<PoolConfiguration> poolConfigurations = new List<PoolConfiguration>();
        public IReadOnlyList<PoolConfiguration> Configurations => poolConfigurations;

        private void OnValidate()
        {
            Validate();
        }

        public void Validate()
        {
            // Validate unique IDs
            var duplicateIds = poolConfigurations
                .GroupBy(c => c.id)
                .Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateIds.Any())
            {
                Debug.LogError($"Duplicate pool IDs found: {string.Join(", ", duplicateIds)}");
            }

            // Validate that all pools have IDs
            var missingIds = poolConfigurations
                .Where(c => string.IsNullOrEmpty(c.id))
                .Select(c => c.prefab != null ? c.prefab.name : "null")
                .ToList();

            if (missingIds.Any())
            {
                Debug.LogError($"Missing pool IDs for prefabs: {string.Join(", ", missingIds)}");
            }
        }
    }
}