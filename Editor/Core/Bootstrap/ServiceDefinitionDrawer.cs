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
        
        // Cached property names to avoid string allocations
        private static readonly string ServiceNamePropName = "serviceName";
        private static readonly string InterfaceTypePropName = "interfaceType";
        private static readonly string ImplementationTypePropName = "implementationType";
        private static readonly string LifetimePropName = "lifetime";
        private static readonly string MonoBehaviourPrefabPropName = "monoBehaviourPrefab";
        private static readonly string ConfigurationPropName = "configuration";

        private static readonly float PropertySpacing = 2f;
        private static readonly float LineHeight = EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var rect = new Rect(position.x, position.y, position.width, LineHeight);
            DrawServiceProperties(rect, property);

            EditorGUI.EndProperty();
        }

        private void DrawServiceProperties(Rect rect, SerializedProperty property)
        {
            // Use cached property names
            DrawProperty(ref rect, property, ServiceNamePropName);
            DrawProperty(ref rect, property, InterfaceTypePropName);
            DrawProperty(ref rect, property, ImplementationTypePropName);
            DrawProperty(ref rect, property, LifetimePropName);

            var implementationProp = property.FindPropertyRelative(ImplementationTypePropName);
            if (IsMonoBehaviour(implementationProp))
            {
                DrawProperty(ref rect, property, MonoBehaviourPrefabPropName);
            }

            DrawProperty(ref rect, property, ConfigurationPropName);
        }

        private void DrawProperty(ref Rect rect, SerializedProperty property, string propertyName)
        {
            var prop = property.FindPropertyRelative(propertyName);
            if (prop != null)
            {
                EditorGUI.PropertyField(rect, prop);
                rect.y += LineHeight + PropertySpacing;
            }
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
            if (HeightCache.TryGetValue(instanceId, out float cachedHeight))
            {
                return cachedHeight;
            }

            int lineCount = 5; // Base number of lines
            
            var implementationProp = property.FindPropertyRelative(ImplementationTypePropName);
            if (IsMonoBehaviour(implementationProp))
            {
                lineCount++;
            }

            float totalHeight = (LineHeight + PropertySpacing) * lineCount;
            HeightCache[instanceId] = totalHeight;
            
            return totalHeight;
        }

        [InitializeOnLoadMethod]
        private static void ClearServiceDefinitionCache()
        {
            HeightCache.Clear();
        }
    }

    [CustomPropertyDrawer(typeof(TypeReference))]
    public class TypeReferencePropertyDrawer : PropertyDrawer
    {
        // Static cache for type lists
        private static readonly Dictionary<Type, TypeCache> TypeLists = new Dictionary<Type, TypeCache>();
        
        private class TypeCache
        {
            public List<Type> Types;
            public string[] DisplayNames;
            public string[] AssemblyQualifiedNames;
            public DateTime LastUpdate;
        }

        // Cache timeout (5 seconds)
        private const float CacheTimeout = 5f;

        private static TypeCache GetOrCreateTypeCache(Type attributeType)
        {
            if (TypeLists.TryGetValue(attributeType, out var cache))
            {
                // Check if cache is still valid
                if ((DateTime.Now - cache.LastUpdate).TotalSeconds < CacheTimeout)
                {
                    return cache;
                }
            }

            // Create new cache
            cache = new TypeCache
            {
                Types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => 
                    {
                        try 
                        {
                            return a.GetTypes();
                        }
                        catch (ReflectionTypeLoadException)
                        {
                            return Array.Empty<Type>();
                        }
                    })
                    .Where(t => t != null && t.GetCustomAttribute(attributeType) != null)
                    .OrderBy(t => t.FullName)
                    .ToList(),
                LastUpdate = DateTime.Now
            };

            cache.DisplayNames = cache.Types.Select(t => t.FullName).ToArray();
            cache.AssemblyQualifiedNames = cache.Types.Select(t => t.AssemblyQualifiedName).ToArray();

            TypeLists[attributeType] = cache;
            return cache;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attributeType = DetermineAttributeType();
            if (attributeType == null)
            {
                EditorGUI.LabelField(position, label.text, "Unsupported field");
                return;
            }

            var cache = GetOrCreateTypeCache(attributeType);
            if (cache.Types.Count == 0)
            {
                EditorGUI.LabelField(position, label.text, "No types found");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var typeNameProp = property.FindPropertyRelative("typeName");
            var currentTypeName = typeNameProp.stringValue;
            int selectedIndex = Array.FindIndex(cache.AssemblyQualifiedNames, t => t == currentTypeName);
            if (selectedIndex < 0) selectedIndex = 0;

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, cache.DisplayNames);
            if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < cache.Types.Count)
            {
                typeNameProp.stringValue = cache.AssemblyQualifiedNames[newIndex];
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
        }

        private Type DetermineAttributeType()
        {
            var fieldName = fieldInfo?.Name;
            if (string.IsNullOrEmpty(fieldName)) return null;

            return fieldName switch
            {
                "interfaceType" => typeof(ServiceInterfaceAttribute),
                "implementationType" => typeof(ServiceImplementationAttribute),
                _ => null
            };
        }

        [InitializeOnLoadMethod]
        private static void ClearTypeReferenceCache()
        {
            TypeLists.Clear();
        }
    }
}