using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Nexus.Extensions;

namespace Nexus.Audio
{
    [CustomEditor(typeof(SoundLibrary))]
    public class SoundLibraryEditor : UnityEditor.Editor
    {
        private bool showIdGenerator = false;
        private string containerClassName = "SoundIds";
        private string enumName = "Sounds";
        private string namespaceName = "";
        private Object[] selectedLibraries;
        private SerializedProperty soundsProperty;
        private bool showSoundList = true;
        private Vector2 scrollPosition;
        private readonly Dictionary<string, bool> soundFoldouts = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> pitchSettingsFoldouts = new Dictionary<string, bool>();


        private void OnEnable()
        {
            soundsProperty = serializedObject.FindProperty("sounds");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Sound List Section
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            showSoundList = EditorGUILayout.Foldout(showSoundList, "Sound Entries", true);
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                AddNewSound();
            }

            EditorGUILayout.EndHorizontal();

            if (showSoundList)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                DrawSoundList();
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(10);

            // ID Generator Section
            DrawIdGeneratorSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSoundList()
        {
            for (int i = 0; i < soundsProperty.arraySize; i++)
            {
                var soundProperty = soundsProperty.GetArrayElementAtIndex(i);
                var idProperty = soundProperty.FindPropertyRelative("id");
                var displayNameProperty = soundProperty.FindPropertyRelative("displayName");
                var clipProperty = soundProperty.FindPropertyRelative("clip");

                string displayText = string.IsNullOrEmpty(displayNameProperty.stringValue)
                    ? "(No Name)"
                    : displayNameProperty.stringValue;

                // Ensure the sound has an ID in the foldout dictionaries
                if (!soundFoldouts.ContainsKey(idProperty.stringValue))
                {
                    soundFoldouts[idProperty.stringValue] = false;
                    pitchSettingsFoldouts[idProperty.stringValue] = false;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Main foldout row with delete button
                EditorGUILayout.BeginHorizontal();
                soundFoldouts[idProperty.stringValue] = EditorGUILayout.Foldout(
                    soundFoldouts[idProperty.stringValue],
                    displayText,
                    true
                );

                // Delete button
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("Delete Sound",
                            $"Are you sure you want to delete '{displayText}'?",
                            "Delete", "Cancel"))
                    {
                        soundsProperty.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                EditorGUILayout.EndHorizontal();

                // Show detailed properties if expanded
                if (soundFoldouts[idProperty.stringValue])
                {
                    EditorGUI.indentLevel++;

                    // Basic Properties Section
                    DrawBasicProperties(soundProperty, displayNameProperty, clipProperty, idProperty);

                    EditorGUILayout.Space(5);

                    // Pitch Settings Section
                    DrawPitchSettings(soundProperty, idProperty.stringValue);

                    EditorGUILayout.Space(5);

                    // Spatialization Settings Section
                    DrawSpatializationSettings(soundProperty);

                    EditorGUILayout.Space(5);

                    // Tags Section
                    EditorGUILayout.PropertyField(soundProperty.FindPropertyRelative("tags"));

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
        }

        private void DrawBasicProperties(SerializedProperty soundProperty, SerializedProperty displayNameProperty,
            SerializedProperty clipProperty, SerializedProperty idProperty)
        {
            // Display name field
            EditorGUILayout.PropertyField(displayNameProperty);

            // Audio clip field with preview options
            EditorGUILayout.PropertyField(clipProperty);
            var clip = clipProperty.objectReferenceValue as AudioClip;
            if (clip != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Preview", EditorStyles.miniButton))
                {
                    AudioPreview.PlayClip(clip);
                }

                if (GUILayout.Button("Stop", EditorStyles.miniButton))
                {
                    AudioPreview.StopAllClips();
                }

                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }

            // ID field (read-only)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(idProperty);
            EditorGUI.EndDisabledGroup();

            // Sound type field
            EditorGUILayout.PropertyField(soundProperty.FindPropertyRelative("type"));

            // Volume field
            EditorGUILayout.PropertyField(soundProperty.FindPropertyRelative("defaultVolume"));
        }

        private void DrawPitchSettings(SerializedProperty soundProperty, string id)
        {
            if (!pitchSettingsFoldouts.ContainsKey(id))
            {
                pitchSettingsFoldouts[id] = false;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            pitchSettingsFoldouts[id] = EditorGUILayout.Foldout(
                pitchSettingsFoldouts[id],
                "Pitch Settings",
                true
            );

            // Quick toggle for randomize pitch
            var randomizePitchProp = soundProperty.FindPropertyRelative("randomizePitch");
            EditorGUILayout.PropertyField(randomizePitchProp, GUIContent.none, GUILayout.Width(30));

            EditorGUILayout.EndHorizontal();

            if (pitchSettingsFoldouts[id])
            {
                EditorGUI.indentLevel++;

                if (randomizePitchProp.boolValue)
                {
                    var pitchMinProp = soundProperty.FindPropertyRelative("pitchMin");
                    var pitchMaxProp = soundProperty.FindPropertyRelative("pitchMax");

                    // Draw min/max pitch as a range slider
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Pitch Range");

                    float minPitch = pitchMinProp.floatValue;
                    float maxPitch = pitchMaxProp.floatValue;

                    EditorGUILayout.MinMaxSlider(ref minPitch, ref maxPitch, -3f, 3f);

                    // Also show float fields for precise control
                    minPitch = EditorGUILayout.FloatField(minPitch, GUILayout.Width(50));
                    maxPitch = EditorGUILayout.FloatField(maxPitch, GUILayout.Width(50));

                    pitchMinProp.floatValue = Mathf.Clamp(minPitch, -3f, maxPitch);
                    pitchMaxProp.floatValue = Mathf.Clamp(maxPitch, minPitch, 3f);

                    EditorGUILayout.EndHorizontal();

                    // Preview buttons for pitch range
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUIUtility.labelWidth);
                    if (GUILayout.Button("Preview Min", EditorStyles.miniButton))
                    {
                        var clip = soundProperty.FindPropertyRelative("clip").objectReferenceValue as AudioClip;
                        if (clip != null) AudioPreview.PlayClip(clip, pitchMinProp.floatValue);
                    }

                    if (GUILayout.Button("Preview Max", EditorStyles.miniButton))
                    {
                        var clip = soundProperty.FindPropertyRelative("clip").objectReferenceValue as AudioClip;
                        if (clip != null) AudioPreview.PlayClip(clip, pitchMaxProp.floatValue);
                    }

                    if (GUILayout.Button("Stop", EditorStyles.miniButton))
                    {
                        AudioPreview.StopAllClips();
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("Enable randomize pitch to set pitch variation range.", MessageType.Info);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSpatializationSettings(SerializedProperty soundProperty)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var spatializeProp = soundProperty.FindPropertyRelative("spatialize");
            EditorGUILayout.PropertyField(spatializeProp);

            if (spatializeProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(soundProperty.FindPropertyRelative("maxDistance"));
                EditorGUILayout.PropertyField(soundProperty.FindPropertyRelative("minDistance"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private static class AudioPreview
        {
            private static AudioSource previewSource;

            private static void EnsurePreviewSource()
            {
                if (previewSource == null)
                {
                    var go = new GameObject("Audio Preview");
                    previewSource = go.AddComponent<AudioSource>();
                    go.hideFlags = HideFlags.HideAndDontSave;
                }
            }

            public static void PlayClip(AudioClip clip, float pitch = 1f)
            {
                EnsurePreviewSource();
                previewSource.pitch = pitch;
                previewSource.clip = clip;
                previewSource.Play();
            }

            public static void StopAllClips()
            {
                if (previewSource != null)
                {
                    previewSource.Stop();
                }
            }
        }


        private void AddNewSound()
        {
            soundsProperty.InsertArrayElementAtIndex(soundsProperty.arraySize);
            var newElement = soundsProperty.GetArrayElementAtIndex(soundsProperty.arraySize - 1);

            // Set default values
            newElement.FindPropertyRelative("id").stringValue = System.Guid.NewGuid().ToString();
            newElement.FindPropertyRelative("defaultVolume").floatValue = 1f;
            newElement.FindPropertyRelative("spatialize").boolValue = false;
            newElement.FindPropertyRelative("maxDistance").floatValue = 100f;
            newElement.FindPropertyRelative("minDistance").floatValue = 1f;

            // Ensure the tags list is initialized
            var tagsProperty = newElement.FindPropertyRelative("tags");
            tagsProperty.ClearArray();

            serializedObject.ApplyModifiedProperties();
        }


        private string SanitizeClassName(string name)
        {
            // Remove "Library" suffix if present
            name = name.Replace("Library", "");

            // Replace invalid characters with underscore
            return new string(name
                    .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                    .ToArray())
                .TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
        }

        private string SanitizeConstName(string name)
        {
            return name
                .Replace(" ", "_")
                .Replace("-", "_")
                .ToUpperInvariant();
        }

        private string GetExistingEnumFile(string enumName)
        {
            var guids = AssetDatabase.FindAssets($"t:Script {enumName}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var content = System.IO.File.ReadAllText(path);
                if (content.Contains($"public enum {enumName}"))
                {
                    return content;
                }
            }

            return null;
        }

        private Dictionary<string, int> GetExistingEnumValues(string existingFile, string enumName)
        {
            var values = new Dictionary<string, int>();
            if (string.IsNullOrEmpty(existingFile)) return values;

            var enumRegex = new System.Text.RegularExpressions.Regex(
                $@"public\s+enum\s+{enumName}\s*\{{([^}}]*)\}}",
                System.Text.RegularExpressions.RegexOptions.Singleline
            );

            var match = enumRegex.Match(existingFile);
            if (!match.Success) return values;

            var valueRegex = new System.Text.RegularExpressions.Regex(@"(\w+)\s*=\s*(\d+)");
            var valueMatches = valueRegex.Matches(match.Groups[1].Value);

            foreach (System.Text.RegularExpressions.Match valueMatch in valueMatches)
            {
                values[valueMatch.Groups[1].Value] = int.Parse(valueMatch.Groups[2].Value);
            }

            return values;
        }

        private void GenerateSoundIdsClass()
        {
            var allLibraries = new List<SoundLibrary> { (SoundLibrary)target };
            if (selectedLibraries != null)
            {
                allLibraries.AddRange(selectedLibraries.Cast<SoundLibrary>());
            }

            var builder = new StringBuilder();

            // Add using directives
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using System.Linq;");

            // Add namespace if specified
            if (!string.IsNullOrEmpty(namespaceName))
            {
                builder.AppendLine($"namespace {namespaceName}");
                builder.AppendLine("{");
            }

            // Get existing enum values to preserve ordering
            var existingFile = GetExistingEnumFile(enumName);
            var existingValues = GetExistingEnumValues(existingFile, enumName);

            // Generate enum
            builder.AppendLine($"    public enum {enumName}");
            builder.AppendLine("    {");

            var allSounds = allLibraries.SelectMany(lib => lib.Sounds)
                .OrderBy(sound => sound.Type)
                .ThenBy(sound => sound.DisplayName)
                .Select(sound => (sound.Type, sound.DisplayName))
                .Distinct()
                .ToList();

            // Start with highest existing value or 0
            int nextValue = existingValues.Count > 0 ? existingValues.Values.Max() + 1 : 0;

            // Add spacing between different sound types
            SoundType? currentType = null;

            // First add existing values in their original order
            foreach (var (type, displayName) in allSounds)
            {
                if (currentType != type && currentType != null)
                {
                    builder.AppendLine();
                }

                currentType = type;

                var enumValue = SanitizeConstName($"{type}_{displayName}");
                if (existingValues.TryGetValue(enumValue, out int value))
                {
                    builder.AppendLine($"        {enumValue} = {value},");
                }
            }

            // Then add new values
            currentType = null;
            foreach (var (type, displayName) in allSounds)
            {
                if (currentType != type && currentType != null)
                {
                    builder.AppendLine();
                }

                currentType = type;

                var enumValue = SanitizeConstName($"{type}_{displayName}");
                if (!existingValues.ContainsKey(enumValue))
                {
                    builder.AppendLine($"        {enumValue} = {nextValue++},");
                }
            }

            builder.AppendLine("    }");
            builder.AppendLine();

            // Generate IDs class grouped by SoundType
            builder.AppendLine($"    public static class {containerClassName}");
            builder.AppendLine("    {");

            // Group sounds by type
            var soundsByType = allLibraries.SelectMany(lib => lib.Sounds)
                .GroupBy(sound => sound.Type)
                .OrderBy(g => g.Key);

            foreach (var group in soundsByType)
            {
                builder.AppendLine($"        public static class {group.Key}");
                builder.AppendLine("        {");

                foreach (var sound in group.OrderBy(s => s.DisplayName))
                {
                    var constName = SanitizeConstName(sound.DisplayName);
                    builder.AppendLine($"            public const string {constName} = \"{sound.Id}\";");
                }

                builder.AppendLine("        }");
                builder.AppendLine();
            }

            builder.AppendLine("    }");
            builder.AppendLine();

            // Generate converter class
            builder.AppendLine("    public static class SoundIdConverter");
            builder.AppendLine("    {");
            builder.AppendLine(
                $"        private static readonly Dictionary<{enumName}, string> _soundIdCache = new Dictionary<{enumName}, string>();");
            builder.AppendLine();
            builder.AppendLine("        static SoundIdConverter()");
            builder.AppendLine("        {");
            builder.AppendLine("            InitializeCache();");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine($"        public static string GetId({enumName} sound)");
            builder.AppendLine("        {");
            builder.AppendLine("            return _soundIdCache.GetValueOrDefault(sound);");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        private static void InitializeCache()");
            builder.AppendLine("        {");
            builder.AppendLine("            var constants = new Dictionary<string, string>();");
            builder.AppendLine();
            builder.AppendLine($"            // Get nested type classes (UI, SFX, Environment)");
            builder.AppendLine($"            var typeClasses = typeof({containerClassName}).GetNestedTypes();");
            builder.AppendLine($"            foreach (var typeClass in typeClasses)");
            builder.AppendLine("            {");
            builder.AppendLine("                // Get the SoundType name from the class name");
            builder.AppendLine("                var typeName = typeClass.Name.ToUpperInvariant();");
            builder.AppendLine();
            builder.AppendLine("                // Get all constant fields in this type class");
            builder.AppendLine(
                "                var fields = typeClass.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy)");
            builder.AppendLine("                    .Where(fi => fi.IsLiteral && !fi.IsInitOnly);");
            builder.AppendLine();
            builder.AppendLine("                // Add constants with type prefix to match enum names");
            builder.AppendLine("                foreach (var field in fields)");
            builder.AppendLine("                {");
            builder.AppendLine(
                "                    constants[$\"{typeName}_{field.Name}\"] = (string)field.GetValue(null);");
            builder.AppendLine("                }");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine($"            foreach ({enumName} sound in System.Enum.GetValues(typeof({enumName})))");
            builder.AppendLine("            {");
            builder.AppendLine("                string enumName = sound.ToString();");
            builder.AppendLine("                if (constants.TryGetValue(enumName, out string id))");
            builder.AppendLine("                {");
            builder.AppendLine("                    _soundIdCache[sound] = id;");
            builder.AppendLine("                }");
            builder.AppendLine("            }");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        public static void RefreshCache()");
            builder.AppendLine("        {");
            builder.AppendLine("            _soundIdCache.Clear();");
            builder.AppendLine("            InitializeCache();");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("#if UNITY_EDITOR");
            builder.AppendLine("        [UnityEditor.InitializeOnLoadMethod]");
            builder.AppendLine("        private static void InitializeOnLoad()");
            builder.AppendLine("        {");
            builder.AppendLine("            RefreshCache();");
            builder.AppendLine("        }");
            builder.AppendLine("#endif");
            builder.AppendLine("    }");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                builder.AppendLine("}");
            }

            var path = EditorUtility.SaveFilePanel(
                "Save Generated Code",
                "Assets",
                $"{containerClassName}.cs",
                "cs"
            );

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, builder.ToString());
                AssetDatabase.Refresh();
                Debug.Log($"Generated code at: {path}");
            }
        }

        private void DrawIdGeneratorSection()
        {
            showIdGenerator = EditorGUILayout.Foldout(showIdGenerator, "Code Generator");
            if (showIdGenerator)
            {
                EditorGUI.indentLevel++;

                // Container class settings
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                containerClassName = EditorGUILayout.TextField("IDs Class Name", containerClassName);
                enumName = EditorGUILayout.TextField("Enum Name", enumName);
                namespaceName = EditorGUILayout.TextField("Namespace", namespaceName);
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(5);

                // Additional Libraries Section
                EditorGUILayout.LabelField("Additional Libraries", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                var newLibrary = EditorGUILayout.ObjectField(
                    "Add Library",
                    null,
                    typeof(SoundLibrary),
                    false
                ) as SoundLibrary;

                if (newLibrary != null && newLibrary != target)
                {
                    var libraryList = (selectedLibraries ?? new Object[0]).ToList();
                    if (!libraryList.Contains(newLibrary))
                    {
                        libraryList.Add(newLibrary);
                        selectedLibraries = libraryList.ToArray();
                    }
                }

                if (selectedLibraries != null && selectedLibraries.Length > 0)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < selectedLibraries.Length; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(selectedLibraries[i], typeof(SoundLibrary), false);
                        EditorGUI.EndDisabledGroup();

                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            var list = selectedLibraries.ToList();
                            list.RemoveAt(i);
                            selectedLibraries = list.ToArray();
                            GUIUtility.ExitGUI();
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(5);

                using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(containerClassName) ||
                                                        string.IsNullOrEmpty(enumName)))
                {
                    if (GUILayout.Button("Generate Code"))
                    {
                        GenerateSoundIdsClass();
                    }
                }

                if (string.IsNullOrEmpty(containerClassName) || string.IsNullOrEmpty(enumName))
                {
                    EditorGUILayout.HelpBox(
                        "Please enter both class name and enum name.",
                        MessageType.Warning
                    );
                }

                EditorGUI.indentLevel--;
            }
        }

        private void PreviewGeneratedCode()
        {
            var allLibraries = new List<SoundLibrary> { (SoundLibrary)target };
            if (selectedLibraries != null)
            {
                allLibraries.AddRange(selectedLibraries.Cast<SoundLibrary>());
            }

            var preview = new StringBuilder();

            // Add namespace if specified
            if (!string.IsNullOrEmpty(namespaceName))
            {
                preview.AppendLine($"namespace {namespaceName}");
                preview.AppendLine("{");
            }

            // Begin container class
            preview.AppendLine($"    public static class {containerClassName}");
            preview.AppendLine("    {");

            // Add example entries from each library
            foreach (var library in allLibraries)
            {
                var libraryClassName = SanitizeClassName(library.name);
                preview.AppendLine($"        public static class {libraryClassName}");
                preview.AppendLine("        {");

                // Show first few sounds as examples
                var previewSounds = library.Sounds.Take(3);
                foreach (var sound in previewSounds)
                {
                    var constName = SanitizeConstName(sound.DisplayName);
                    preview.AppendLine($"            public const string {constName} = \"{sound.Id}\";");
                }

                if (library.Sounds.Count > 3)
                {
                    preview.AppendLine("            // ... additional sound IDs ...");
                }

                preview.AppendLine("        }");
                preview.AppendLine();
            }

            preview.AppendLine("    }");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                preview.AppendLine("}");
            }

            // Create preview window
            EditorUtility.DisplayDialog(
                "Generated Code Preview",
                preview.ToString(),
                "OK"
            );
        }
    }
}