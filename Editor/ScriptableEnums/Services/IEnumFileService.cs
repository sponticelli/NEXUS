using System;
using System.Threading.Tasks;

namespace Nexus.ScriptableEnums.Services
{
    /// <summary>
    /// Interface for handling enum file operations
    /// </summary>
    public interface IEnumFileService
    {
        Task<string> FindEnumScriptAsync(Type type);
        Task<bool> SaveEnumScriptAsync(string path, string content);
        Task<string> CreateEnumScriptAsync(string enumName, string directory);
        bool ValidateScript(string path, Type enumType);
    }
}