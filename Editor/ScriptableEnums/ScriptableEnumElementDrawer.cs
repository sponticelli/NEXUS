using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Nexus.ScriptableEnums
{
    [CustomPropertyDrawer(typeof(ScriptableEnumItem))]
    public class ScriptableEnumElementDrawer : PropertyDrawer
    {
        private const float ErrorIconWidth = 16f;
        private const float LabelWidth = 50f;
        private const float Spacing = 5f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Calculate rects
            var nameRect = new Rect(
                position.x,
                position.y,
                (position.width - Spacing) * 0.6f - ErrorIconWidth,
                position.height
            );

            var valueLabelRect = new Rect(
                nameRect.xMax + Spacing + ErrorIconWidth,
                position.y,
                LabelWidth,
                position.height
            );

            var valueRect = new Rect(
                valueLabelRect.xMax + Spacing,
                position.y,
                position.width - nameRect.width - valueLabelRect.width - Spacing * 3 - ErrorIconWidth,
                position.height
            );

            var nameIconRect = new Rect(
                nameRect.xMax,
                position.y,
                ErrorIconWidth,
                position.height
            );

            var valueIconRect = new Rect(
                valueRect.xMax,
                position.y,
                ErrorIconWidth,
                position.height
            );

            // Get the ScriptableEnumItem to check for errors
            var enumItem = (ScriptableEnumItem)property.boxedValue;
            var hasNameError = enumItem.errors != null && enumItem.errors.Length > 0 &&
                               enumItem.errors.Any(e => e.Type == ValidationErrorType.NameCollision ||
                                                        e.Type == ValidationErrorType.InvalidName);
            var hasValueError = enumItem.errors != null && enumItem.errors.Length > 0 &&
                                enumItem.errors.Any(e => e.Type == ValidationErrorType.ValueCollision ||
                                                         e.Type == ValidationErrorType.InvalidValue);

            // Draw name field with error highlighting
            var defaultColor = GUI.color;
            if (hasNameError)
            {
                GUI.color = new Color(1f, 0.7f, 0.7f); // Light red for better visibility
            }

            EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("name"), GUIContent.none);
            if (hasNameError)
            {
                var errorContent =
                    EditorGUIUtility.TrIconContent("d_console.erroricon.sml", GetErrorTooltip(enumItem, true));
                GUI.Label(nameIconRect, errorContent);
            }

            GUI.color = defaultColor;

            // Draw "Value" label
            EditorGUI.LabelField(valueLabelRect, "Value");

            // Draw value field with error highlighting
            if (hasValueError)
            {
                GUI.color = new Color(1f, 0.7f, 0.7f);
            }

            EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("value"), GUIContent.none);
            if (hasValueError)
            {
                var errorContent =
                    EditorGUIUtility.TrIconContent("d_console.erroricon.sml", GetErrorTooltip(enumItem, false));
                GUI.Label(valueIconRect, errorContent);
            }

            GUI.color = defaultColor;

            EditorGUI.EndProperty();
        }

        private string GetErrorTooltip(ScriptableEnumItem item, bool nameErrors)
        {
            if (item.errors == null || item.errors.Length == 0)
                return string.Empty;

            var tooltipBuilder = new StringBuilder();
            foreach (var error in item.errors)
            {
                bool isNameError = error.Type == ValidationErrorType.NameCollision ||
                                   error.Type == ValidationErrorType.InvalidName;

                if (nameErrors == isNameError)
                {
                    if (tooltipBuilder.Length > 0)
                        tooltipBuilder.AppendLine();
                    tooltipBuilder.Append(error.Message);
                }
            }

            return tooltipBuilder.ToString();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}