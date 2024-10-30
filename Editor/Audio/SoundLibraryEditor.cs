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

        private void GenerateSoundIdsClass()
        {
            var allLibraries = new List<SoundLibrary> { (SoundLibrary)target };
            if (selectedLibraries != null)
            {
                allLibraries.AddRange(selectedLibraries.Cast<SoundLibrary>());
            }

            var builder = new StringBuilder();

            if (!string.IsNullOrEmpty(namespaceName))
            {
                builder.AppendLine($"namespace {namespaceName}");
                builder.AppendLine("{");
            }

            // Begin container class
            builder.AppendLine($"    public static class {containerClassName}");
            builder.AppendLine("    {");

            // Generate nested class for each library
            foreach (var library in allLibraries)
            {
                var libraryClassName = SanitizeClassName(library.name);
                builder.AppendLine($"        public static class {libraryClassName}");
                builder.AppendLine("        {");

                // Add const fields for sound IDs
                foreach (var sound in library.Sounds)
                {
                    var constName = SanitizeConstName(sound.DisplayName);
                    builder.AppendLine($"            public const string {constName} = \"{sound.Id}\";");
                }

                builder.AppendLine("        }");
                builder.AppendLine();
            }

            builder.AppendLine("    }");

            if (!string.IsNullOrEmpty(namespaceName))
            {
                builder.AppendLine("}");
            }

            var path = EditorUtility.SaveFilePanel(
                "Save Sound IDs Class",
                "Assets",
                $"{containerClassName}.cs",
                "cs"
            );

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, builder.ToString());
                AssetDatabase.Refresh();

                Debug.Log($"Generated Sound IDs class at: {path}");
            }
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

        private void DrawIdGeneratorSection()
        {
            showIdGenerator = EditorGUILayout.Foldout(showIdGenerator, "Sound IDs Class Generator");
            if (showIdGenerator)
            {
                EditorGUI.indentLevel++;

                // Container class settings
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                containerClassName = EditorGUILayout.TextField("Container Class Name", containerClassName);
                namespaceName = EditorGUILayout.TextField("Namespace", namespaceName);
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(5);

                // Additional Libraries Section
                EditorGUILayout.LabelField("Additional Libraries", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Library selection field
                var newLibrary = EditorGUILayout.ObjectField(
                    "Add Library",
                    null,
                    typeof(SoundLibrary),
                    false
                ) as SoundLibrary;

                if (newLibrary != null)
                {
                    // Convert to list for modification
                    var libraryList = (selectedLibraries ?? new Object[0]).ToList();

                    // Add if not already included
                    if (!libraryList.Contains(newLibrary))
                    {
                        libraryList.Add(newLibrary);
                        selectedLibraries = libraryList.ToArray();
                    }
                }

                // Display selected libraries
                if (selectedLibraries != null && selectedLibraries.Length > 0)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < selectedLibraries.Length; i++)
                    {
                        EditorGUILayout.BeginHorizontal();

                        // Display library field
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(
                            selectedLibraries[i],
                            typeof(SoundLibrary),
                            false
                        );
                        EditorGUI.EndDisabledGroup();

                        // Remove button
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

                // Generation button and preview
                using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(containerClassName)))
                {
                    if (GUILayout.Button("Generate Sound IDs Class"))
                    {
                        GenerateSoundIdsClass();
                    }

                    // Optional: Show preview of the class structure
                    if (GUILayout.Button("Preview Generated Code"))
                    {
                        PreviewGeneratedCode();
                    }
                }

                // Warning messages
                if (string.IsNullOrEmpty(containerClassName))
                {
                    EditorGUILayout.HelpBox(
                        "Please enter a container class name.",
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