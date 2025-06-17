using System;
using Game.Core.Generics;
using UnityEngine;
using System.Collections;
using Game.UI.Scripts;
using Spine;
using Spine.Unity;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace Game.Core.Managers
{
    public class SceneLoader:MonoSingleton<SceneLoader>
    {
        [SerializeField] private Animator animator;
        [SerializeField] private MeshRenderer skeletonRenderer;
        
        public void TriggerClose(System.Action onComplete)
        {
            StartCoroutine(TriggerAndWaitCoroutine("Close", "closed",onComplete));
        }

        public void TriggerOpen(System.Action onComplete)
        {
            StartCoroutine(TriggerAndWaitCoroutine("Open", "opened", onComplete));
        }

        public void TriggerOut(System.Action onComplete)
        {
            StartCoroutine(TriggerAndWaitCoroutine("Out", "out",onComplete));
        }

        public void TriggerClose()
        {
            TriggerClose(null);
        }
        public void TriggerOpen()
        {
            TriggerOpen(null);
        }
        public void TriggerOut()
        {
            TriggerOut(null);
        }

        private IEnumerator TriggerAndWaitCoroutine(string trigger, string stateName, Action onComplete)
        {
            yield return null;
            if (animator == null || skeletonRenderer == null)
            {
               var curtain = GameObject.FindObjectOfType<Curtain>();
               if (curtain == null)
               {
                   Debug.LogWarning("Animator or SkeletonRenderer not found");
               }
               animator = curtain.animator;
               skeletonRenderer = curtain.meshRenderer;
            }
            animator.SetTrigger(trigger);
            // wait until the animator actually enters your state
            yield return new WaitUntil(() => 
                animator.GetCurrentAnimatorStateInfo(0).IsName(stateName)
            );
            // now wait until that state has played through once
            yield return new WaitUntil(() => 
                animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f
            );
            onComplete?.Invoke();
        }

        public void SetSkeletonSortingLayer(string sortingLayerName)
        {
            SetSkeletonSortingLayer(sortingLayerName, null);
        }

        public void SetSkeletonSortingLayer(string sortingLayerName, System.Action callback)
        {
            if (skeletonRenderer != null)
            {
                skeletonRenderer.sortingLayerName = sortingLayerName;
            }
            callback?.Invoke();
        }

        public void LoadSceneWithCallback(int sceneIndex, System.Action callback = null)
        {
            StartCoroutine(LoadSceneCoroutine(sceneIndex, callback));
        }

        private IEnumerator LoadSceneCoroutine(int sceneIndex, System.Action callback)
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