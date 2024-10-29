using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Nexus.Core.Services;

namespace Nexus.Core.Bootstrap
{
    [CustomPropertyDrawer(typeof(ServiceDefinition))]
    public class ServiceDefinitionDrawer : PropertyDrawer
    {
        // Cache for calculated property heights
        private static readonly Dictionary<int, float> HeightCache = new Dictionary<int, float>();
        
        // Cached property names as const fields
        private const string SERVICE_NAME_PROP = "serviceName";
        private const string INTERFACE_TYPE_PROP = "interfaceType";
        private const string IMPLEMENTATION_TYPE_PROP = "implementationType";
        private const string LIFETIME_PROP = "lifetime";
        private const string MONO_BEHAVIOUR_PREFAB_PROP = "monoBehaviourPrefab";
        private const string CONFIGURATION_PROP = "configuration";

        private static readonly float PropertySpacing = 2f;
        private static readonly float LineHeight = EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var implementationProp = property.FindPropertyRelative(IMPLEMENTATION_TYPE_PROP);
            bool isMonoBehaviour = IsMonoBehaviour(implementationProp);

            DrawProperties(position, property, isMonoBehaviour);

            EditorGUI.EndProperty();
        }

        private void DrawProperties(Rect position, SerializedProperty property, bool isMonoBehaviour)
        {
            var rect = new Rect(position.x, position.y, position.width, LineHeight);

            // Always draw these properties
            DrawProperty(ref rect, property, SERVICE_NAME_PROP);
            DrawProperty(ref rect, property, INTERFACE_TYPE_PROP);
            DrawProperty(ref rect, property, IMPLEMENTATION_TYPE_PROP);
            DrawProperty(ref rect, property, LIFETIME_PROP);

            // Draw MonoBehaviourPrefab if it's a MonoBehaviour
            if (isMonoBehaviour)
            {
                DrawProperty(ref rect, property, MONO_BEHAVIOUR_PREFAB_PROP);
            }

            // Always draw Configuration last
            DrawProperty(ref rect, property, CONFIGURATION_PROP);
        }

        private void DrawProperty(ref Rect rect, SerializedProperty property, string propertyName)
        {
            var prop = property.FindPropertyRelative(propertyName);
            if (prop != null)
            {
                string label = GetDisplayLabel(propertyName);
                EditorGUI.PropertyField(rect, prop, new GUIContent(label));
                rect.y += LineHeight + PropertySpacing;
            }
        }

        private string GetDisplayLabel(string propertyName)
        {
            // Convert property names to display labels
            return propertyName switch
            {
                SERVICE_NAME_PROP => "Service Name",
                INTERFACE_TYPE_PROP => "Interface Type",
                IMPLEMENTATION_TYPE_PROP => "Implementation Type",
                LIFETIME_PROP => "Lifetime",
                MONO_BEHAVIOUR_PREFAB_PROP => "Prefab",
                CONFIGURATION_PROP => "Configuration",
                _ => ObjectNames.NicifyVariableName(propertyName)
            };
        }

        private bool IsMonoBehaviour(SerializedProperty implementationTypeProp)
        {
            if (implementationTypeProp == null) return false;
            
            var typeNameProp = implementationTypeProp.FindPropertyRelative("typeName");
            if (typeNameProp == null) return false;
            
            var typeRef = typeNameProp.stringValue;
            if (string.IsNullOrEmpty(typeRef)) return false;

            var type = Type.GetType(typeRef);
            return type != null && typeof(MonoBehaviour).IsAssignableFrom(type);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Use instance ID as cache key
            int instanceId = property.serializedObject.targetObject.GetInstanceID();
            
            // Always calculate height for array elements as they can have different states
            if (property.propertyPath.Contains("Array.data["))
            {
                return CalculateHeight(property);
            }

            if (HeightCache.TryGetValue(instanceId, out float cachedHeight))
            {
                return cachedHeight;
            }

            float height = CalculateHeight(property);
            HeightCache[instanceId] = height;
            return height;
        }

        private float CalculateHeight(SerializedProperty property)
        {
            // Base properties (always visible)
            int lineCount = 4; // serviceName, interfaceType, implementationType, lifetime

            // Check if MonoBehaviourPrefab should be shown
            var implementationProp = property.FindPropertyRelative(IMPLEMENTATION_TYPE_PROP);
            if (IsMonoBehaviour(implementationProp))
            {
                lineCount++;
            }

            // Configuration is always shown
            lineCount++;

            return (LineHeight + PropertySpacing) * lineCount;
        }

        [InitializeOnLoadMethod]
        private static void ClearServiceDefinitionCache()
        {
            HeightCache.Clear();
        }
    }
}