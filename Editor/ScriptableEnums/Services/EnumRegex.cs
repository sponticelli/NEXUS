using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Nexus.ScriptableEnums.Services
{
    /// <summary>
    /// Handles regex pattern matching and replacement for enum scripts
    /// </summary>
    public class EnumRegex
    {
        private const string ENUM_PATTERN = @"public\s+enum\s+{0}\s*\{{\s*{1}\s*\}}";
        private const string ENUM_VALUE_PATTERN = @"{0}\s*=\s*{1}";
        private const string NAMESPACE_PATTERN = @"namespace\s+([a-zA-Z_][\w.]*)\s*\{";

        /// <summary>
        /// Checks if the given text contains a matching enum definition
        /// </summary>
        /// <param name="type">The enum type to match</param>
        /// <param name="text">The text to search in</param>
        /// <returns>True if a match is found</returns>
        public bool Match(Type type, string text)
        {
            if (type == null || string.IsNullOrEmpty(text))
                return false;

            try
            {
                var valuesPattern = BuildEnumValuesPattern(type);
                var pattern = string.Format(ENUM_PATTERN, type.Name, valuesPattern);
                return Regex.IsMatch(text, pattern, RegexOptions.Multiline);
            }
            catch (ArgumentException ex)
            {
                UnityEngine.Debug.LogError($"Invalid regex pattern: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Replaces an enum definition in the given text
        /// </summary>
        /// <param name="type">The enum type to replace</param>
        /// <param name="text">The source text</param>
        /// <param name="replacement">The replacement text</param>
        /// <returns>The modified text, or null if replacement failed</returns>
        public string Replace(Type type, string text, string replacement)
        {
            if (type == null || string.IsNullOrEmpty(text))
                return null;

            try
            {
                var valuesPattern = BuildEnumValuesPattern(type);
                var pattern = string.Format(ENUM_PATTERN, type.Name, valuesPattern);
                
                // First try to replace within a namespace
                var namespaceMatch = Regex.Match(text, NAMESPACE_PATTERN);
                if (namespaceMatch.Success)
                {
                    var namespaceName = namespaceMatch.Groups[1].Value;
                    if (type.Namespace == namespaceName)
                    {
                        // Match enum within namespace
                        pattern = $@"(namespace\s+{Regex.Escape(namespaceName)}\s*{{[\s\S]*?){pattern}([\s\S]*?}})";
                        return Regex.Replace(text, pattern, $"$1{replacement}$2");
                    }
                }

                // If no namespace match or different namespace, try global replace
                return Regex.Replace(text, pattern, replacement);
            }
            catch (ArgumentException ex)
            {
                UnityEngine.Debug.LogError($"Invalid regex pattern: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Builds a pattern that matches all enum values in the correct order
        /// </summary>
        private string BuildEnumValuesPattern(Type type)
        {
            if (!type.IsEnum)
                throw new ArgumentException("Type must be an enum", nameof(type));

            var names = Enum.GetNames(type);
            var values = Enum.GetValues(type);

            var patterns = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var value = Convert.ToInt32(values.GetValue(i));
                
                // Make the = value part optional to support both explicit and implicit values
                patterns[i] = string.Format(
                    ENUM_VALUE_PATTERN, 
                    Regex.Escape(name), 
                    value
                ) + "?";
            }

            // Join patterns with optional whitespace and commas
            return string.Join(@"\s*,?\s*", patterns);
        }

        /// <summary>
        /// Extracts the namespace from a script file
        /// </summary>
        /// <param name="text">The script text</param>
        /// <returns>The namespace, or null if not found</returns>
        public string ExtractNamespace(string text)
        {
            var match = Regex.Match(text, NAMESPACE_PATTERN);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// Validates if a string is a valid C# identifier
        /// </summary>
        public static bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return Regex.IsMatch(name, @"^[a-zA-Z_]\w*$");
        }

        /// <summary>
        /// Validates if a string is a valid enum value format
        /// </summary>
        public static bool IsValidEnumValue(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return Regex.IsMatch(text, @"^[a-zA-Z_][\w]*\s*=\s*-?\d+$");
        }

        /// <summary>
        /// Validates an entire enum definition
        /// </summary>
        public static bool IsValidEnumDefinition(string text)
        {
            const string enumPattern = @"^\s*public\s+enum\s+([a-zA-Z_]\w*)\s*\{\s*(([a-zA-Z_]\w*\s*=\s*-?\d+\s*,?\s*)*)\}$";
            return Regex.IsMatch(text, enumPattern, RegexOptions.Multiline);
        }

        /// <summary>
        /// Gets the values from an enum definition string
        /// </summary>
        public static Dictionary<string, int> GetEnumValues(string enumDefinition)
        {
            var values = new Dictionary<string, int>();
            var matches = Regex.Matches(enumDefinition, @"([a-zA-Z_]\w*)\s*=\s*(-?\d+)");

            foreach (Match match in matches)
            {
                var name = match.Groups[1].Value;
                var value = int.Parse(match.Groups[2].Value);
                values[name] = value;
            }

            return values;
        }
        
        /// <summary>
        /// Finds an enum definition within a specific namespace
        /// </summary>
        public bool FindInNamespace(string text, string enumName, string namespaceName, out string matchedNamespace)
        {
            matchedNamespace = null;
        
            // If no namespace specified, check global scope
            if (string.IsNullOrEmpty(namespaceName))
            {
                var globalMatch = Regex.Match(text, $@"public\s+enum\s+{Regex.Escape(enumName)}\s*{{");
                // Ensure this match isn't inside any namespace
                if (globalMatch.Success && !IsInsideNamespace(text, globalMatch.Index))
                {
                    return true;
                }
                return false;
            }

            // Check for enum in specific namespace
            var nsPattern = $@"namespace\s+{Regex.Escape(namespaceName)}\s*{{[\s\S]*?public\s+enum\s+{Regex.Escape(enumName)}\s*{{";
            var match = Regex.Match(text, nsPattern);
        
            if (match.Success)
            {
                matchedNamespace = namespaceName;
                return true;
            }

            return false;
        }

        private bool IsInsideNamespace(string text, int position)
        {
            var nsMatches = Regex.Matches(text, @"namespace\s+([a-zA-Z_][\w.]*)\s*{");
            foreach (Match nsMatch in nsMatches)
            {
                // Find the closing brace for this namespace
                var openBraces = 1;
                var currentPos = nsMatch.Index + nsMatch.Length;
            
                while (currentPos < text.Length && openBraces > 0)
                {
                    if (text[currentPos] == '{') openBraces++;
                    if (text[currentPos] == '}') openBraces--;
                    currentPos++;
                }

                // If our position is between the namespace declaration and its closing brace
                if (position > nsMatch.Index && position < currentPos)
                {
                    return true;
                }
            }
            return false;
        }
    }
}