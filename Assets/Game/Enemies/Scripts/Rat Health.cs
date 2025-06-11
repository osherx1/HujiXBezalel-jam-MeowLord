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


        private void OnEnable()
        {
            ResetHealth();
            ResetAnimator();
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

            StartCoroutine(DelayedReturn());
        }
        
        private System.Collections.IEnumerator DelayedReturn()
        {
            yield return new WaitForSeconds(1.5f);
            RatPoolManager.Instance.ReturnRat(gameObject);
        }
    }
}