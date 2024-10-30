using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Nexus.Extensions;

namespace Nexus.Audio
{
    [CustomEditor(typeof(MusicPlaylist))]
    public class MusicPlaylistEditor : UnityEditor.Editor
    {
        private bool showIdGenerator = false;
        private string containerClassName = "PlaylistIds";
        private string namespaceName = "";
        private Object[] selectedPlaylists;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(10);
            
            showIdGenerator = EditorGUILayout.Foldout(showIdGenerator, "Track IDs Class Generator");
            if (showIdGenerator)
            {
                EditorGUI.indentLevel++;
                
                containerClassName = EditorGUILayout.TextField("Container Class Name", containerClassName);
                namespaceName = EditorGUILayout.TextField("Namespace", namespaceName);

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Additional Playlists", EditorStyles.boldLabel);
                
                selectedPlaylists = EditorGUILayout.ObjectField("Add Playlist", null, typeof(MusicPlaylist), false)
                    .Yield()
                    .Concat(selectedPlaylists ?? new Object[0])
                    .Where(x => x != null)
                    .Distinct()
                    .ToArray();

                if (selectedPlaylists != null && selectedPlaylists.Length > 0)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < selectedPlaylists.Length; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(selectedPlaylists[i], typeof(MusicPlaylist), false);
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            var list = selectedPlaylists.ToList();
                            list.RemoveAt(i);
                            selectedPlaylists = list.ToArray();
                            GUIUtility.ExitGUI();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }

                if (GUILayout.Button("Generate Track IDs Class"))
                {
                    GenerateTrackIdsClass();
                }
                
                EditorGUI.indentLevel--;
            }
        }

        private void GenerateTrackIdsClass()
        {
            var allPlaylists = new List<MusicPlaylist> { (MusicPlaylist)target };
            if (selectedPlaylists != null)
            {
                allPlaylists.AddRange(selectedPlaylists.Cast<MusicPlaylist>());
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

            // Generate nested class for each playlist
            foreach (var playlist in allPlaylists)
            {
                var playlistClassName = SanitizeClassName(playlist.name);
                builder.AppendLine($"        public static class {playlistClassName}");
                builder.AppendLine("        {");

                // Add const fields for track IDs
                foreach (var track in playlist.Tracks)
                {
                    var constName = SanitizeConstName(track.DisplayName);
                    builder.AppendLine($"            public const string {constName} = \"{track.Id}\";");
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
                "Save Track IDs Class",
                "Assets",
                $"{containerClassName}.cs",
                "cs"
            );

            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }

                File.WriteAllText(path, builder.ToString());
                AssetDatabase.Refresh();
                
                Debug.Log($"Generated Track IDs class at: {path}");
            }
        }

        private string SanitizeClassName(string name)
        {
            // Remove "Playlist" suffix if present
            name = name.Replace("Playlist", "");
            
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
    }
}