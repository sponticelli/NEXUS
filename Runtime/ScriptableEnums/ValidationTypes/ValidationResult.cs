using System.Collections.Generic;
using System.Linq;

namespace Nexus.ScriptableEnums
{
    /// <summary>
    /// Represents the result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; }
        public IReadOnlyList<ValidationError> Errors { get; }

        public ValidationResult(bool isValid, IEnumerable<ValidationError> errors = null)
        {
            IsValid = isValid;
            Errors = errors?.ToList() ?? new List<ValidationError>();
        }

        public static ValidationResult Success => new ValidationResult(true);
        public static ValidationResult Failure(params ValidationError[] errors) => 
            new ValidationResult(false, errors);
    }
}