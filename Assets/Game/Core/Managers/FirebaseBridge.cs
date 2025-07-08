using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using Game.Core.Generics;
#if !UNITY_WEBGL
using Firebase;
using Firebase.Extensions;
using Firebase.Auth;
using Firebase.Database;
#endif

namespace Game.Core.Managers
{
    [Serializable]
    public class HighScoreEntry
    {
        public int Score;
        public string Nickname;
        public float FinishTime;
    }

    // Called from JS to C# with leaderboard data (JSON string)
    [Serializable]
    public class HighScoreEntryListWrapper
    {
        public List<HighScoreEntry> entries;
    }

    public class FirebaseBridge : MonoSingleton<FirebaseBridge>
    {
        private bool _intilaized = false;
        private readonly Queue<Action> actionsQueue = new Queue<Action>();
#if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void SaveScoreToFirebase(string nickname, int score, float finishTime,
            string callbackSuccess, string callbackError);

        [DllImport("__Internal")]
        private static extern void GetLeaderboardFromFirebase(string callbackSuccess, string callbackError);

        [DllImport("__Internal")]
        private static extern void InitializeFirebase();

        private static Action _pendingInitSuccess;
        private static Action<string> _pendingInitError;
#endif

        // For C# SDK: store callback for pending leaderboard fetch
#if !UNITY_WEBGL
        private bool isWriteInProgress = false;
        private Action pendingFetch = null;
        private bool isGetHighScoreTableInProgress = false;
        private const int MaxHighScores = 10;
        private const string HighScoresPath = "highscores";

#endif

        private static Action<List<(int, int, string, float)>> _pendingLeaderboardCallback;
        private static Action _pendingLeaderboardErrorCallback;


        public void Initialize(Action onReady = null, Action<string> onError = null)
        {
            Debug.Log("Firebase starts intiliztion");
            Action onReadyWrapper = () =>
            {
                _intilaized = true;
                onReady?.Invoke();
                DequeWaitingAction();
            };

#if UNITY_WEBGL
            _pendingInitSuccess = onReadyWrapper;
            _pendingInitError = onError;
            Debug.Log("Firebase enter java script on webgl");
            InitializeFirebase();
#else
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    Debug.Log("Firebase is ready!");
                    FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWithOnMainThread(authTask =>
                    {
                        if (authTask.IsCompleted && !authTask.IsFaulted && !authTask.IsCanceled)
                        {
                            Debug.Log("Signed in anonymously to Firebase!");
                            onReadyWrapper?.Invoke();
                        }
                        else
                        {
                            Debug.LogError("Failed to sign in anonymously: " + authTask.Exception);
                            onError?.Invoke("Failed to sign in anonymously: " + authTask.Exception);
                        }
                    });
                }
                else
                {
                    Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
                    onError?.Invoke($"Could not resolve all Firebase dependencies: {dependencyStatus}");
                }
            });
#endif
        }

        private void DequeWaitingAction()
        {
            Debug.Log("Firebase deque ENTERED");
            if (actionsQueue.Count > 0)
            {
                Debug.Log("Firebase deque action is queued");
                var action = actionsQueue.Dequeue();
                action?.Invoke();
            }
        }

        // SubmitScore with success and error callbacks
        public void SubmitScore(int score, string nickname, float finishTime, Action onSuccess = null,
            Action<string> onError = null)
        {
            if (!_intilaized)
            {
                Debug.Log("Firebase not initialized yet, quequeing Submit Score");
                actionsQueue.Enqueue(() => SubmitScore(score, nickname, finishTime, onSuccess, onError));
                return;
            }

            Action onSuccessWrapper =
                () =>
                {
                    onSuccess?.Invoke();
                    DequeWaitingAction();
                };

#if UNITY_WEBGL
            // JS should call back to OnSubmitScoreSuccess or OnSubmitScoreError
            SaveScoreToFirebase(nickname, score, finishTime, "OnSubmitScoreSuccess", "OnSubmitScoreError");
            _pendingSubmitSuccess = onSuccessWrapper;
            _pendingSubmitError = onError;
#else
            SubmitScoreCSharp(score, nickname, finishTime, onSuccess, onError);
#endif
        }

