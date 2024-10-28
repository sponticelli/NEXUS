using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Nexus.ScriptableEnums.Services
{
    /// <summary>
    /// Default implementation of IEnumValidator
    /// </summary>
    public class EnumValidator : IEnumValidator
    {
        private static readonly Regex IdentifierRegex = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$");

        public ValidationResult ValidateElements(ScriptableEnumItem[] elements)
        {
            var errors = new List<ValidationError>();

            for (int i = 0; i < elements.Length; i++)
            {
                var result = ValidateElement(elements[i], elements);
                if (!result.IsValid)
                {
                    errors.AddRange(result.Errors);
                }
            }

            return new ValidationResult(errors.Count == 0, errors);
        }

        public ValidationResult ValidateElement(ScriptableEnumItem element, ScriptableEnumItem[] otherElements)
        {
            var errors = new List<ValidationError>();

            // Validate name format
            if (!IdentifierRegex.IsMatch(element.Name))
            {
                errors.Add(new ValidationError(
                    $"Invalid identifier format: {element.Name}",
                    ValidationErrorType.InvalidName));
            }

            // Check for duplicates
            foreach (var other in otherElements)
            {
                if (other.Name == element.Name && other.Value != element.Value)
                {
                    errors.Add(new ValidationError(
                        $"Duplicate name: {element.Name}",
                        ValidationErrorType.NameCollision));
                }

                if (other.Value == element.Value && other.Name != element.Name)
                {
                    errors.Add(new ValidationError(
                        $"Duplicate value: {element.Value}",
                        ValidationErrorType.ValueCollision));
                }
            }

            return new ValidationResult(errors.Count == 0, errors);
        }
    }
}