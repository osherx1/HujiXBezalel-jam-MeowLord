using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Auth;
using Game.Core.Generics;
using Game.Core.Score;

namespace Game.Core.Managers
{
    public class FirebaseInitializer 
    {
        private readonly HighScoreLogger _logger;

        public FirebaseInitializer(HighScoreLogger logger)
        {
            _logger = logger;
            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    _logger?.Log("Firebase is ready!");
                    SignInAnonymously();
                }
                else
                {
                    _logger?.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
                }
            });
        }

        private void SignInAnonymously()
        {
            FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWithOnMainThread(authTask =>
            {
                if (authTask.IsCompleted && !authTask.IsFaulted && !authTask.IsCanceled)
                {
                    _logger?.Log("Signed in anonymously to Firebase!");
                }
                else
                {
                    _logger?.LogError("Failed to sign in anonymously: " + authTask.Exception);
                }
            });
        }
    }
} 