using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nexus.ScriptableEnums.Services;
using Nexus.ScriptableEnums.State;
using UnityEngine;

namespace Nexus.ScriptableEnums
{
    /// <summary>
    /// Custom editor for ScriptableEnum assets
    /// </summary>
    [CustomEditor(typeof(ScriptableEnum))]
    public class ScriptableEnumEditor : Editor
    {
        private ScriptableEnum _scriptableEnum;
        private EnumEditorState _currentState;
        private readonly IEnumFileService _fileService;
        private readonly IEnumValidator _validator;
        private Type _currentType;
        private bool _hasUnsavedChanges;
        private string _errorMessage;

        public SerializedProperty ArrayProperty { get; private set; }
        public string EnumName => _scriptableEnum.EnumName;
        public bool HasUnsavedChanges => _hasUnsavedChanges;

        private readonly Dictionary<ScriptableEnum.EditState, EnumEditorState> _states;
        
        private SerializedProperty _namespaceProperty;
        private SerializedProperty _useCustomNamespaceProperty;
        
        private static readonly Dictionary<string, Type> _typeCache = new();
    
        // Clear cache on domain reload
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            AssemblyReloadEvents.afterAssemblyReload += ClearTypeCache;
        }

        public ScriptableEnumEditor()
        {
            _fileService = new EnumFileService();
            _validator = new EnumValidator();

            // Initialize state dictionary
            _states = new Dictionary<ScriptableEnum.EditState, EnumEditorState>
            {
                { ScriptableEnum.EditState.Searching, new SearchingState(this) },
                { ScriptableEnum.EditState.Preview, new PreviewState(this) },
                { ScriptableEnum.EditState.Loaded, new LoadedState(this) }
            };
        }

        private void OnEnable()
        {
            _scriptableEnum = (ScriptableEnum)target;
            ArrayProperty = serializedObject.FindProperty("items");
            _namespaceProperty = serializedObject.FindProperty("namespaceName");
            _useCustomNamespaceProperty = serializedObject.FindProperty("useCustomNamespace");
            RefreshState();
        }

