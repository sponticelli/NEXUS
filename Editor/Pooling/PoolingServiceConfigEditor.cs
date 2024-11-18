using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Nexus.Extensions;

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

        private void DrawPoolProperties(SerializedProperty poolProperty, SerializedProperty idProperty,
            SerializedProperty prefabProperty)
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

        

        private string SanitizePoolId(string name)
        {
            return name.Replace(" ", "").Replace("-", "_");
        }
        
    }
}