using System;
using Game.Core.Generics;
using UnityEngine;
using System.Collections;
using Game.UI.Scripts;
using Spine;
using Spine.Unity;
using UnityEngine.SceneManagement;

namespace Game.Core.Managers
{
    public class SceneLoader : MonoSingleton<SceneLoader>
    {
        [SerializeField] private SkeletonGraphic skeletonGraphic;

        public void TriggerClose(Action onComplete) => StartCoroutine(PlayAndWait("Close", onComplete));
        public void TriggerOpen(Action onComplete) => StartCoroutine(PlayAndWait("Open", onComplete));
        public void TriggerOut(Action onComplete) => StartCoroutine(PlayAndWait("Out", onComplete));

        public void TriggerClose() => TriggerClose(null);
        public void TriggerOpen() => TriggerOpen(null);
        public void TriggerOut() => TriggerOut(null);

        private IEnumerator PlayAndWait(string trigger, Action onComplete)
        {
            yield return null;

            

            var state = skeletonGraphic.AnimationState;
            var current = state.GetCurrent(0);
            string currentAnim = current?.Animation?.Name;

            TrackEntry firstEntry = null;

            switch (trigger)
            {
                case "Close":
                    if (currentAnim == "opened")
                    {
                        firstEntry = state.SetAnimation(0, "inClose", false);
                        state.AddAnimation(0, "closed", false, 0f).MixDuration = 0.5f;
                    }
                    else if (currentAnim == "out")
                    {
                        firstEntry = state.SetAnimation(0, "outClose", false);
                        state.AddAnimation(0, "closed", false, 0f).MixDuration = 0.5f;
                    }
                    break;

                case "Open":
                    firstEntry = state.SetAnimation(0, "inOpen", false);
                    state.AddAnimation(0, "opened", false, 0f).MixDuration = 0.5f;
                    break;

                case "Out":
                    firstEntry = state.SetAnimation(0, "outOpen", false);
                    state.AddAnimation(0, "out", false, 0f).MixDuration = 0.5f;
                    break;

                default:
                    Debug.LogWarning($"Unknown trigger: {trigger}");
                    yield break;
            }

            if (firstEntry != null)
            {
                yield return new WaitForSpineAnimation(firstEntry,WaitForSpineAnimation.AnimationEventTypes.Complete);
            }

            onComplete?.Invoke();
        }

        public void SetSkeletonSortingLayer(string sortingLayerName, Action callback = null)
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;

                // Use a symbolic sorting layer name to set sortingOrder
                switch (sortingLayerName)
                {
                    case "Curtain":
                        canvas.sortingOrder = 10;
                        break;
                    case "default":
                        canvas.sortingOrder = -10;
                        break;
                    default:
                        canvas.sortingOrder = 0;
                        break;
                }
            }
            else
            {
                Debug.LogWarning("Canvas not found on GameObject.");
            }

            callback?.Invoke();
        }

        public void SetSkeletonSortingLayer(string sortingLayerName) => SetSkeletonSortingLayer(sortingLayerName, null);

        public void LoadSceneWithCallback(int sceneIndex, Action callback = null)
        {
            StartCoroutine(LoadSceneCoroutine(sceneIndex, callback));
        }

        private IEnumerator LoadSceneCoroutine(int sceneIndex, Action callback)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            callback?.Invoke();
        }
        
    }
}
