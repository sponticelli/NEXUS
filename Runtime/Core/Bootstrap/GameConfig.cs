using UnityEngine;

namespace Nexus.Core.Bootstrap
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Nexus/Game Configuration")]
    public class GameConfig : ScriptableObject, IGameConfig
    {
        [SerializeField] private string gameName = "Nexus Game";
        [SerializeField] private GameMode gameMode = GameMode.Normal;
        [SerializeField] private bool isDebugMode = false;

        public string GameName => gameName;
        public GameMode CurrentGameMode => gameMode;
        public bool IsDebugMode => isDebugMode;
    }
}