#if UNITY_WEBGL
        private static Action _pendingSubmitSuccess;

        private static Action<string> _pendingSubmitError;

        // Called from JS on success
        public void OnSubmitScoreSuccess(string msg)
        {
            Debug.Log("Score submitted successfully (WebGL/JS): " + msg);
            _pendingSubmitSuccess?.Invoke();
            _pendingSubmitSuccess = null;
            _pendingSubmitError = null;
        }

        // Called from JS on error
        public void OnSubmitScoreError(string error)
        {
            Debug.LogError("Score submission failed (WebGL/JS): " + error);
            _pendingSubmitError?.Invoke(error);
            _pendingSubmitSuccess = null;
            _pendingSubmitError = null;
        }
#endif

#if !UNITY_WEBGL
        // C# SDK logic for submitting a score
        private void SubmitScoreCSharp(int score, string nickname, float finishTime, Action onSuccess,
            Action<string> onError)
        {
            if (string.IsNullOrEmpty(nickname))
                nickname = "Player";
            FetchLeaderboardCSharp(table =>
            {
                UnityMainThreadDispatcher.Instance.Enqueue((() =>
                {
                    var list = table.Select(e => new HighScoreEntry
                    {
                        Score = e.Item2, Nickname = e.Item3, FinishTime =
                            e.Item4
                    }).ToList();
                    var existing = list.Find(entry => entry.Nickname == nickname);
                    if (existing != null)
                    {
                        if (score > existing.Score)
                        {
                            existing.Score = score;
                            existing.FinishTime = finishTime;
                        }
                        else if (pendingFetch != null)
                        {
                            var fetch = pendingFetch;
                            pendingFetch = null;
                            fetch();
                            onSuccess?.Invoke();
                            return;
                        } // No update needed
                    }
                    else
                    {
                        list.Add(new HighScoreEntry
                        {
                            Score = score,
                            FinishTime = finishTime,
                            Nickname =  nickname
                        });
                    }

                    // Sort and keep only top 10
                    list = list.OrderByDescending(e => e.Score).Take(MaxHighScores).ToList();
                    // Write back to Firebase
                    var db = FirebaseDatabase.DefaultInstance;
                    var dict = new Dictionary<string, object>();
                    foreach (var entry in list)
                    {
                        dict[entry.Nickname] = new Dictionary<string, object>
                        {
                            { "Score", entry.Score },
                            { "FinishTime", entry.FinishTime }
                        };
                    }

                    Debug.Log(
                        $"Writing high scores to Firebase: {string.Join(", ", list.Select(e => $"{e.Nickname}:{e.Score}"))}");
                    isWriteInProgress = true;
                    db.RootReference.Child(HighScoresPath).SetValueAsync(dict).ContinueWith(task =>
                    {
                        isWriteInProgress = false;
                        if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
                        {
                            Debug.Log("Write complete. Success");
                            onSuccess?.Invoke();
                        }
                        else
                        {
                            Debug.LogError("Write failed: " + task.Exception);
                            onError?.Invoke(task.Exception?.ToString() ?? "Unknown error");
                        }

                        if (pendingFetch != null)
                        {
                            var fetch = pendingFetch;
                            pendingFetch = null;
                            fetch();
                        }
                    });
                }));
            }, () => onError?.Invoke("Failed to fetch leaderboard for submission"));
        }
#endif

        // FetchLeaderboard with success and error callbacks
        public void FetchLeaderboard(Action<List<(int, int, string, float)>> onSuccess, Action onError = null)
        {
            if (!_intilaized)
            {
                Debug.Log("Firebase not initialized yet, quequeing Fetch Leaderboard");
                actionsQueue.Enqueue(() => FetchLeaderboard(onSuccess, onError));
                return;
            }

            Action<List<(int, int, string, float)>> onSuccessWrapper =
                (list) =>
                {
                    onSuccess?.Invoke(list);
                    DequeWaitingAction();
                };
#if UNITY_WEBGL
            _pendingLeaderboardCallback = onSuccessWrapper;
            _pendingLeaderboardErrorCallback = onError;
            GetLeaderboardFromFirebase("OnLeaderboardReceived", "OnFetchLeaderboardError");
#else
            FetchLeaderboardCSharp(onSuccess, onError);
#endif
        }

