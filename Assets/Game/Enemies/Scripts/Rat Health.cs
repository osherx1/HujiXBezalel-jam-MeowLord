using System;
using Game.Core.Managers;
using UnityEngine;

namespace Game.Enemies.Scripts
{
    public class RatHealth : MonoBehaviour
    {
        [SerializeField] public int maxHealth = 1;
        private int _currentHealth;

        private void OnEnable()
        {
            ResetHealth();
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
            GameEvents.MouseCatch(transform.position); // 🔔 Notify listeners
            RatPoolManager.Instance.ReturnRat(gameObject);
        }
    }
}