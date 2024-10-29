using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nexus.Core.Services;
using UnityEditor;
using UnityEngine;

namespace Nexus.Core.Bootstrap
{
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