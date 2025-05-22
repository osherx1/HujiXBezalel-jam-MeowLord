using System;
using UnityEngine;

namespace Game.Enemies.Scripts
{
    public class RatHealth : MonoBehaviour
    {
        public static event Action<RatHealth> OnRatDied;

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
            OnRatDied?.Invoke(this);
            RatPoolManager.Instance.ReturnRat(gameObject);
        }
    }
}