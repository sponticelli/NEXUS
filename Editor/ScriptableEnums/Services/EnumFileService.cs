using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using System.Linq;

namespace Nexus.ScriptableEnums.Services
{
    /// <summary>
    /// Default implementation of IEnumFileService
    /// </summary>
    public class EnumFileService : IEnumFileService
    {
        private readonly EnumRegex _enumRegex;

        public EnumFileService()
        {
            _enumRegex = new EnumRegex();
        }

        public async Task<string> FindEnumScriptAsync(Type type)
        {
            if (type == null) return null;

            var guids = AssetDatabase.FindAssets("t:MonoScript");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scriptText = await File.ReadAllTextAsync(path);

                if (_enumRegex.Match(type, scriptText))
                {
                    return path;
                }
            }

            return null;
        }

        public async Task<bool> SaveEnumScriptAsync(string path, string content)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            try
            {
                await File.WriteAllTextAsync(path, content);
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string> CreateEnumScriptAsync(string enumName, string directory)
        {
            var path = Path.Combine(directory, $"{enumName}.cs");
            var content = GenerateEnumScript(enumName);

            await File.WriteAllTextAsync(path, content);
            AssetDatabase.Refresh();

            return path;
        }

        public bool ValidateScript(string path, Type enumType)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            var scriptText = File.ReadAllText(path);
            return _enumRegex.Match(enumType, scriptText);
        }

        private string GenerateEnumScript(string enumName)
        {
            var nspace = EditorSettings.projectGenerationRootNamespace;
            var hasNamespace = !string.IsNullOrEmpty(nspace);

            return $@"{(hasNamespace ? $"namespace {nspace}\n{{" : "")}
    public enum {enumName}
    {{
    }}
{(hasNamespace ? "}" : "")}";
        }
    }
}