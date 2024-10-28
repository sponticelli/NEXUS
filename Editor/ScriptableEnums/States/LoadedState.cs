using UnityEditor;
using UnityEngine;

namespace Nexus.ScriptableEnums.State
{
    /// <summary>
    /// State for when editing a loaded enum
    /// </summary>
    public class LoadedState : EnumEditorState
    {
        public LoadedState(ScriptableEnumEditor editor) : base(editor) { }
        
        public override void OnGUI()
        {
            DrawCommonFields();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(Editor.ArrayProperty, new GUIContent("Elements"));
            if (EditorGUI.EndChangeCheck())
            {
                Editor.ValidateCurrentElements();
            }

            using (new EditorGUI.DisabledGroupScope(!Editor.HasUnsavedChanges))
            {
                if (GUILayout.Button("Save"))
                {
                    Editor.SaveEnum();
                }
            }

            if (GUILayout.Button("Stop Editing"))
            {
                Editor.StopEditing();
            }
        }
        
        public override void OnEnter() 
        {
            Editor.ValidateCurrentElements();
        }
        
        public override void OnExit() { }
    }
}