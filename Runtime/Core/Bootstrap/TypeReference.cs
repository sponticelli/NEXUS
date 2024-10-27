using System;
using UnityEngine;

namespace Nexus.Core.Bootstrap
{
    [Serializable]
    public class TypeReference : ISerializationCallbackReceiver
    {
        [SerializeField] private string typeName;

        public Type Type { get; private set; }

        public void SetType(Type type)
        {
            Type = type;
            typeName = type.AssemblyQualifiedName;
        }

        public void OnBeforeSerialize()
        {
            if (Type != null)
            {
                typeName = Type.AssemblyQualifiedName;
            }
        }

        public void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(typeName))
            {
                Type = Type.GetType(typeName);
            }
        }
    }
}