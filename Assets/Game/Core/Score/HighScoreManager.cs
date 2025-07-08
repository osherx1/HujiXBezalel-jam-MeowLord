using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Game.Core.Managers;
using System.Text;

namespace Game.Core.Score
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string Name;
        public int Score;
        public float FinishTime;
        public bool PendingUpload;
    }

    [Serializable]
    public class LeaderboardListWrapper
    {
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    }

    public class HighScoreManager
    {
        private const string LocalLeaderboardKey = "LocalLeaderboard";
        private const int MaxHighScores = 10;
        private HighScoreLogger _logger;
        private List<LeaderboardEntry> _localLeaderboard;

        public HighScoreManager(HighScoreLogger logger)
        {
            _logger = logger;
            Initialize();
        }

        private void Initialize()
        {
            _localLeaderboard = LoadLocalLeaderboard();
            FirebaseBridge.Instance.FetchLeaderboard(firebaseEntries =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue((() =>
                {
                    var merged = MergeLeaderboards(_localLeaderboard, firebaseEntries);
                    SaveLocalLeaderboard(merged);
                    _localLeaderboard = merged;
                }));
            });
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (_localLeaderboard == null) _localLeaderboard = new List<LeaderboardEntry>();
                // If local leaderboard is empty, initialize with defaults
                if ( _localLeaderboard.Count < MaxHighScores)
                {
                    _localLeaderboard = new List<LeaderboardEntry>();
                    for (int i = _localLeaderboard.Count; i < MaxHighScores; i++)
                    {
                        _localLeaderboard.Add(new LeaderboardEntry
                        {
                            Name = ((char)('A' + i)).ToString() + ((char)('A' + i)).ToString() +
                                   ((char)('A' + i)).ToString(),
                            Score = 0, FinishTime = 0f, PendingUpload = false
                        });
                    }

                    SaveLocalLeaderboard(_localLeaderboard);
                }
            });
            
        }

        // Merge and compare local and Firebase leaderboards
        private List<LeaderboardEntry> MergeLeaderboards(List<LeaderboardEntry> local,
            List<(int, int, string, float)> firebase)
        {
            // Convert firebase to LeaderboardEntry
            var firebaseEntries = firebase.Select(e => new LeaderboardEntry
            {
                Name = e.Item3,
                Score = e.Item2,
                FinishTime = e.Item4,
                PendingUpload = false
            }).ToList();

            // Combine all entries
            var allEntries = new List<LeaderboardEntry>(firebaseEntries);

            foreach (var localEntry in local)
            {
                // Check if this local entry is already in Firebase (by name, score, finishTime)
                bool exists = firebaseEntries.Any(fb =>
                    fb.Name == localEntry.Name &&
                    fb.Score == localEntry.Score &&
                    Math.Abs(fb.FinishTime - localEntry.FinishTime) < 0.001f);

                if (!exists)
                {
                    // Mark as pending upload (not in Firebase)
                    localEntry.PendingUpload = true;
                    allEntries.Add(localEntry);
                }
            }

            // Remove duplicates (by name, score, finishTime)
            allEntries = allEntries
                .GroupBy(e => (e.Name, e.Score, e.FinishTime))
                .Select(g => g.First())
                .ToList();

            // Sort and keep top 10
            var mergedList = allEntries
                .OrderByDescending(e => e.Score)
                .ThenBy(e => e.FinishTime)
                .Take(MaxHighScores)
                .ToList();

            // Upload pending scores after merging
            UploadPendingScoresSequentially(mergedList);

            return mergedList;
        }

        private void UploadPendingScoresSequentially(List<LeaderboardEntry> leaderboard)
        {
            var pending = leaderboard
                .Where(e => e.PendingUpload)
                .OrderByDescending(e => e.Score)
                .ThenBy(e => e.FinishTime)
                .ToList();

            void UploadNext(int index)
            {
                if (index >= pending.Count)
                {
                    SaveLocalLeaderboard(leaderboard);
                    _localLeaderboard = leaderboard;
                    return;
                }

                var entry = pending[index];
                FirebaseBridge.Instance.SubmitScore(entry.Score, entry.Name, entry.FinishTime,
                    onSuccess: () => UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        entry.PendingUpload = false;
                        UploadNext(index + 1);
                    }),
                    onError: (err) => UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        Debug.LogError($"Failed to upload score for {entry.Name}: {err}");
                        // Optionally, continue with the rest or stop here
                        UploadNext(index + 1);
                    })
                );
            }

            UploadNext(0);
        }

        // Get leaderboard from PlayerPrefs only
        public List<LeaderboardEntry> GetHighScoreTable()
        {
            return LoadLocalLeaderboard();
        }

        // Add a score locally if it qualifies
        public void TryAddHighScore(string name, int score, float finishedTime)
        {
            var leaderboard = LoadLocalLeaderboard();

            // Check if the name already exists
            var existing = leaderboard.FirstOrDefault(e => e.Name == name);

            if (existing != null)
            {
                // Only update if the new score is higher, or same score but better finish time
                if (score > existing.Score || (score == existing.Score && finishedTime < existing.FinishTime))
                {
                    existing.Score = score;
                    existing.FinishTime = finishedTime;
                    existing.PendingUpload = true;
                }
                // If not better, do nothing
            }
            else
            {
                // Add the new entry
                leaderboard.Add(new LeaderboardEntry { Name = name, Score = score, FinishTime = finishedTime, PendingUpload = true });
            }

            // Only keep the highest score per name
            leaderboard = leaderboard
                .GroupBy(e => e.Name)
                .Select(g => g.OrderByDescending(e => e.Score).ThenBy(e => e.FinishTime).First())
                .ToList();

            // Sort and keep top 10
            leaderboard = leaderboard
                .OrderByDescending(e => e.Score)
                .ThenBy(e => e.FinishTime)
                .Take(MaxHighScores)
                .ToList();

            SaveLocalLeaderboard(leaderboard);
            _localLeaderboard = leaderboard;

            // Only submit if the entry is in the top 10
            if (leaderboard.Any(e => e.Name == name && e.Score == score && Math.Abs(e.FinishTime - finishedTime) < 0.001f))
            {
                FirebaseBridge.Instance.SubmitScore(score, name, finishedTime,
                 onSuccess: () => UnityMainThreadDispatcher.Instance.Enqueue(() => leaderboard.First(e => e.Name == name).PendingUpload = false));
            }
        }

        // PlayerPrefs helpers
        private void SaveLocalLeaderboard(List<LeaderboardEntry> entries)
        {
            var wrapper = new LeaderboardListWrapper { entries = entries };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(LocalLeaderboardKey, json);
            PlayerPrefs.Save();
            GetHighScoreTable();
        }

        private List<LeaderboardEntry> LoadLocalLeaderboard()
        {
            string json = PlayerPrefs.GetString(LocalLeaderboardKey, "");
            if (string.IsNullOrEmpty(json))
                return new List<LeaderboardEntry>();
            var wrapper = JsonUtility.FromJson<LeaderboardListWrapper>(json);
            return wrapper.entries ?? new List<LeaderboardEntry>();
        }
    }
}