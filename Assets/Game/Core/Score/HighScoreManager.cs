using System;
using System.Collections.Generic;
using System.Linq;
using Firebase.Database;
using System.Text;
using UnityEngine;

namespace Game.Core.Score
{
    public class HighScoreManager
    {
        private const int MaxHighScores = 10;
        private const string HighScoresPath = "highscores";

        private bool isWriteInProgress = false;
        private Action pendingFetch = null;
        private HighScoreLogger _logger;

        public HighScoreManager(HighScoreLogger logger)
        {
            _logger = logger;
        }

        // Call this when player finishes and wants to submit score
        public void TryAddHighScore(int score, string nickname)
        {
            _logger?.Log($"TryAddHighScore called with score={score}, nickname={nickname}");
            if (string.IsNullOrEmpty(nickname))
                nickname = "Player";

            // Get all scores, update/add, and write back
            GetHighScoreTable(table =>
            {
                var list = table.Select(e => new HighScoreEntry { Score = e.Item2, Nickname = e.Item3 }).ToList();
                var existing = list.Find(entry => entry.Nickname == nickname);
                if (existing != null)
                {
                    if (score > existing.Score)
                        existing.Score = score;
                    else
                        return; // No update needed
                }
                else
                {
                    list.Add(new HighScoreEntry(score, nickname));
                }
                // Sort and keep only top 10
                list = list.OrderByDescending(e => e.Score).Take(MaxHighScores).ToList();
                // Write back to Firebase
                var db = FirebaseDatabase.DefaultInstance;
                var dict = new Dictionary<string, object>();
                foreach (var entry in list)
                {
                    dict[entry.Nickname] = entry.Score;
                }
                _logger?.Log($"Writing high scores to Firebase: {string.Join(", ", list.Select(e => $"{e.Nickname}:{e.Score}"))}");
                db.RootReference.Child(HighScoresPath).SetValueAsync(dict).ContinueWith(task => {
                    isWriteInProgress = false;
                    _logger?.Log($"Write complete. Success: {task.IsCompleted && !task.IsFaulted && !task.IsCanceled}");
                    if (pendingFetch != null)
                    {
                        var fetch = pendingFetch;
                        pendingFetch = null;
                        fetch();
                    }
                });
            });
        }

        // For displaying the high scores (returns list for UI)
        public void GetHighScoreTable(Action<List<(int, int, string)>> callback)
        {
            if (isWriteInProgress)
            {
                _logger?.Log("GetHighScoreTable called while write in progress. Queuing fetch.");
                pendingFetch = () => GetHighScoreTable(callback);
                return;
            }
            var db = FirebaseDatabase.DefaultInstance;
            isWriteInProgress = true;
            db.RootReference.Child(HighScoresPath).OrderByValue().LimitToLast(MaxHighScores).GetValueAsync().ContinueWith(task =>
            {
                var result = new List<(int, int, string)>();
                if (task.IsCompleted && task.Result != null && task.Result.Exists)
                {
                    var entries = new List<HighScoreEntry>();
                    foreach (var child in task.Result.Children)
                    {
                        string name = child.Key;
                        int score = 0;
                        int.TryParse(child.Value.ToString(), out score);
                        entries.Add(new HighScoreEntry(score, name));
                    }
                    // Sort descending
                    entries = entries.OrderByDescending(e => e.Score).Take(MaxHighScores).ToList();
                    for (int i = 0; i < entries.Count; i++)
                    {
                        result.Add((i + 1, entries[i].Score, entries[i].Nickname));
                    }
                }
                _logger?.Log($"Fetched high scores: {string.Join(", ", result.Select(e => $"{e.Item3}:{e.Item2}"))}");
                callback?.Invoke(result);
            });
        }

        [Serializable]
        public class HighScoreEntry
        {
            public int Score;
            public string Nickname;
            public HighScoreEntry() { }
            public HighScoreEntry(int score, string nickname)
            {
                Score = score;
                Nickname = nickname;
            }
        }
    }
}