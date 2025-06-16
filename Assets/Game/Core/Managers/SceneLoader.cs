using System;
using Game.Core.Generics;
using UnityEngine;
using System.Collections;
using Spine;
using Spine.Unity;
using UnityEngine.Serialization;

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
            if (skeletonRenderer != null)
            {
                skeletonRenderer.sortingLayerName = sortingLayerName;
            }
        }
    }
}