using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace Nexus.Core.Bootstrap.Editor
{
    [CustomEditor(typeof(ServiceRegistryAsset))]
    public class ServiceRegistryAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty parentRegistryProperty;
        private SerializedProperty servicesProperty;
        private bool showServiceList = true;
        private Vector2 scrollPosition;
        private bool showInheritedServices;
        private Dictionary<int, bool> serviceFoldouts = new Dictionary<int, bool>();
        private GUIStyle headerStyle;
        private GUIContent addServiceIcon;
        private GUIContent removeServiceIcon;
        private GUIContent moveUpIcon;
        private GUIContent moveDownIcon;
        private GUIContent inheritedIcon;

        private void OnEnable()
        {
            parentRegistryProperty = serializedObject.FindProperty("parentRegistry");
            servicesProperty = serializedObject.FindProperty("services");
            InitializeGUIStyles();
        }

        private void InitializeGUIStyles()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 10)
            };

            addServiceIcon = EditorGUIUtility.IconContent("d_Toolbar Plus", "Add new service");
            removeServiceIcon = EditorGUIUtility.IconContent("d_Toolbar Minus", "Remove service");
            moveUpIcon = EditorGUIUtility.IconContent("d_scrollup@2x", "Move up");
            moveDownIcon = EditorGUIUtility.IconContent("d_scrolldown@2x", "Move down");
            inheritedIcon = EditorGUIUtility.IconContent("d_FilterByLabel", "Inherited from parent registry");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawRegistryHeader();
            EditorGUILayout.Space(5);
            DrawParentRegistryField();
            EditorGUILayout.Space(5);
            DrawServiceList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRegistryHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Service Registry", headerStyle);

            var registry = (ServiceRegistryAsset)target;
            var services = registry.GetServices();
            EditorGUILayout.LabelField($"({services.Count} services)", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawParentRegistryField()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(parentRegistryProperty);

            var parentRegistry = (ServiceRegistryAsset)parentRegistryProperty.objectReferenceValue;
            if (parentRegistry != null)
            {
                EditorGUILayout.BeginHorizontal();
                showInheritedServices = EditorGUILayout.Toggle("Show Inherited Services", showInheritedServices);
                if (GUILayout.Button("Select Parent", GUILayout.Width(100)))
                {
                    Selection.activeObject = parentRegistry;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawServiceList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Service list header
            EditorGUILayout.BeginHorizontal();
            showServiceList = EditorGUILayout.Foldout(showServiceList, "Services", true);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(addServiceIcon, EditorStyles.miniButton, GUILayout.Width(24)))
            {
                AddNewService();
            }

            EditorGUILayout.EndHorizontal();

            if (showServiceList)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                // Draw inherited services if requested
                if (showInheritedServices && parentRegistryProperty.objectReferenceValue != null)
                {
                    DrawInheritedServices();
                }

                // Draw local services
                for (int i = 0; i < servicesProperty.arraySize; i++)
                {
                    DrawServiceElement(i);
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawInheritedServices()
        {
            var parentRegistry = (ServiceRegistryAsset)parentRegistryProperty.objectReferenceValue;
            if (parentRegistry == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Inherited Services", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            foreach (var service in parentRegistry.GetServices())
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                EditorGUILayout.LabelField(inheritedIcon, GUILayout.Width(20));
                EditorGUILayout.LabelField(service.serviceName);
                EditorGUILayout.LabelField(service.lifetime.ToString(), EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawServiceElement(int index)
        {
            var serviceProperty = servicesProperty.GetArrayElementAtIndex(index);

            if (!serviceFoldouts.ContainsKey(index))
            {
                serviceFoldouts[index] = false;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Service header
            EditorGUILayout.BeginHorizontal();

            serviceFoldouts[index] = EditorGUILayout.Foldout(serviceFoldouts[index],
                GetServiceDisplayName(serviceProperty), true);

            GUILayout.FlexibleSpace();

            // Move up button
            if (index > 0 && GUILayout.Button(moveUpIcon, EditorStyles.miniButton, GUILayout.Width(24)))
            {
                servicesProperty.MoveArrayElement(index, index - 1);
            }

            // Move down button
            if (index < servicesProperty.arraySize - 1 &&
                GUILayout.Button(moveDownIcon, EditorStyles.miniButton, GUILayout.Width(24)))
            {
                servicesProperty.MoveArrayElement(index, index + 1);
            }

            // Remove button
            if (GUILayout.Button(removeServiceIcon, EditorStyles.miniButton, GUILayout.Width(24)))
            {
                if (EditorUtility.DisplayDialog("Remove Service",
                        "Are you sure you want to remove this service?", "Remove", "Cancel"))
                {
                    servicesProperty.DeleteArrayElementAtIndex(index);
                    serviceFoldouts.Remove(index);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
            }

            EditorGUILayout.EndHorizontal();

            // Draw service details if expanded
            if (serviceFoldouts[index])
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serviceProperty);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private string GetServiceDisplayName(SerializedProperty serviceProperty)
        {
            var nameProperty = serviceProperty.FindPropertyRelative("serviceName");
            var interfaceProperty = serviceProperty.FindPropertyRelative("interfaceType")
                .FindPropertyRelative("typeName");
            var lifetimeProperty = serviceProperty.FindPropertyRelative("lifetime");

            string name = !string.IsNullOrEmpty(nameProperty.stringValue)
                ? nameProperty.stringValue
                : "New Service";

            string interfaceType = !string.IsNullOrEmpty(interfaceProperty.stringValue)
                ? interfaceProperty.stringValue.Split(',')[0].Split('.').Last()
                : "No Interface";

            return $"{name} ({interfaceType}) - {lifetimeProperty.enumDisplayNames[lifetimeProperty.enumValueIndex]}";
        }

        private void AddNewService()
        {
            servicesProperty.InsertArrayElementAtIndex(servicesProperty.arraySize);
            var newElement = servicesProperty.GetArrayElementAtIndex(servicesProperty.arraySize - 1);

            // Reset all fields
            newElement.FindPropertyRelative("serviceName").stringValue = "";
            newElement.FindPropertyRelative("interfaceType").FindPropertyRelative("typeName").stringValue = "";
            newElement.FindPropertyRelative("implementationType").FindPropertyRelative("typeName").stringValue = "";
            newElement.FindPropertyRelative("lifetime").enumValueIndex = 0;
            newElement.FindPropertyRelative("monoBehaviourPrefab").objectReferenceValue = null;
            newElement.FindPropertyRelative("configuration").objectReferenceValue = null;

            // Expand the new service
            serviceFoldouts[servicesProperty.arraySize - 1] = true;
        }
    }
}