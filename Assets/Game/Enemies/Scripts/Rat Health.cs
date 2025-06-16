using System.Collections;
using Game.Core.Managers;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

namespace Game.Enemies.Scripts
{
    public class RatHealth : MonoBehaviour
    {
        private static readonly int RandomDeath = Animator.StringToHash("RandomDeath");
        private static readonly int Death = Animator.StringToHash("Death");
        [SerializeField] public int maxHealth = 1;
        private int _currentHealth;
        
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualTransform;

        public GameObject score;
        [SerializeField] private GameObject scoreFlyObjectPrefab;


        private void OnEnable()
        {
            ResetHealth();
            ResetAnimator();
            
            if (visualTransform != null)
                visualTransform.localScale = Vector3.one;
        }
        
        private void ResetAnimator()
        {
            if (animator == null) return;

            animator.Rebind(); // resets parameters and state
            animator.Update(0f); // forces immediate re-evaluation of state
        }

        private void ResetHealth()
        {
            _currentHealth = maxHealth;
        }

        public void TakeDamage(int amount)
        {
            _currentHealth -= amount;

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void Die()
        {
            GameEvents.MouseCatch(transform.position); 
            int deathIndex = Random.Range(1, 5);
            animator.SetInteger(RandomDeath, deathIndex);
            animator.SetTrigger(Death);

            
            StartCoroutine(ScaleUpEffect());
            if (score != null)
                StartCoroutine(FlyToScoreTarget());
            StartCoroutine(DelayedReturn());
            //StartCoroutine(DelayedReturn());
        }
        
        protected virtual IEnumerator DelayedReturn()
        {
            yield return new WaitForSeconds(1.5f);
            RatPoolManager.Instance.ReturnRat(gameObject);
        }
        
        private IEnumerator ScaleUpEffect()
        {
            float duration = 0.3f;
            float elapsed = 0f;
            Vector3 originalScale = visualTransform.localScale;
            Vector3 targetScale = originalScale * 1.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                visualTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            visualTransform.localScale = targetScale;
        }
        
        public void SetScoreTarget(GameObject scoreTarget)
        {
            score = scoreTarget;
        }
        
        private IEnumerator FlyToScoreTarget()
        {
            // Spawn the flying object at the rat's position
            GameObject flyObj = Instantiate(scoreFlyObjectPrefab, transform.position, Quaternion.identity);
            float duration = 0.5f;
            float elapsed = 0f;

            Vector3 start = flyObj.transform.position;
            Vector3 end = score.transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                flyObj.transform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            Destroy(flyObj); // Remove once it reaches the target
        }
    }
}