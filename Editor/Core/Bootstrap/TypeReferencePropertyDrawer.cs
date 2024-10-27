using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Nexus.Core.Services;

namespace Nexus.Core.Bootstrap
{
    [CustomPropertyDrawer(typeof(TypeReference))]
    public class TypeReferencePropertyDrawer : PropertyDrawer
    {
        private bool initialized = false;
        private List<Type> availableTypes;
        private string[] typeNames;
        private int selectedIndex = -1;

        private void Initialize(SerializedProperty property, Type attributeType)
        {
            if (initialized) return;

            // Get all types marked with the specified attribute
            availableTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttribute(attributeType) != null)
                .OrderBy(t => t.FullName)
                .ToList();

            typeNames = availableTypes.Select(t => t.FullName).ToArray();

            // Get the currently selected type
            var typeNameProperty = property.FindPropertyRelative("typeName");
            var currentTypeName = typeNameProperty.stringValue;
            selectedIndex = Array.FindIndex(typeNames, t => t == currentTypeName);

            initialized = true;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Determine whether we're selecting an interface or implementation type
            FieldInfo field = this.fieldInfo; // Use the fieldInfo provided by PropertyDrawer
            Type attributeType = null;

            if (field != null)
            {
                if (field.Name == "interfaceType")
                {
                    attributeType = typeof(ServiceInterfaceAttribute);
                }
                else if (field.Name == "implementationType")
                {
                    attributeType = typeof(ServiceImplementationAttribute);
                }
            }

            if (attributeType == null)
            {
                EditorGUI.LabelField(position, label.text, "Unsupported field");
                return;
            }

            // Initialize availableTypes and typeNames
            var availableTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttribute(attributeType) != null)
                .OrderBy(t => t.FullName)
                .ToList();

            // Use AssemblyQualifiedName for storage and comparison
            var typeNames = availableTypes.Select(t => t.AssemblyQualifiedName).ToArray();
            // Use FullName for display purposes
            var displayNames = availableTypes.Select(t => t.FullName).ToArray();

            // Get the currently selected type
            var typeNameProperty = property.FindPropertyRelative("typeName");
            var currentTypeName = typeNameProperty.stringValue;
            int selectedIndex = Array.FindIndex(typeNames, t => t == currentTypeName);
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            EditorGUI.BeginProperty(position, label, property);

            int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, displayNames);

            if (newIndex != selectedIndex)
            {
                selectedIndex = newIndex;
                var type = availableTypes[selectedIndex];
                typeNameProperty.stringValue = type.AssemblyQualifiedName;
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }
    }
}