using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Nexus.Audio
{
    [CustomEditor(typeof(MusicPlaylist))]
    public class MusicPlaylistEditor : UnityEditor.Editor
    {
        private bool showIdGenerator = false;
        private string containerClassName = "PlaylistIds";
        private string enumName = "MusicTracks";
        private string namespaceName = "";
        private Object[] selectedPlaylists;
        private bool showInheritedTracks;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(10);

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

                // Additional Playlists Section
                EditorGUILayout.LabelField("Additional Playlists", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                var newPlaylist = EditorGUILayout.ObjectField(
                    "Add Playlist",
                    null,
                    typeof(MusicPlaylist),
                    false
                ) as MusicPlaylist;

                if (newPlaylist != null && newPlaylist != target)
                {
                    var playlists = (selectedPlaylists ?? new Object[0]).ToList();
                    if (!playlists.Contains(newPlaylist))
                    {
                        playlists.Add(newPlaylist);
                        selectedPlaylists = playlists.ToArray();
                    }
                }

                if (selectedPlaylists != null && selectedPlaylists.Length > 0)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < selectedPlaylists.Length; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(selectedPlaylists[i], typeof(MusicPlaylist), false);
                        EditorGUI.EndDisabledGroup();

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

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(5);

                using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(containerClassName) ||
                                                        string.IsNullOrEmpty(enumName)))
                {
                    if (GUILayout.Button("Generate Code"))
                    {
                        GenerateCode();
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

        private void GenerateCode()
        {
            var allPlaylists = new List<MusicPlaylist> { (MusicPlaylist)target };
            if (selectedPlaylists != null)
            {
                allPlaylists.AddRange(selectedPlaylists.Cast<MusicPlaylist>());
            }

            var builder = new StringBuilder();

            // Add using directives
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using System.Linq;");
            builder.AppendLine();

            // Add namespace if specified
            if (!string.IsNullOrEmpty(namespaceName))
            {
                builder.AppendLine($"namespace {namespaceName}");
                builder.AppendLine("{");
            }

            // Get existing enum values to preserve ordering
            var existingFile = GetExistingEnumFile(enumName);
            var existingValues = GetExistingEnumValues(existingFile, enumName);

            // Generate enum with playlist prefixes
            builder.AppendLine($"    public enum {enumName}");
            builder.AppendLine("    {");

            var allTracks = allPlaylists.SelectMany(p => p.Tracks)
                .OrderBy(t => t.DisplayName)
                .ToList();

            // Start with highest existing value or 0
            int nextValue = existingValues.Count > 0 ? existingValues.Values.Max() + 1 : 0;

            // First add existing values with their original values
            foreach (var track in allTracks)
            {
                var playlistPrefix = SanitizeClassName(track.Id).ToUpperInvariant();
                var enumValue = $"{playlistPrefix}_{SanitizeConstName(track.DisplayName)}";

                if (existingValues.TryGetValue(enumValue, out int value))
                {
                    builder.AppendLine($"        {enumValue} = {value},");
                }
            }

            // Then add new values
            foreach (var track in allTracks)
            {
                var playlistPrefix = SanitizeClassName(track.Id).ToUpperInvariant();
                var enumValue = $"{playlistPrefix}_{SanitizeConstName(track.DisplayName)}";

                if (!existingValues.ContainsKey(enumValue))
                {
                    builder.AppendLine($"        {enumValue} = {nextValue++},");
                }
            }

            builder.AppendLine("    }");
            builder.AppendLine();

            // Generate IDs class
            builder.AppendLine($"    public static class {containerClassName}");
            builder.AppendLine("    {");

            // Group tracks by playlist
            foreach (var playlist in allPlaylists.OrderBy(p => p.name))
            {
                var playlistClassName = SanitizeClassName(playlist.name);
                builder.AppendLine($"        public static class {playlistClassName}");
                builder.AppendLine("        {");

                foreach (var track in playlist.Tracks.OrderBy(t => t.DisplayName))
                {
                    var constName = SanitizeConstName(track.DisplayName);
                    builder.AppendLine($"            public const string {constName} = \"{track.Id}\";");
                }

                builder.AppendLine("        }");
                builder.AppendLine();
            }

            builder.AppendLine("    }");
            builder.AppendLine();

            // Generate converter class
            builder.AppendLine("    public static class MusicTrackConverter");
            builder.AppendLine("    {");
            builder.AppendLine(
                $"        private static readonly Dictionary<{enumName}, string> _trackIdCache = new Dictionary<{enumName}, string>();");
            builder.AppendLine(
                $"        private static readonly Dictionary<string, {enumName}> _reverseCache = new Dictionary<string, {enumName}>();");
            builder.AppendLine();
            builder.AppendLine("        static MusicTrackConverter()");
            builder.AppendLine("        {");
            builder.AppendLine("            InitializeCache();");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine($"        public static string GetId({enumName} track)");
            builder.AppendLine("        {");
            builder.AppendLine("            return _trackIdCache.GetValueOrDefault(track);");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine($"        public static {enumName}? GetTrack(string trackId)");
            builder.AppendLine("        {");
            builder.AppendLine("            return _reverseCache.GetValueOrDefault(trackId, default);");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine($"        public static IEnumerable<{enumName}> GetPlaylistTracks(string playlistName)");
            builder.AppendLine("        {");
            builder.AppendLine("            var prefix = playlistName.Replace(\"Playlist\", \"\").ToUpperInvariant();");
            builder.AppendLine($"            return System.Enum.GetValues(typeof({enumName}))");
            builder.AppendLine($"                .Cast<{enumName}>()");
            builder.AppendLine("                .Where(t => t.ToString().StartsWith(prefix + \"_\"));");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        private static void InitializeCache()");
            builder.AppendLine("        {");
            builder.AppendLine("            var constants = new Dictionary<string, string>();");
            builder.AppendLine();
            builder.AppendLine($"            // Get nested type classes (playlists)");
            builder.AppendLine($"            var playlistClasses = typeof({containerClassName}).GetNestedTypes();");
            builder.AppendLine($"            foreach (var playlistClass in playlistClasses)");
            builder.AppendLine("            {");
            builder.AppendLine("                var playlistName = playlistClass.Name.ToUpperInvariant();");
            builder.AppendLine();
            builder.AppendLine("                // Get all constant fields in this playlist class");
            builder.AppendLine(
                "                var fields = playlistClass.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy)");
            builder.AppendLine("                    .Where(fi => fi.IsLiteral && !fi.IsInitOnly);");
            builder.AppendLine();
            builder.AppendLine("                foreach (var field in fields)");
            builder.AppendLine("                {");
            builder.AppendLine(
                "                    constants[$\"{playlistName}_{field.Name}\"] = (string)field.GetValue(null);");
            builder.AppendLine("                }");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine($"            foreach ({enumName} track in System.Enum.GetValues(typeof({enumName})))");
            builder.AppendLine("            {");
            builder.AppendLine("                string enumName = track.ToString();");
            builder.AppendLine("                if (constants.TryGetValue(enumName, out string id))");
            builder.AppendLine("                {");
            builder.AppendLine("                    _trackIdCache[track] = id;");
            builder.AppendLine("                    _reverseCache[id] = track;");
            builder.AppendLine("                }");
            builder.AppendLine("            }");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        public static void RefreshCache()");
            builder.AppendLine("        {");
            builder.AppendLine("            _trackIdCache.Clear();");
            builder.AppendLine("            _reverseCache.Clear();");
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