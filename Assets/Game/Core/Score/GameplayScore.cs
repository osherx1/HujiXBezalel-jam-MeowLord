namespace Game.Core.Score
{
    public class GameplayScore
    {
        private int _score;
        public int Score => _score;

        // Simple event for UI or systems to subscribe to
        public event System.Action<int> OnScoreChanged;

        public GameplayScore()
        {
            _score = 0;
        }

        public void ResetScore()
        {
            _score = 0;
            OnScoreChanged?.Invoke(_score);
        }

        public void AddScore(int amount)
        {
            _score += amount;
            if (_score < 0)
                _score = 0;
            OnScoreChanged?.Invoke(_score);
        }
        
    }
}