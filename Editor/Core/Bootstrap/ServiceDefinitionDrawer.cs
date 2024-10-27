using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Nexus.Core.Services;
using UnityEngine;

namespace Nexus.Core.Bootstrap
{
    [CustomPropertyDrawer(typeof(ServiceDefinition))]
    public class ServiceDefinitionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var serviceNameProp = property.FindPropertyRelative("serviceName");
            var interfaceTypeProp = property.FindPropertyRelative("interfaceType");
            var implementationTypeProp = property.FindPropertyRelative("implementationType");
            var lifetimeProp = property.FindPropertyRelative("lifetime");
            var monoBehaviourPrefabProp = property.FindPropertyRelative("monoBehaviourPrefab");
            var configurationProp = property.FindPropertyRelative("configuration");

            EditorGUI.BeginProperty(position, label, property);

            Rect rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(rect, serviceNameProp);
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            EditorGUI.PropertyField(rect, interfaceTypeProp);
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            EditorGUI.PropertyField(rect, implementationTypeProp);
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            EditorGUI.PropertyField(rect, lifetimeProp);
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            if (IsMonoBehaviour(implementationTypeProp))
            {
                EditorGUI.PropertyField(rect, monoBehaviourPrefabProp);
                rect.y += EditorGUIUtility.singleLineHeight + 2;
            }

            EditorGUI.PropertyField(rect, configurationProp);
            rect.y += EditorGUIUtility.singleLineHeight + 2;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lineCount = 5;

            var implementationTypeProp = property.FindPropertyRelative("implementationType");
            if (IsMonoBehaviour(implementationTypeProp))
            {
                lineCount++; // For monoBehaviourPrefab
            }

            return (EditorGUIUtility.singleLineHeight + 2) * lineCount;
        }

        private bool IsMonoBehaviour(SerializedProperty implementationTypeProp)
        {
            Type implementationType = GetTypeFromSerializedProperty(implementationTypeProp);
            return implementationType != null && typeof(MonoBehaviour).IsAssignableFrom(implementationType);
        }

        private Type GetTypeFromSerializedProperty(SerializedProperty typeReferenceProperty)
        {
            var typeNameProperty = typeReferenceProperty.FindPropertyRelative("typeName");
            var typeName = typeNameProperty.stringValue;
            return Type.GetType(typeName);
        }
    }
}