        private void OnDisable()
        {
            if (_hasUnsavedChanges)
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes", 
                    "You have unsaved changes. Would you like to save them?", "Save", "Cancel"))
                {
                    SaveEnum();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (!string.IsNullOrEmpty(_errorMessage))
            {
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
            }

            _currentState?.OnGUI();
            
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Refreshes the current state based on the enum name
        /// </summary>
        public async void RefreshState()
        {
            try 
            {
                _currentState?.OnExit();

                var newType = await FindExistingEnumType();
                _currentType = newType;
        
                var newState = newType != null
                    ? ScriptableEnum.EditState.Preview 
                    : ScriptableEnum.EditState.Searching;

                _scriptableEnum.editState = newState;
                _currentState = _states[newState];
                _currentState.OnEnter();

                _errorMessage = null;
                _hasUnsavedChanges = false;
        
                // Force a repaint since we're async
                Repaint();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error refreshing state: {ex}");
                _errorMessage = "Failed to refresh enum state";
                Repaint();
            }
        }

        /// <summary>
        /// Sets the enum name and triggers a state refresh
        /// </summary>
        public void SetEnumName(string newName)
        {
            if (_scriptableEnum.EnumName == newName) return;
            
            Undo.RecordObject(_scriptableEnum, "Change Enum Name");
            _scriptableEnum.SetEnumName(newName);
            EditorUtility.SetDirty(_scriptableEnum);
        }

        /// <summary>
        /// Creates a new enum script
        /// </summary>
        public async void CreateEnum()
        {
            var dirPath = EditorUtility.OpenFolderPanel("Choose Enum Script Destination", "Assets", "");
            if (string.IsNullOrEmpty(dirPath)) return;

            try
            {
                var path = await _fileService.CreateEnumScriptAsync(_scriptableEnum.EnumName, dirPath);
                _scriptableEnum.enumScriptPath = path;
                RefreshState();
                EditorUtility.SetDirty(_scriptableEnum);
            }
            catch (Exception ex)
            {
                _errorMessage = $"Failed to create enum: {ex.Message}";
            }
        }

        /// <summary>
        /// Loads an existing enum for editing
        /// </summary>
        public async void LoadEnum()
        {
            if (_currentType == null) return;

            try
            {
                var path = await _fileService.FindEnumScriptAsync(_currentType);
                if (string.IsNullOrEmpty(path))
                {
                    _errorMessage = "Could not find enum script file";
                    return;
                }

                _scriptableEnum.enumScriptPath = path;
                _scriptableEnum.editState = ScriptableEnum.EditState.Loaded;
                
                _currentState?.OnExit();
                _currentState = _states[ScriptableEnum.EditState.Loaded];
                _currentState.OnEnter();

                EditorUtility.SetDirty(_scriptableEnum);
            }
            catch (Exception ex)
            {
                _errorMessage = $"Failed to load enum: {ex.Message}";
            }
        }

        /// <summary>
        /// Saves the current enum changes
        /// </summary>
        public async void SaveEnum()
        {
            if (!ValidateCurrentElements()) return;

            try
            {
                var enumScript = GenerateEnumScript();
                var success = await _fileService.SaveEnumScriptAsync(_scriptableEnum.enumScriptPath, enumScript);
                
                if (success)
                {
                    _hasUnsavedChanges = false;
                    _errorMessage = null;
                    EditorUtility.SetDirty(_scriptableEnum);
                }
                else
                {
                    _errorMessage = "Failed to save enum script";
                }
            }
            catch (Exception ex)
            {
                _errorMessage = $"Failed to save enum: {ex.Message}";
            }
        }

        /// <summary>
        /// Stops editing the current enum
        /// </summary>
        public void StopEditing()
        {
            if (_hasUnsavedChanges)
            {
                if (!EditorUtility.DisplayDialog("Unsaved Changes", 
                    "You have unsaved changes. Are you sure you want to stop editing?", 
                    "Yes", "No")) return;
            }

            _scriptableEnum.editState = ScriptableEnum.EditState.Preview;
            _currentState?.OnExit();
            _currentState = _states[ScriptableEnum.EditState.Preview];
            _currentState.OnEnter();
            
            _hasUnsavedChanges = false;
            RefreshState();
        }

        /// <summary>
        /// Validates the current enum elements
        /// </summary>
        public bool ValidateCurrentElements()
        {
            var result = _validator.ValidateElements(_scriptableEnum.Items);
            if (!result.IsValid)
            {
                _errorMessage = string.Join("\n", result.Errors.Select(e => e.Message));
                return false;
            }

            _errorMessage = null;
            _hasUnsavedChanges = true;
            return true;
        }

        /// <summary>
        /// Tries to preview an enum based on the current enum name
        /// </summary>
        private async Task<bool> TryPreviewEnum()
        {
            var type = await FindExistingEnumType();
            if (type == null) return false;

            _currentType = type;

            // If we found the type, update the namespace to match the found type
            if (type.Namespace != null && _scriptableEnum.UseCustomNamespace)
            {
                _scriptableEnum.SetNamespace(type.Namespace);
            }

            var enumValues = Enum.GetValues(type);
            var items = new ScriptableEnumItem[enumValues.Length];
        
            for (int i = 0; i < enumValues.Length; i++)
            {
                var value = enumValues.GetValue(i);
                items[i] = new ScriptableEnumItem(
                    value.ToString(), 
                    Convert.ToInt32(value)
                );
            }

            _scriptableEnum.SetItems(items);
            return true;
        }

        public void DrawNamespaceField()
        {
            EditorGUI.BeginChangeCheck();

            // Use custom namespace toggle
            EditorGUILayout.PropertyField(_useCustomNamespaceProperty, new GUIContent("Use Custom Namespace"));

            // Namespace field
            using (new EditorGUI.DisabledGroupScope(!_scriptableEnum.UseCustomNamespace))
            {
                var rect = EditorGUILayout.GetControlRect();
                var label = new GUIContent("Namespace", "The namespace for the enum. Leave empty for global namespace.");
            
                if (!_scriptableEnum.UseCustomNamespace)
                {
                    var defaultNs = EditorSettings.projectGenerationRootNamespace;
                    EditorGUI.TextField(rect, label, string.IsNullOrEmpty(defaultNs) ? "(Global Namespace)" : defaultNs);
                }
                else
                {
                    EditorGUI.PropertyField(rect, _namespaceProperty, label);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_scriptableEnum);
            }
        }
        
        private async Task<Type> FindExistingEnumType()
        {
            var enumName = _scriptableEnum.EnumName;
            if (string.IsNullOrEmpty(enumName)) return null;

            var fullName = _scriptableEnum.GetEffectiveNamespace() is string ns 
                ? $"{ns}.{enumName}" 
                : enumName;

            // Check cache first
            if (_typeCache.TryGetValue(fullName, out var cachedType))
            {
                return cachedType;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        
            var foundType = await Task.Run(() =>
            {
                foreach (var assembly in assemblies)
                {
                    // Try full name first
                    var type = assembly.GetType(fullName);
                    if (type?.IsEnum == true) return type;

                    // Fall back to searching by simple name if not found
                    if (_scriptableEnum.UseCustomNamespace) continue;
            
                    type = assembly.GetTypes().FirstOrDefault(t => t.Name == enumName && t.IsEnum);
                    if (type != null) return type;
                }

                return null;
            });

            // Cache the result (even if null)
            _typeCache[fullName] = foundType;
        
            return foundType;
        }

        // Add method to clear cache if needed
        private static void ClearTypeCache()
        {
            _typeCache.Clear();
        }

        /// <summary>
        /// Generates the enum script content
        /// </summary>
        private string GenerateEnumScript()
        {
            var effectiveNamespace = _scriptableEnum.GetEffectiveNamespace();
            var hasNamespace = !string.IsNullOrEmpty(effectiveNamespace);

            var script = new StringBuilder();
        
            if (hasNamespace)
            {
                script.AppendLine($"namespace {effectiveNamespace}");
                script.AppendLine("{");
            }

            script.AppendLine($"    public enum {_scriptableEnum.EnumName}");
            script.AppendLine("    {");

            for (int i = 0; i < _scriptableEnum.Items.Length; i++)
            {
                var item = _scriptableEnum.Items[i];
                var needsComma = i < _scriptableEnum.Items.Length - 1;
                script.AppendLine($"        {item.Name} = {item.Value}{(needsComma ? "," : "")}");
            }

            script.AppendLine("    }");
        
            if (hasNamespace)
            {
                script.AppendLine("}");
            }

            return script.ToString();
        }
    }
}