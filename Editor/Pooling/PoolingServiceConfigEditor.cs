using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Nexus.Pooling
{
    [CustomEditor(typeof(PoolingServiceConfig))]
    public class PoolingServiceConfigEditor : Editor
    {
        private bool showIdGenerator = false;
        private string containerClassName = "PoolIds";
        private string enumName = "Pools";
        private string namespaceName = "";
        private Object[] selectedConfigs;
        private SerializedProperty configurationsProperty;
        private bool showPoolList = true;
        private Vector2 scrollPosition;
        private Dictionary<string, bool> poolFoldouts = new Dictionary<string, bool>();

        private void OnEnable()
        {
            configurationsProperty = serializedObject.FindProperty("poolConfigurations");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Pool List Section
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            showPoolList = EditorGUILayout.Foldout(showPoolList, "Pool Configurations", true);
            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                AddNewPool();
            }
            EditorGUILayout.EndHorizontal();

            if (showPoolList)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                DrawPoolList();
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(10);

            // ID Generator Section
            DrawIdGeneratorSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPoolList()
        {
            for (int i = 0; i < configurationsProperty.arraySize; i++)
            {
                var poolProperty = configurationsProperty.GetArrayElementAtIndex(i);
                var idProperty = poolProperty.FindPropertyRelative("id");
                var prefabProperty = poolProperty.FindPropertyRelative("prefab");

                string displayText = string.IsNullOrEmpty(idProperty.stringValue)
                    ? "(No ID)"
                    : idProperty.stringValue;

                if (!poolFoldouts.ContainsKey(idProperty.stringValue))
                {
                    poolFoldouts[idProperty.stringValue] = false;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Main foldout row with delete button
                EditorGUILayout.BeginHorizontal();
                poolFoldouts[idProperty.stringValue] = EditorGUILayout.Foldout(
                    poolFoldouts[idProperty.stringValue],
                    displayText,
                    true
                );

                // Delete button
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("Delete Pool",
                            $"Are you sure you want to delete '{displayText}'?",
                            "Delete", "Cancel"))
                    {
                        configurationsProperty.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                EditorGUILayout.EndHorizontal();

                // Show detailed properties if expanded
                if (poolFoldouts[idProperty.stringValue])
                {
                    EditorGUI.indentLevel++;
                    DrawPoolProperties(poolProperty, idProperty, prefabProperty);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
        }

        private void DrawPoolProperties(SerializedProperty poolProperty, SerializedProperty idProperty, SerializedProperty prefabProperty)
        {
            EditorGUILayout.PropertyField(idProperty);
            EditorGUILayout.PropertyField(prefabProperty);

            if (prefabProperty.objectReferenceValue != null && string.IsNullOrEmpty(idProperty.stringValue))
            {
                // Auto-generate ID from prefab name if empty
                var prefab = prefabProperty.objectReferenceValue as GameObject;
                idProperty.stringValue = SanitizePoolId(prefab.name);
            }

            EditorGUILayout.PropertyField(poolProperty.FindPropertyRelative("initialSize"));
            EditorGUILayout.PropertyField(poolProperty.FindPropertyRelative("maxSize"));
            EditorGUILayout.PropertyField(poolProperty.FindPropertyRelative("autoExpand"));
            EditorGUILayout.PropertyField(poolProperty.FindPropertyRelative("recycleTimeout"));

            // Validation
            if (prefabProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Prefab is required", MessageType.Error);
            }
            if (string.IsNullOrEmpty(idProperty.stringValue))
            {
                EditorGUILayout.HelpBox("Pool ID is required", MessageType.Error);
            }
        }

        private void AddNewPool()
        {
            configurationsProperty.InsertArrayElementAtIndex(configurationsProperty.arraySize);
            var newElement = configurationsProperty.GetArrayElementAtIndex(configurationsProperty.arraySize - 1);

            // Set default values
            newElement.FindPropertyRelative("id").stringValue = "";
            newElement.FindPropertyRelative("prefab").objectReferenceValue = null;
            newElement.FindPropertyRelative("initialSize").intValue = 10;
            newElement.FindPropertyRelative("maxSize").intValue = 100;
            newElement.FindPropertyRelative("autoExpand").boolValue = true;
            newElement.FindPropertyRelative("recycleTimeout").floatValue = -1f;

            serializedObject.ApplyModifiedProperties();
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

                // Additional Configs Section
                EditorGUILayout.LabelField("Additional Configs", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                var newConfig = EditorGUILayout.ObjectField(
                    "Add Config",
                    null,
                    typeof(PoolingServiceConfig),
                    false
                ) as PoolingServiceConfig;

                if (newConfig != null && newConfig != target)
                {
                    var configList = (selectedConfigs ?? new Object[0]).ToList();
                    if (!configList.Contains(newConfig))
                    {
                        configList.Add(newConfig);
                        selectedConfigs = configList.ToArray();
                    }
                }

                if (selectedConfigs != null && selectedConfigs.Length > 0)
                {
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < selectedConfigs.Length; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(selectedConfigs[i], typeof(PoolingServiceConfig), false);
                        EditorGUI.EndDisabledGroup();

                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            var list = selectedConfigs.ToList();
                            list.RemoveAt(i);
                            selectedConfigs = list.ToArray();
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

                EditorGUI.indentLevel--;
            }
        }

        private void GenerateCode()
        {
            var allConfigs = new List<PoolingServiceConfig> { (PoolingServiceConfig)target };
            if (selectedConfigs != null)
            {
                allConfigs.AddRange(selectedConfigs.Cast<PoolingServiceConfig>());
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

            // Generate enum
            builder.AppendLine($"    public enum {enumName}");
            builder.AppendLine("    {");

            var allPools = allConfigs.SelectMany(config => config.Configurations)
                .OrderBy(pool => pool.id);

            foreach (var pool in allPools)
            {
                var enumValue = SanitizeEnumValue(pool.id);
                builder.AppendLine($"        {enumValue},");
            }

            builder.AppendLine("    }");
            builder.AppendLine();

            // Generate IDs class
            builder.AppendLine($"    public static class {containerClassName}");
            builder.AppendLine("    {");

            foreach (var pool in allPools)
            {
                var constName = SanitizeConstName(pool.id);
                builder.AppendLine($"        public const string {constName} = \"{pool.id}\";");
            }

            builder.AppendLine();

            // Add helper method to get prefab name
            builder.AppendLine("        private static readonly Dictionary<string, string> _prefabNames = new Dictionary<string, string>");
            builder.AppendLine("        {");
            foreach (var pool in allPools)
            {
                if (pool.prefab != null)
                {
                    builder.AppendLine($"            {{ \"{pool.id}\", \"{pool.prefab.name}\" }},");
                }
            }
            builder.AppendLine("        };");
            builder.AppendLine();

            builder.AppendLine("        public static string GetPrefabName(string poolId)");
            builder.AppendLine("        {");
            builder.AppendLine("            return _prefabNames.TryGetValue(poolId, out var name) ? name : null;");
            builder.AppendLine("        }");

            builder.AppendLine("    }");
            builder.AppendLine();

            // Generate converter class
            builder.AppendLine($"    public static class PoolIdConverter");
            builder.AppendLine("    {");
            builder.AppendLine($"        private static readonly Dictionary<{enumName}, string> _poolIdCache = new Dictionary<{enumName}, string>();");
            builder.AppendLine();
            builder.AppendLine("        static PoolIdConverter()");
            builder.AppendLine("        {");
            builder.AppendLine("            InitializeCache();");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine($"        public static string GetId({enumName} pool)");
            builder.AppendLine("        {");
            builder.AppendLine("            return _poolIdCache.GetValueOrDefault(pool);");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        private static void InitializeCache()");
            builder.AppendLine("        {");
            builder.AppendLine($"            var fields = typeof({containerClassName}).GetFields();");
            builder.AppendLine("            var constants = fields.ToDictionary(");
            builder.AppendLine("                f => f.Name,");
            builder.AppendLine("                f => (string)f.GetValue(null)");
            builder.AppendLine("            );");
            builder.AppendLine();
            builder.AppendLine($"            foreach ({enumName} pool in System.Enum.GetValues(typeof({enumName})))");
            builder.AppendLine("            {");
            builder.AppendLine("                string enumName = pool.ToString();");
            builder.AppendLine("                if (constants.TryGetValue(enumName, out string id))");
            builder.AppendLine("                {");
            builder.AppendLine("                    _poolIdCache[pool] = id;");
            builder.AppendLine("                }");
            builder.AppendLine("            }");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        public static void RefreshCache()");
            builder.AppendLine("        {");
            builder.AppendLine("            _poolIdCache.Clear();");
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

        private string SanitizePoolId(string name)
        {
            return name.Replace(" ", "").Replace("-", "_");
        }

        private string SanitizeEnumValue(string id)
        {
            return id.Replace(" ", "_").Replace("-", "_");
        }

        private string SanitizeConstName(string name)
        {
            return name.Replace(" ", "_").Replace("-", "_").ToUpperInvariant();
        }
    }
}