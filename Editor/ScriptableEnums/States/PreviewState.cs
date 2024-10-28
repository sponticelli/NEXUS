using UnityEditor;
using UnityEngine;

namespace Nexus.ScriptableEnums.State
{
    /// <summary>
    /// State for when previewing an enum
    /// </summary>
    public class PreviewState : EnumEditorState
    {
        public PreviewState(ScriptableEnumEditor editor) : base(editor) { }
        
        public override void OnGUI()
        {
            DrawCommonFields();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(Editor.ArrayProperty, new GUIContent("Preview"));
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Load"))
            {
                Editor.LoadEnum();
            }
        }
        
        public override void OnEnter() { }
        public override void OnExit() { }
    }
}