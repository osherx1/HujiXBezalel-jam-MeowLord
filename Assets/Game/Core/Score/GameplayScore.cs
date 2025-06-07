using Game.Core.Managers;

namespace Game.Core.Score
{
    public class GameplayScore
    {
        private int _score;
        public int Score => _score;
        public GameplayScoreCombinator ScoreCombinator { get; }

        public GameplayScore()
        {
            _score = 0;
            GameEvents.OnGameStarted += ResetScore;
            ScoreCombinator = new GameplayScoreCombinator(this);
        }

        public void ResetScore()
        {
            _score = 0;
            GameEvents.UpdateScore(_score);
        }

        public void AddScore(int amount)
        {
            _score += amount;
            if (_score < 0)
                _score = 0;
            GameEvents.UpdateScore(_score);
        }
    }
}