namespace Nexus.ScriptableEnums.Services
{
    /// <summary>
    /// Interface for enum validation operations
    /// </summary>
    public interface IEnumValidator
    {
        ValidationResult ValidateElements(ScriptableEnumItem[] elements);
        ValidationResult ValidateElement(ScriptableEnumItem element, ScriptableEnumItem[] otherElements);
    }
}