#if !UNITY_WEBGL
        // C# SDK logic for fetching leaderboard
        private void FetchLeaderboardCSharp(Action<List<(int, int, string, float)>> onSuccess, Action onError)
        {
            if (isWriteInProgress || isGetHighScoreTableInProgress)
            {
                pendingFetch = () => FetchLeaderboardCSharp(onSuccess, onError);
                return;
            }

            var db = FirebaseDatabase.DefaultInstance;
            isGetHighScoreTableInProgress = true;
            db.RootReference.Child(HighScoresPath).OrderByValue().LimitToLast(MaxHighScores).GetValueAsync()
                .ContinueWith(task =>
                {
                    var result = new List<(int, int, string, float)>();
                    if (task.IsCompleted && task.Result != null && task.Result.Exists)
                    {
                        var entries = new List<HighScoreEntry>();
                        foreach (var child in task.Result.Children)
                        {
                            string name = child.Key;
                            int score = 0;
                            float finishTime = 0f;

                            if (child.Value is Dictionary<string, object> entryDict)
                            {
                                if (entryDict.TryGetValue("Score", out var scoreObj))
                                    int.TryParse(scoreObj.ToString(), out score);
                                if (entryDict.TryGetValue("FinishTime", out var timeObj))
                                    float.TryParse(timeObj.ToString(), out finishTime);
                            }
                            else if (child.Value is string || child.Value is long || child.Value is int)
                            {
                                // Backward compatibility: only score stored
                                int.TryParse(child.Value.ToString(), out score);
                            }

                            entries.Add(new HighScoreEntry
                            {
                                Score = score,
                                FinishTime = finishTime,
                                Nickname = name
                            });
                        }

                        // Sort descending
                        entries = entries.OrderByDescending(e => e.Score).Take(MaxHighScores).ToList();
                        for (int i = 0; i < entries.Count; i++)
                        {
                            result.Add((i + 1, entries[i].Score, entries[i].Nickname, entries[i].FinishTime));
                        }

                        onSuccess?.Invoke(result);
                    }
                    else
                    {
                        Debug.LogWarning("Failed to fetch leaderboard from Firebase, using local backup.");
                        onError?.Invoke();
                    }

                    isGetHighScoreTableInProgress = false;
                });
        }


#endif

#if UNITY_WEBGL
        // Called from JS on error
        public void OnFetchLeaderboardError(string error)
        {
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                Debug.LogError("Fetch leaderboard failed (WebGL/JS): " + error);
                _pendingLeaderboardErrorCallback?.Invoke();
                _pendingLeaderboardErrorCallback = null;
            });
        }
#endif


        public void OnLeaderboardReceived(string json)
        {
#if UNITY_WEBGL
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                Debug.Log("Leaderboard received from JS: " + json);
                if (_pendingLeaderboardCallback != null)
                {
                    try
                    {
                        var wrapper = JsonUtility.FromJson<HighScoreEntryListWrapper>("{\"entries\": " + json + "}");
                        if (wrapper != null && wrapper.entries != null)
                        {
                            Debug.Log($"Parsed {wrapper.entries.Count} leaderboard entries");
                            foreach (var entry in wrapper.entries)
                            {
                                Debug.Log($"{entry.Nickname} {entry.Score} {entry.FinishTime}");
                            }

                            // Convert to tuple format for callback
                            var tupleList = wrapper.entries
                                .Select((e, i) => (i + 1, e.Score, e.Nickname, e.FinishTime))
                                .ToList();
                            _pendingLeaderboardCallback(tupleList);
                        }
                        else
                        {
                            Debug.LogError("Failed to parse leaderboard JSON or entries are null.");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error parsing leaderboard JSON: " + e.Message);
                    }
                }
            });
#endif
        }

        // Called from JS via SendMessage
        public void OnFirebaseInitialized(string result)
        {
#if UNITY_WEBGL
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log("Firebase maybe Initialized: " + result);
                    if (result == "success")
                    {
                        _pendingInitSuccess?.Invoke();
                    }
                    else if (result.StartsWith("error:"))
                    {
                        _pendingInitError?.Invoke("Failed to sign in anonymously");
                    }

                    _pendingInitSuccess = null;
                    _pendingInitError = null;
                }
            );
#endif
        }
    }
}