namespace Nexus.Core.Bootstrap
{
    /// <summary>
    /// Interface for configuration providers
    /// </summary>
    public interface IGameConfig
    {
        string GameName { get; }
        GameMode CurrentGameMode { get; }
        bool IsDebugMode { get; }
    }
}