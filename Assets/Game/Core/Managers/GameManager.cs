using Game.Core.Score;

namespace Game.Core.Managers
{
    public class GameManager
    {
        private readonly GameplayScore _gameplayScore;
        private readonly HighScoreManager _highScoreManager;

        // The single instance, lazily initialized
        private static readonly GameManager _instance = new GameManager();
        
        private static readonly string[] _randomNames = new string[]
        {
            "Pixel", "Whiskers", "Shadow", "Nova", "Milo",
            "Luna", "Ziggy", "Pepper", "Mochi", "Ninja",
            "Jinx", "Maple", "Muffin", "Rocket", "Hazel",
            "Blitz", "Mocha", "Sprout", "Olive", "Cosmo"
        };

        public string CurrentNickname { get; private set; }

        // Private constructor prevents external instantiation
        private GameManager()
        {
            _highScoreManager = new HighScoreManager();
            int idx = UnityEngine.Random.Range(0, _randomNames.Length);
            CurrentNickname = _randomNames[idx];
            _gameplayScore = new GameplayScore();
        }

        // Public property to access the single instance
        public static GameManager Instance
        {
            get
            {
                return _instance;
            }
        }
    }
}