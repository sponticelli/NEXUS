using System;
using UnityEngine;

namespace Nexus.ScriptableEnums
{
    /// <summary>
    /// Represents a single item in a ScriptableEnum
    /// </summary>
    [System.Serializable]
    public struct ScriptableEnumItem
    {
        [SerializeField]
        private string name;
        public string Name => name;

        [SerializeField]
        private int value;
        public int Value => value;

#if UNITY_EDITOR
        [HideInInspector] 
        public ValidationError[] errors;
#endif

        public ScriptableEnumItem(string name, int value)
        {
            this.name = name;
            this.value = value;
#if UNITY_EDITOR
            this.errors = Array.Empty<ValidationError>();
#endif
        }

        public override string ToString() => $"{{{name}}}{{{value}}}";

        public void SetName(string newName) => name = newName;
        public void SetValue(int newValue) => value = newValue;
        public void SetErrors(ValidationError[] newErrors) => errors = newErrors;
    }
}