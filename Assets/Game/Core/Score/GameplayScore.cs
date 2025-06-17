using System;
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
            GameEvents.OnTutorialStarted += ResetScore;
            GameEvents.OnTutorialReset += ResetScore;
            ScoreCombinator = new GameplayScoreCombinator(this);
        }

        private void ResetScore(Action<Action> obj)
        {
            _score = 0;
            GameEvents.UpdateScore(_score);
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