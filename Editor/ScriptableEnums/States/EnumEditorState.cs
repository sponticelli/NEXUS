using UnityEditor;

namespace Nexus.ScriptableEnums.State
{
    /// <summary>
    /// Base class for editor states
    /// </summary>
    public abstract class EnumEditorState
    {
        protected readonly ScriptableEnumEditor Editor;
        
        protected EnumEditorState(ScriptableEnumEditor editor)
        {
            Editor = editor;
        }
        
        public abstract void OnGUI();
        public abstract void OnEnter();
        public abstract void OnExit();
        
        protected bool DrawNameField()
        {
            EditorGUI.BeginChangeCheck();
            var nameRect = EditorGUILayout.GetControlRect();
            var newName = EditorGUI.TextField(nameRect, "Enum Name", Editor.EnumName);
            
            if (EditorGUI.EndChangeCheck())
            {
                Editor.SetEnumName(newName);
                return true;
            }
            
            return false;
        }

        protected void DrawCommonFields()
        {
            // Draw name field
            if (DrawNameField())
            {
                Editor.RefreshState();
            }

            // Draw namespace field
            Editor.DrawNamespaceField();
            
            EditorGUILayout.Space(10);
        }
    }
}