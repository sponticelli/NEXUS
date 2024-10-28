namespace Nexus.ScriptableEnums
{
    /// <summary>
    /// Represents a single validation error
    /// </summary>
    public class ValidationError
    {
        public string Message { get; }
        public ValidationErrorType Type { get; }
        public ValidationErrorSeverity Severity { get; }

        public ValidationError(string message, ValidationErrorType type, 
            ValidationErrorSeverity severity = ValidationErrorSeverity.Error)
        {
            Message = message;
            Type = type;
            Severity = severity;
        }
    }
}