#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Nexus.ScriptableEnums
{
    /// <summary>
    /// Represents a ScriptableObject-based enum definition that can be edited in the Unity Inspector
    /// </summary>
    [CreateAssetMenu(fileName = "ScriptableEnum", menuName = "Nexus/Scriptable/Enum")]
    public class ScriptableEnum : ScriptableObject
    {
        [SerializeField] 
        private string enumName;
        public string EnumName => enumName;

        [SerializeField] 
        private string namespaceName;
        public string NamespaceName => namespaceName;

        [SerializeField] 
        private bool useCustomNamespace;
        public bool UseCustomNamespace => useCustomNamespace;

        [SerializeField] 
        private ScriptableEnumItem[] items = new ScriptableEnumItem[0];
        public ScriptableEnumItem[] Items => items;

#if UNITY_EDITOR
        
        [HideInInspector] public EditState editState;

        [HideInInspector] public string enumScriptPath;

        public enum EditState
        {
            Searching,
            Preview,
            Loaded
        }

        public void SetItems(ScriptableEnumItem[] newItems)
        {
            items = newItems;
        }

        public void SetEnumName(string newName)
        {
            enumName = newName;
        }

        public void SetNamespace(string newNamespace)
        {
            namespaceName = newNamespace;
        }

        public void SetUseCustomNamespace(bool useCustom)
        {
            useCustomNamespace = useCustom;
        }

        
        public string GetEffectiveNamespace()
        {
            if (!useCustomNamespace)
                return EditorSettings.projectGenerationRootNamespace;
            return string.IsNullOrEmpty(namespaceName) ? null : namespaceName;
        }
#endif
    }
}