using UnityEngine;

namespace Nexus.ScriptableEnums.State
{
    /// <summary>
    /// State for when searching for an enum
    /// </summary>
    public class SearchingState : EnumEditorState
    {
        public SearchingState(ScriptableEnumEditor editor) : base(editor) { }
        
        public override void OnGUI()
        {
            DrawCommonFields();

            GUI.enabled = !string.IsNullOrEmpty(Editor.EnumName);
            if (GUILayout.Button("Create"))
            {
                Editor.CreateEnum();
            }
            GUI.enabled = true;
        }
        
        public override void OnEnter() { }
        public override void OnExit() { }
    }
}