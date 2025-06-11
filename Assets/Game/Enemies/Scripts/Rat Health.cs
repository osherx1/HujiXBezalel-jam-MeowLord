using System.Collections;
using Game.Core.Managers;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Game.Enemies.Scripts
{
    public class RatHealth : MonoBehaviour
    {
        [SerializeField] public int maxHealth = 1;
        private int _currentHealth;
        
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualTransform;



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

        private void Die()
        {
            GameEvents.MouseCatch(transform.position); 
            int deathIndex = Random.Range(1, 5);
            animator.SetInteger("RandomDeath", deathIndex);
            animator.SetTrigger("Death");

            
            StartCoroutine(ScaleUpEffect());
            StartCoroutine(DelayedReturn());
        }
        
        private IEnumerator DelayedReturn()
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

    }
}